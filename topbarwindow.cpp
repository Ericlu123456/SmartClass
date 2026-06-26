/*
 * ==========================================================================
 *  topbarwindow.cpp
 *  顶栏
 * ==========================================================================
 */

#include "topbarwindow.h"
#include "settingswindow.h"
#include "config.h"

#include <QSettings>
#include <QHBoxLayout>
#include <QVBoxLayout>
#include <QDateTime>
#include <QApplication>
#include <QScreen>
#include <QTime>
#include <QRandomGenerator>
#include <QPainter>
#include <QPainterPath>
#include <QPixmap>
#include <QTimer>
#include <QtMath>
#include <QEnterEvent>
#include <QPropertyAnimation>
#include <QGraphicsOpacityEffect>
#include <QDesktopServices>
#include <QUrl>
#include <QDir>

#ifdef HAS_LAYER_SHELL
#include <LayerShellQt/Window>
#endif
#ifdef Q_OS_WIN
#include <windows.h>
static RECT  s_origWorkArea;
static bool  s_workAreaSaved = false;
#endif

// =========================================================================
//  PulseRing — 放大版, 60×60
// =========================================================================

PulseRing::PulseRing(QWidget *parent) : QWidget(parent)
{
    setFixedSize(52, 52);
    setAttribute(Qt::WA_TransparentForMouseEvents);
}

void PulseRing::paintEvent(QPaintEvent *)
{
    QPainter painter(this);
    painter.setRenderHint(QPainter::Antialiasing);

    int s = qMin(width(), height());
    int margin = 3;
    int x = (width() - s) / 2 + margin;
    int y = (height() - s) / 2 + margin;
    int d = s - margin * 2;
    int penW = 3;

    qint64 ms = QDateTime::currentMSecsSinceEpoch();
    double progress = (ms % 1000) / 1000.0;
    int span16 = static_cast<int>(progress * 360 * 16);

    painter.setPen(QPen(QColor(255, 255, 255, 15), penW));
    painter.setBrush(Qt::NoBrush);
    painter.drawEllipse(x, y, d, d);

    QPen arcPen(QColor(255, 255, 255, 220), penW, Qt::SolidLine, Qt::RoundCap);
    painter.setPen(arcPen);
    painter.drawArc(x, y, d, d, 90 * 16, -span16);
}

// =========================================================================
//  FlipClock — 逐位翻页
// =========================================================================

FlipClock::FlipClock(int pixelSize, QWidget *parent)
    : QWidget(parent), m_pixelSize(pixelSize)
{
    setMinimumHeight(pixelSize);

    for (int i = 0; i < 12; ++i) {
        m_slots[i].ch = '0';
        m_slots[i].oldCh = '0';
        m_slots[i].anim = 1.0;
        m_slots[i].sep = (i == 2 || i == 5 || i == 8);
        if (m_slots[i].sep) {
            m_slots[i].ch = (i == 8) ? '.' : ':';
            m_slots[i].oldCh = m_slots[i].ch;
            m_slots[i].dur = 0;
        } else if (i >= 9) {
            int msPos = i - 9;
            m_slots[i].dur = msPos == 0 ? 80 : (msPos == 1 ? 25 : 0);
        } else {
            m_slots[i].dur = 380;
        }
    }
}

void FlipClock::setText(const QString &hhmmss, const QString &ms)
{
    QString full = hhmmss + ms;
    if (full == m_fullText) {
        update();
        return;
    }
    for (int i = 0; i < 12 && i < full.size(); ++i) {
        if (m_slots[i].sep) continue;
        if (full[i] != m_slots[i].ch) {
            m_slots[i].oldCh = m_slots[i].ch;
            m_slots[i].ch    = full[i];
            m_slots[i].anim  = 0.0;
        }
    }
    for (int i : {2, 5, 8})
        if (i < full.size()) m_slots[i].ch = full[i];

    m_fullText = full;
    recalcLayout();
    update();
}

void FlipClock::setPixelSize(int ps)
{
    if (ps == m_pixelSize) return;
    m_pixelSize = ps;
    recalcLayout();
    update();
}

void FlipClock::recalcLayout()
{
    QFont f = font();
    f.setPixelSize(m_pixelSize);
    f.setWeight(QFont::Light);
    QFontMetrics fm(f);
    double x = 0;
    for (int i = 0; i < 12; ++i) {
        m_slots[i].x = x;
        m_slots[i].w = fm.horizontalAdvance(
            m_slots[i].sep ? m_slots[i].ch : QChar('0'));
        x += m_slots[i].w;
    }
    setFixedWidth(static_cast<int>(x + 12));  // 12px 余量, 确保毫秒不裁切
}

void FlipClock::paintEvent(QPaintEvent *)
{
    QPainter painter(this);
    painter.setRenderHint(QPainter::Antialiasing);

    // 推进动画
    for (int i = 0; i < 12; ++i) {
        if (m_slots[i].sep) continue;
        if (m_slots[i].anim < 1.0 && m_slots[i].dur > 0) {
            m_slots[i].anim += 6.0 / m_slots[i].dur;
            if (m_slots[i].anim >= 1.0) m_slots[i].anim = 1.0;
        }
    }

    QFont mainF = font();
    mainF.setPixelSize(m_pixelSize);
    mainF.setWeight(QFont::Light);
    painter.setFont(mainF);

    double slideH = height() * 0.85;

    for (int i = 0; i < 12; ++i) {
        auto &s = m_slots[i];
        double cx = s.x;

        if (s.sep) {
            painter.setPen(QColor(160, 160, 180));
            painter.drawText(QRectF(cx, 0, s.w, height()),
                             Qt::AlignCenter, QString(s.ch));
        } else if (s.anim >= 1.0 || s.dur == 0) {
            painter.setPen(QColor(240, 240, 248));
            painter.drawText(QRectF(cx, 0, s.w, height()),
                             Qt::AlignCenter, QString(s.ch));
        } else {
            double p = s.anim;
            double ep = 1.0 - std::pow(1.0 - p, 3.0);  // OutCubic

            painter.setPen(QColor(240, 240, 248,
                                  static_cast<int>((1.0 - ep) * 200 + 55)));
            painter.drawText(QRectF(cx, ep * slideH, s.w, height()),
                             Qt::AlignCenter, QString(s.oldCh));

            painter.setPen(QColor(240, 240, 248,
                                  static_cast<int>(ep * 200 + 55)));
            painter.drawText(QRectF(cx, (ep - 1.0) * slideH, s.w, height()),
                             Qt::AlignCenter, QString(s.ch));
        }
    }
}

// =========================================================================
//  WobbleButton — timer 驱动悬停缩放 (无晃动)
// =========================================================================

WobbleButton::WobbleButton(int iconType, const QString &tooltip,
                           QWidget *parent)
    : QWidget(parent), m_iconType(iconType)
{
    setFixedSize(40, 40);
    setCursor(Qt::PointingHandCursor);
    setToolTip(tooltip);
    // 确保初始不透明 — 消除第一帧"黑掉"的问题
    m_hover = 0.001;

    m_timer = new QTimer(this);
    connect(m_timer, &QTimer::timeout, this, &WobbleButton::tick);
}

void WobbleButton::tick()
{
    double step = 8.0 / 160.0;  // 160ms 完成 (更快)
    if (m_target > m_hover) {
        m_hover = qMin(m_hover + step, m_target);
        if (m_hover >= 1.0) m_timer->stop();
    } else {
        m_hover = qMax(m_hover - step, m_target);
        if (m_hover <= 0.001) m_timer->stop();
    }
    update();
}

void WobbleButton::paintEvent(QPaintEvent *)
{
    QPainter painter(this);
    painter.setRenderHint(QPainter::Antialiasing);

    // 平滑缓动 (OutQuad 近似, 不从 0 开始避免黑闪)
    double t = qBound(0.0, m_hover, 1.0);
    double ep = 1.0 - (1.0 - t) * (1.0 - t);  // ease-out

    // alpha: 100→255, 幅度更大
    int alpha = static_cast<int>(100 + 155 * ep);
    if (alpha > 255) alpha = 255;
    int bgAlpha = static_cast<int>(28 * ep);
    double scale = 1.0 + 0.22 * ep;  // 放大到 1.22x

    QRectF r = rect().toRectF();
    QPointF center = r.center();
    double baseSize = 15.0;
    double s = baseSize * scale;

    if (bgAlpha > 0) {
        painter.setPen(Qt::NoPen);
        painter.setBrush(QColor(255, 255, 255, bgAlpha));
        painter.drawRoundedRect(r.adjusted(0, 0, 0, 0), 10, 10);
    }

    QPen pen(QColor(255, 255, 255, alpha), 2.2, Qt::SolidLine, Qt::RoundCap,
             Qt::RoundJoin);
    painter.setPen(pen);
    painter.setBrush(Qt::NoBrush);

    switch (m_iconType) {
    case 0: {  // 六边形齿轮
        QPainterPath hex;
        for (int i = 0; i < 6; ++i) {
            double a = M_PI / 3.0 * i - M_PI / 6.0;
            double x = center.x() + s * 0.7 * cos(a);
            double y = center.y() + s * 0.7 * sin(a);
            if (i == 0) hex.moveTo(x, y);
            else        hex.lineTo(x, y);
        }
        hex.closeSubpath();
        painter.drawPath(hex);
        painter.drawEllipse(center, s * 0.22, s * 0.22);
        break;
    }
    case 1: {  // 同心圆
        painter.drawEllipse(center, s * 0.75, s * 0.75);
        painter.drawEllipse(center, s * 0.30, s * 0.30);
        break;
    }
    case 2: {  // 双框
        double o = s * 0.72, i = s * 0.35;
        painter.drawRect(QRectF(center.x() - o, center.y() - o, o * 2, o * 2));
        painter.drawRect(QRectF(center.x() - i, center.y() - i, i * 2, i * 2));
        break;
    }
    case 3: {  // 三角
        QPainterPath tri;
        tri.moveTo(center.x(),          center.y() - s * 0.75);
        tri.lineTo(center.x() + s * 0.7, center.y() + s * 0.45);
        tri.lineTo(center.x() - s * 0.7, center.y() + s * 0.45);
        tri.closeSubpath();
        painter.drawPath(tri);
        break;
    }
    }
}

void WobbleButton::enterEvent(QEnterEvent *)
{
    m_target = 1.0;
    if (!m_timer->isActive()) m_timer->start(8);
    emit hoverIn();
}

void WobbleButton::leaveEvent(QEvent *)
{
    m_target = 0.001;
    if (!m_timer->isActive()) m_timer->start(8);
    emit hoverOut();
}

void WobbleButton::mousePressEvent(QMouseEvent *e)
{
    if (e->button() == Qt::LeftButton) m_pressed = true;
}

void WobbleButton::mouseReleaseEvent(QMouseEvent *e)
{
    if (e->button() == Qt::LeftButton && m_pressed) {
        m_pressed = false;
        if (rect().contains(e->position().toPoint()))
            emit clicked();
    }
}

// =========================================================================
//  QuickButtons
// =========================================================================

QuickButtons::QuickButtons(QLabel *hoverTip, QGraphicsOpacityEffect *tipOpacity,
                           QWidget *parent)
    : QWidget(parent)
{
    setStyleSheet("background: transparent;");
    auto *lay = new QHBoxLayout(this);
    lay->setContentsMargins(0, 0, 0, 0);
    lay->setSpacing(10);

    for (const auto &link : QUICK_LINKS) {
        auto *btn = new WobbleButton(link.iconType, link.tooltip);
        if (link.iconType == 0) {
            connect(btn, &WobbleButton::clicked, this,
                    &QuickButtons::settingsRequested);
        } else if (link.iconType == 1) {
            connect(btn, &WobbleButton::clicked, this,
                    &QuickButtons::browserRequested);
        } else if (link.iconType == 2) {
            connect(btn, &WobbleButton::clicked, this,
                    &QuickButtons::fileManagerRequested);
        } else if (link.iconType == 3) {
            connect(btn, &WobbleButton::clicked, this,
                    &QuickButtons::toggleClockRequested);
        }
        connect(btn, &WobbleButton::hoverIn, this, [hoverTip, tipOpacity, btn]() {
            if (!hoverTip) return;
            hoverTip->setText(btn->toolTip());
            hoverTip->adjustSize();
            QPoint g = btn->mapToGlobal(
                QPoint(btn->width() / 2, btn->height() + 4));
            hoverTip->move(g.x() - hoverTip->width() / 2, g.y());
            hoverTip->show();
            auto *anim = new QPropertyAnimation(tipOpacity, "opacity", hoverTip);
            anim->setDuration(160);
            anim->setStartValue(qMax(0.0, tipOpacity->opacity()));
            anim->setEndValue(1.0);
            anim->setEasingCurve(QEasingCurve::OutCubic);
            anim->start(QAbstractAnimation::DeleteWhenStopped);
        });
        connect(btn, &WobbleButton::hoverOut, this, [tipOpacity]() {
            if (!tipOpacity) return;
            auto *anim = new QPropertyAnimation(tipOpacity, "opacity", tipOpacity);
            anim->setDuration(200);
            anim->setStartValue(tipOpacity->opacity());
            anim->setEndValue(0.0);
            anim->setEasingCurve(QEasingCurve::OutCubic);
            anim->start(QAbstractAnimation::DeleteWhenStopped);
        });

        lay->addWidget(btn);
    }
}

// =========================================================================
//  ExamWindow — 全屏考试时钟
// =========================================================================

ExamWindow::ExamWindow(QWidget *parent) : QWidget(parent)
{
    setWindowFlags(Qt::FramelessWindowHint | Qt::WindowStaysOnTopHint | Qt::Tool);
    setAttribute(Qt::WA_TranslucentBackground);
    setCursor(Qt::ArrowCursor);

    // 内容容器 — 独立定位, 动画移动的就是它
    m_content = new QWidget(this);
    m_content->setStyleSheet("background: transparent;");
    auto *lay = new QHBoxLayout(m_content);
    lay->setContentsMargins(0, 0, 0, 0);
    lay->setSpacing(20);

    m_ring = new PulseRing();
    m_ring->setFixedSize(150, 150);
    m_clock = new FlipClock(240);

    lay->addStretch();
    lay->addWidget(m_ring);
    lay->addWidget(m_clock);
    lay->addStretch();

    // 刷新定时器
    m_timer = new QTimer(this);
    connect(m_timer, &QTimer::timeout, this, &ExamWindow::tick);
}

void ExamWindow::tick()
{
    auto now = QDateTime::currentDateTime();
    m_clock->setText(now.toString("HH:mm:ss"), "." + now.toString("zzz"));
    m_ring->update();

    int w = width(), h = height();
    if (w <= 0 || h <= 0) return;

    QRectF dirty;

    // 推进已有三角形
    for (int i = m_triangles.size() - 1; i >= 0; --i) {
        auto &t = m_triangles[i];
        t.life += t.lifeStep;
        if (t.life >= 1.0) {
            dirty = dirty.united(t.prevRect);
            m_triangles.removeAt(i);
            continue;
        }
        t.y -= t.lifeStep * h * 0.55;
        t.opacity = t.peakOpacity * std::sin(t.life * M_PI);
        double s = t.size;
        t.pts[0] = {t.x,          t.y - s * 0.75};
        t.pts[1] = {t.x + s * 0.7, t.y + s * 0.45};
        t.pts[2] = {t.x - s * 0.7, t.y + s * 0.45};
        QRectF cur(t.x - s * 0.7, t.y - s * 0.75, s * 1.4, s * 1.2);
        dirty = dirty.united(t.prevRect);
        dirty = dirty.united(cur);
        t.prevRect = cur;
    }

    // 生成新三角形
    if (--m_spawnCd <= 0) {
        m_spawnCd = 30 + QRandomGenerator::global()->bounded(55);
        RiseTri t;
        t.x = QRandomGenerator::global()->bounded(w);
        t.y = h + 30;
        t.size = 55 + QRandomGenerator::global()->bounded(95);
        t.peakOpacity = 0.04 + QRandomGenerator::global()->bounded(0.12);
        double duration = 280 + QRandomGenerator::global()->bounded(520);
        t.lifeStep = 1.0 / duration;
        t.life = 0.0;
        t.opacity = 0.0;
        double s = t.size;
        t.prevRect = QRectF(t.x - s * 0.7, t.y - s * 0.75, s * 1.4, s * 1.2);
        dirty = dirty.united(t.prevRect);
        m_triangles.append(t);
    }

    if (!dirty.isEmpty())
        update(dirty.adjusted(-25, -25, 25, 25).toRect());
}

void ExamWindow::paintEvent(QPaintEvent *)
{
    QPainter painter(this);
    int bgA = static_cast<int>(m_bgAlpha * 255);

    painter.setPen(Qt::NoPen);
    painter.setBrush(QColor(0, 0, 0, bgA));
    painter.drawRect(rect());

    if (bgA < 20) return;

    painter.setBrush(Qt::NoBrush);
    QPen pen(Qt::white);
    pen.setCapStyle(Qt::RoundCap);
    pen.setJoinStyle(Qt::RoundJoin);
    for (const auto &t : m_triangles) {
        int a = static_cast<int>(t.opacity * bgA);
        if (a < 1) continue;
        pen.setColor(QColor(210, 220, 255, a));
        pen.setWidthF(qMax(4.5, t.size * 0.12));
        painter.setPen(pen);
        painter.drawPolygon(t.pts, 3);
    }
}

void ExamWindow::mousePressEvent(QMouseEvent *)
{
    animateOut();
}

static int lerpI(int a, int b, double t) { return a + (int)((b - a) * t); }

void ExamWindow::animateIn(QRect clockGlobalRect)
{
    m_fromRect = clockGlobalRect;
    QScreen *screen = QApplication::primaryScreen();
    if (!screen) return;
    QRect screenGeo = screen->geometry();

    // Wayland + LayerShell: 用叠加层全屏
#ifdef HAS_LAYER_SHELL
    if (qEnvironmentVariableIsSet("WAYLAND_DISPLAY")) {
        winId();
        if (auto *sw = LayerShellQt::Window::get(windowHandle())) {
            sw->setLayer(LayerShellQt::Window::LayerOverlay);
            using A = LayerShellQt::Window::Anchor;
            sw->setAnchors(
                LayerShellQt::Window::Anchors(A::AnchorTop)
                | A::AnchorLeft | A::AnchorRight | A::AnchorBottom);
            sw->setExclusiveZone(0);
            sw->setKeyboardInteractivity(
                LayerShellQt::Window::KeyboardInteractivityOnDemand);
        }
    }
#endif

#ifndef HAS_LAYER_SHELL
    setGeometry(screenGeo);
#else
    if (!qEnvironmentVariableIsSet("WAYLAND_DISPLAY"))
        setGeometry(screenGeo);
#endif

    // 起始: 38px (与顶栏时钟一致), 放在顶栏时钟位置
    m_clock->setPixelSize(38);
    tick();  // 触发 recalcLayout
    QPoint fromCenter = m_fromRect.center();
    QPoint toCenter = screenGeo.center();
    QSize initSize = m_content->sizeHint();
    m_content->setGeometry(
        fromCenter.x() - initSize.width()  / 2,
        fromCenter.y() - initSize.height() / 2,
        initSize.width(), initSize.height());
    m_content->show();
    show();
    raise();

    // 背景淡入
    auto *bgAnim = new QPropertyAnimation(this, "bgAlpha");
    bgAnim->setDuration(350);
    bgAnim->setStartValue(0.0);
    bgAnim->setEndValue(1.0);
    bgAnim->setEasingCurve(QEasingCurve::OutCubic);
    bgAnim->start(QAbstractAnimation::DeleteWhenStopped);

    // 字体尺寸 + 位置同步插值
    auto *anim = new QVariantAnimation(this);
    anim->setDuration(500);
    anim->setStartValue(0.0);
    anim->setEndValue(1.0);
    anim->setEasingCurve(QEasingCurve::OutCubic);
    connect(anim, &QVariantAnimation::valueChanged, this,
            [this, fromCenter, toCenter](const QVariant &v) {
        double p = v.toDouble();
        m_clock->setPixelSize(38 + (int)((240 - 38) * p));
        QSize sz = m_content->sizeHint();
        m_content->setGeometry(
            lerpI(fromCenter.x(), toCenter.x(), p) - sz.width()  / 2,
            lerpI(fromCenter.y(), toCenter.y(), p) - sz.height() / 2,
            sz.width(), sz.height());
    });
    anim->start(QAbstractAnimation::DeleteWhenStopped);

    m_timer->start(6);
}

void ExamWindow::animateOut()
{
    m_timer->stop();

    QPoint curCenter = m_content->geometry().center();
    QPoint fromCenter = m_fromRect.center();
    int curPx = m_clock->pixelSize();

    auto *bgAnim = new QPropertyAnimation(this, "bgAlpha");
    bgAnim->setDuration(350);
    bgAnim->setStartValue(m_bgAlpha);
    bgAnim->setEndValue(0.0);
    bgAnim->setEasingCurve(QEasingCurve::InCubic);
    bgAnim->start(QAbstractAnimation::DeleteWhenStopped);

    auto *anim = new QVariantAnimation(this);
    anim->setDuration(400);
    anim->setStartValue(0.0);
    anim->setEndValue(1.0);
    anim->setEasingCurve(QEasingCurve::InCubic);
    connect(anim, &QVariantAnimation::valueChanged, this,
            [this, curCenter, fromCenter, curPx](const QVariant &v) {
        double p = v.toDouble();
        m_clock->setPixelSize(curPx + (int)((38 - curPx) * p));
        QSize sz = m_content->sizeHint();
        m_content->setGeometry(
            lerpI(curCenter.x(), fromCenter.x(), p) - sz.width()  / 2,
            lerpI(curCenter.y(), fromCenter.y(), p) - sz.height() / 2,
            sz.width(), sz.height());
    });
    connect(anim, &QVariantAnimation::finished, this, [this]() {
        emit dismissed();
        hide();
    });
    anim->start(QAbstractAnimation::DeleteWhenStopped);
}

// =========================================================================
//  TopBarWindow
// =========================================================================

TopBarWindow::TopBarWindow(QWidget *parent) : QWidget(parent)
{
    Qt::WindowFlags flags = Qt::FramelessWindowHint | Qt::Tool;
    if (TOPBAR_ALWAYS_ON_TOP) flags |= Qt::WindowStaysOnTopHint;
    setWindowFlags(flags);
    setAttribute(Qt::WA_TranslucentBackground);
    setAttribute(Qt::WA_NoSystemBackground);

    setupUi();
    initPlatform();
    applySettings();  // 启动时即应用置顶/独占区块设置
    refresh();

    m_timer = new QTimer(this);
    connect(m_timer, &QTimer::timeout, this, &TopBarWindow::refresh);
    m_timer->start(6);
}

int TopBarWindow::todayIndex()
{
    int dow = QDate::currentDate().dayOfWeek();
    if (dow >= 6) return -1;
    return dow - 1;
}

void TopBarWindow::findNextClasses(int &nextIdx, int &next2Idx) const
{
    nextIdx = -1;
    next2Idx = -1;
    int today = todayIndex();
    if (today < 0) return;

    QTime now = QTime::currentTime();
    for (int i = 0; i < PERIOD_TIMES.size(); ++i) {
        QTime start = QTime::fromString(PERIOD_TIMES[i].start, "HH:mm");
        if (now < start) {
            nextIdx = i;
            for (int j = i + 1; j < PERIOD_TIMES.size(); ++j) {
                const auto &sched = WEEK_SUBJECT[today];
                if (j < sched.size() && !sched[j].isEmpty()) {
                    next2Idx = j;
                    break;
                }
            }
            const auto &sched = WEEK_SUBJECT[today];
            if (nextIdx < sched.size() && sched[nextIdx].isEmpty()
                && next2Idx >= 0) {
                nextIdx = next2Idx;
                for (int j = nextIdx + 1; j < PERIOD_TIMES.size(); ++j) {
                    if (j < sched.size() && !sched[j].isEmpty()) {
                        next2Idx = j;
                        return;
                    }
                }
                next2Idx = -1;
            }
            return;
        }
    }
}

void TopBarWindow::closeEvent(QCloseEvent *)
{
    if (m_settings) m_settings->popOut();
#ifdef Q_OS_WIN
    if (s_workAreaSaved) {
        SystemParametersInfo(SPI_SETWORKAREA, 0, &s_origWorkArea,
                             SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        s_workAreaSaved = false;
    }
#endif
}

void TopBarWindow::paintEvent(QPaintEvent *)
{
    QPainter painter(this);
    painter.setRenderHint(QPainter::Antialiasing);

    // alpha = 191 * (1 - t^3), 起始 75% 不透明度
    QLinearGradient bg(0, 0, 0, height());
    for (int i = 0; i <= 12; ++i) {
        double t = i / 12.0;
        int a = static_cast<int>(223.0 * (1.0 - t * t));
        bg.setColorAt(t, QColor(0, 0, 0, a));
    }

    painter.setPen(Qt::NoPen);
    painter.setBrush(bg);
    painter.drawRect(rect());
}

void TopBarWindow::initPlatform()
{
#ifdef HAS_LAYER_SHELL
    if (qEnvironmentVariableIsSet("WAYLAND_DISPLAY")) {
        setFixedHeight(70);
        winId();
        if (auto *sw = LayerShellQt::Window::get(windowHandle())) {
            sw->setLayer(LayerShellQt::Window::LayerTop);
            using A = LayerShellQt::Window::Anchor;
            sw->setAnchors(
                LayerShellQt::Window::Anchors(A::AnchorTop)
                | A::AnchorLeft | A::AnchorRight);
            sw->setExclusiveZone(TOPBAR_EXCLUSIVE_ZONE ? 70 : 0);
            sw->setKeyboardInteractivity(
                LayerShellQt::Window::KeyboardInteractivityNone);
            m_usingLayerShell = true;
            return;
        }
    }
#endif
    positionAtTop();
}

void TopBarWindow::applySettings()
{
    // 持久化
    QSettings s(QSettings::IniFormat, QSettings::UserScope,
                "HibiBoard", "HibiBoard");
    s.setValue("topbar/exclusiveZone", TOPBAR_EXCLUSIVE_ZONE);
    s.setValue("topbar/alwaysOnTop",   TOPBAR_ALWAYS_ON_TOP);

#ifdef HAS_LAYER_SHELL
    if (m_usingLayerShell) {
        if (auto *sw = LayerShellQt::Window::get(windowHandle())) {
            sw->setExclusiveZone(TOPBAR_EXCLUSIVE_ZONE ? 70 : 0);
            sw->setLayer(TOPBAR_ALWAYS_ON_TOP
                ? LayerShellQt::Window::LayerTop
                : LayerShellQt::Window::LayerBottom);
        }
    }
#endif
#ifndef HAS_LAYER_SHELL
    {
        Qt::WindowFlags flags = Qt::FramelessWindowHint | Qt::Tool;
        if (TOPBAR_ALWAYS_ON_TOP) flags |= Qt::WindowStaysOnTopHint;
        setWindowFlags(flags);
        if (isVisible()) { hide(); show(); }
#ifdef Q_OS_WIN
        HWND hwnd = reinterpret_cast<HWND>(winId());
        SetWindowPos(hwnd,
                     TOPBAR_ALWAYS_ON_TOP ? HWND_TOPMOST : HWND_NOTOPMOST,
                     0, 0, 0, 0,
                     SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        // 独占区块: 裁剪桌面工作区, 最大化窗口自动避让
        if (TOPBAR_EXCLUSIVE_ZONE) {
            if (!s_workAreaSaved) {
                SystemParametersInfo(SPI_GETWORKAREA, 0, &s_origWorkArea, 0);
                s_workAreaSaved = true;
            }
            RECT r = s_origWorkArea;
            r.top += 70;
            SystemParametersInfo(SPI_SETWORKAREA, 0, &r,
                                 SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        } else if (s_workAreaSaved) {
            SystemParametersInfo(SPI_SETWORKAREA, 0, &s_origWorkArea,
                                 SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            s_workAreaSaved = false;
        }
#endif
    }
#endif
}

void TopBarWindow::positionAtTop()
{
    QScreen *screen = QApplication::primaryScreen();
    if (!screen) { setGeometry(0, 0, 1920, 70); return; }
    QRect geo = screen->geometry();
    setGeometry(geo.x(), geo.y(), geo.width(), 70);
}

// ================================================================
// setupUi
/*
    TopBar 层级结构：
    *TopBarWindow (this)
    │
    └── mainLayout (QHBoxLayout)
    │
    ├── leftPanel (QWidget)
    │   │
    │   └── leftLayout (QHBoxLayout)
    │       │
    │       ├── m_dateLabel (QLabel)
    │       ├── leftDiv (QWidget)
    │       ├── m_countdownLabel (QLabel)
    │       ├── WobbleButton × N
    │       └── Stretch
    │
    ├── centerPanel (QWidget)
    │   │
    │   └── centerLayout (QHBoxLayout)
    │       │
    │       ├── Stretch
    │       ├── m_pulseRing (PulseRing)
    │       ├── m_flipClock (FlipClock)
    │       └── Stretch
    │
    └── rightPanel (QWidget)
    │
    └── rightLayout (QHBoxLayout)
    │
    ├── courseCol (QWidget)
    │   │
    │   └── courseLayout (QVBoxLayout)
    │       │
    │       ├── m_nextLabel
    │       └── m_next2Label
    │
    ├── div (QWidget)
    │
    └── schoolCol (QWidget)
    │
    └── schoolLayout (QHBoxLayout)
    │
    ├── nameCol (QWidget)
    │   │
    │   └── nameLayout (QVBoxLayout)
    │       │
    │       ├── m_schoolLabel
    │       └── m_classLabel
    │
    └── m_logoLabel
*/
// ================================================================

void TopBarWindow::setupUi()
{
    QHBoxLayout *mainLayout = new QHBoxLayout(this);
    mainLayout->setContentsMargins(24, 0, 24, 0);
    mainLayout->setSpacing(0);

    // ---- 悬停提示标签 (独立浮动小窗, 可超出顶栏边界, 须在各 Widget 之前创建) ----
    m_hoverTip = new QLabel();    // 无 parent → 独立窗口
    m_hoverTip->setWindowFlags(Qt::ToolTip | Qt::FramelessWindowHint);
    m_hoverTip->setAttribute(Qt::WA_TranslucentBackground);
    m_hoverTip->setAttribute(Qt::WA_ShowWithoutActivating);
    m_hoverTip->setStyleSheet(R"(
        font-size: 15px; color: #e0e0e0;
        background: rgba(30,30,40,210);
        border-radius: 6px; padding: 5px 14px;
    )");
    m_tipOpacity = new QGraphicsOpacityEffect(m_hoverTip);
    m_tipOpacity->setOpacity(0.0);
    m_hoverTip->setGraphicsEffect(m_tipOpacity);

    // ------------------------
    // 左栏 | 日期 | 星期 | 倒计时
    // ------------------------

    // 初始化布局
    QWidget *leftPanel = new QWidget();
    leftPanel->setStyleSheet("background: transparent;");
    QHBoxLayout *leftLayout = new QHBoxLayout(leftPanel);
    leftLayout->setContentsMargins(0, 0, 0, 0);
    leftLayout->setSpacing(20);

    // 日期/星期
    m_dateLabel = new QLabel();
    m_dateLabel->setStyleSheet(R"(
        font-size: 24px; color: #e0e0e8; font-weight: 600;
    )");

    // 日期与倒计时间的竖分割线
    QWidget *leftDiv = new QWidget();
    leftDiv->setFixedSize(1, 40);
    leftDiv->setStyleSheet("background: rgba(255,255,255,0.12);");

    // 倒计时
    m_countdownLabel = new QLabel();
    m_countdownLabel->setStyleSheet(R"(
        font-size: 18px; color: #f0a040;
    )");

    // 倒计时与快捷按钮间的竖分割线
    QWidget *leftDiv2 = new QWidget();
    leftDiv2->setFixedSize(1, 40);
    leftDiv2->setStyleSheet("background: rgba(255,255,255,0.12);");

    leftLayout->addWidget(m_dateLabel);
    leftLayout->addWidget(leftDiv);
    leftLayout->addWidget(m_countdownLabel);
    leftLayout->addWidget(leftDiv2);

    // 快捷按钮
    m_quickBtns = new QuickButtons(m_hoverTip, m_tipOpacity);
    connect(m_quickBtns, &QuickButtons::settingsRequested, this, [this]() {
        if (!m_settings) {
            m_settings = new SettingsWindow(this);
            connect(m_settings, &QWidget::destroyed, this,
                    [this]() { m_settings = nullptr; });
        }
        m_settings->popIn();
    });
    connect(m_quickBtns, &QuickButtons::browserRequested, this, []() {
        QDesktopServices::openUrl(QUrl("about:blank"));
    });
    connect(m_quickBtns, &QuickButtons::fileManagerRequested, this, []() {
        QDesktopServices::openUrl(QUrl::fromLocalFile(QDir::homePath()));
    });
    connect(m_quickBtns, &QuickButtons::toggleClockRequested, this, [this]() {
        if (m_centerPanel)
            m_centerPanel->setVisible(!m_centerPanel->isVisible());
    });
    leftLayout->addWidget(m_quickBtns);

    leftLayout->addStretch();

    // -----------------------
    // 中栏 | 时间圆环 | 翻页时钟
    // -----------------------

    // 初始化布局
    m_centerPanel = new QWidget();
    m_centerPanel->setStyleSheet("background: transparent;");
    m_centerPanel->setCursor(Qt::PointingHandCursor);
    m_centerPanel->installEventFilter(this);
    QHBoxLayout *centerLayout = new QHBoxLayout(m_centerPanel);
    centerLayout->setContentsMargins(0, 0, 0, 0);
    centerLayout->setSpacing(8);

    // 时间圆环
    m_pulseRing = new PulseRing();        // 60×60
    m_pulseRing->setAttribute(Qt::WA_TransparentForMouseEvents);

    // 翻页时钟
    m_flipClock = new FlipClock();        // 宽度自适应
    m_flipClock->setAttribute(Qt::WA_TransparentForMouseEvents);

    // 居中
    centerLayout->addStretch();
    centerLayout->addWidget(m_pulseRing);
    centerLayout->addWidget(m_flipClock);
    centerLayout->addStretch();

    // -----------------------
    // 右栏 | 课程信息 | 学校信息
    // -----------------------

    // 初始化布局
    QWidget *rightPanel = new QWidget();
    rightPanel->setStyleSheet("background: transparent;");
    QHBoxLayout *rightLayout = new QHBoxLayout(rightPanel);
    rightLayout->setContentsMargins(0, 0, 0, 0);
    rightLayout->setSpacing(20);

    // 课程信息
    QWidget *courseCol = new QWidget();
    courseCol->setStyleSheet("background: transparent;");
    QVBoxLayout *courseLayout = new QVBoxLayout(courseCol);
    courseLayout->setContentsMargins(0, 0, 0, 0);
    courseLayout->setSpacing(1);

    m_nextLabel = new QLabel();
    m_nextLabel->setAlignment(Qt::AlignRight | Qt::AlignVCenter);
    m_nextLabel->setStyleSheet(R"(
        font-size: 20px; color: #7ec8e3; font-weight: 600;
    )");
    m_next2Label = new QLabel();
    m_next2Label->setAlignment(Qt::AlignRight | Qt::AlignVCenter);
    m_next2Label->setStyleSheet(R"(
        font-size: 16px; color: #8888a0;
    )");
    courseLayout->addWidget(m_nextLabel);
    courseLayout->addWidget(m_next2Label);

    // 竖分割线
    QWidget *div = new QWidget();
    div->setFixedSize(1, 40);  // 1px宽 40px高, 不撑满
    div->setStyleSheet("background: rgba(255,255,255,0.12);");

    // 校名
    QWidget *schoolCol = new QWidget();
    schoolCol->setStyleSheet("background: transparent;");
    QHBoxLayout *schoolLayout = new QHBoxLayout(schoolCol);
    schoolLayout->setContentsMargins(0, 0, 0, 0);
    schoolLayout->setSpacing(6);

    m_schoolLabel = new QLabel("上海市大同中学");
    m_schoolLabel->setAlignment(Qt::AlignRight | Qt::AlignVCenter);
    // 与下方 classLabel 等宽: 固定最小宽度让短文本撑开
    m_schoolLabel->setStyleSheet(
        "font-size: 11px; color: #9898a8;");

    // 班名
    QWidget *nameCol = new QWidget();
    nameCol->setStyleSheet("background: transparent;");
    QVBoxLayout *nameLayout = new QVBoxLayout(nameCol);
    nameLayout->setContentsMargins(0, 0, 0, 0);
    nameLayout->setSpacing(-2);  // 负间距, 极紧贴合

    m_classLabel = new QLabel("高一8班");
    m_classLabel->setAlignment(Qt::AlignRight | Qt::AlignVCenter);
    m_classLabel->setStyleSheet(
        "font-size: 18px; color: #d0d0e0; font-weight: 600;");

    nameLayout->addWidget(m_schoolLabel);
    nameLayout->addWidget(m_classLabel);

    // 校徽
    m_logoLabel = new QLabel();
    m_logoLabel->setFixedWidth(0);  // 让 QLabel 紧贴 pixmap 实际宽度
    m_logoLabel->setSizePolicy(QSizePolicy::Fixed, QSizePolicy::Fixed);
    QPixmap logo(":/logo");
    if (!logo.isNull()) {
        QPixmap scaled = logo.scaledToHeight(42, Qt::SmoothTransformation);
        m_logoLabel->setPixmap(scaled);
        m_logoLabel->setFixedSize(scaled.size());
    } else {
        m_logoLabel->setText("[徽]");
    }

    schoolLayout->addWidget(nameCol);
    schoolLayout->addWidget(m_logoLabel);

    rightLayout->addStretch();
    rightLayout->addWidget(courseCol);
    rightLayout->addWidget(div,            0, Qt::AlignVCenter);  // 竖线居中
    rightLayout->addWidget(schoolCol,      0, Qt::AlignVCenter);  // 学校列居中

    // ---------------
    // 添加各栏目到总布局
    // ---------------

    mainLayout->addWidget(leftPanel, 1);
    mainLayout->addWidget(m_centerPanel, 1);
    mainLayout->addWidget(rightPanel, 1);
}

void TopBarWindow::refresh()
{
    QDateTime now = QDateTime::currentDateTime();
    int today = todayIndex();

    m_dateLabel->setText(now.toString("M月d日  ddd"));
    qint64 daysLeft = QDate::currentDate().daysTo(SEMESTER_END);
    if (daysLeft > 0)
        m_countdownLabel->setText(QString("距期末 %1 天").arg(daysLeft));
    else if (daysLeft == 0)
        m_countdownLabel->setText("学期最后一天");
    else
        m_countdownLabel->setText("假期啦");

    m_flipClock->setText(now.toString("HH:mm:ss"),
                         "." + now.toString("zzz"));
    m_pulseRing->update();

    if (today < 0) {
        m_nextLabel->setText("周末愉快 ~");
        m_next2Label->setText("");
    } else {
        int nextIdx = -1, next2Idx = -1;
        findNextClasses(nextIdx, next2Idx);
        const auto &sched = WEEK_SUBJECT[today];

        if (nextIdx >= 0 && nextIdx < sched.size()
            && !sched[nextIdx].isEmpty()) {
            QString text = sched[nextIdx];
            if (nextIdx < WEEK_TEACHER[today].size() && !WEEK_TEACHER[today][nextIdx].isEmpty())
                text += " " + WEEK_TEACHER[today][nextIdx];
            m_nextLabel->setText(QString("下一节  %1  %2")
                                     .arg(PERIOD_TIMES[nextIdx].name, text));
        } else {
            m_nextLabel->setText("今日课程已结束");
        }
        if (next2Idx >= 0 && next2Idx < sched.size()
            && !sched[next2Idx].isEmpty()) {
            QString text = sched[next2Idx];
            if (next2Idx < WEEK_TEACHER[today].size() && !WEEK_TEACHER[today][next2Idx].isEmpty())
                text += " " + WEEK_TEACHER[today][next2Idx];
            m_next2Label->setText(QString("下二节  %1  %2")
                                     .arg(PERIOD_TIMES[next2Idx].name, text));
        } else {
            m_next2Label->setText("");
        }
    }
}

bool TopBarWindow::eventFilter(QObject *obj, QEvent *ev)
{
    if (obj == m_centerPanel && ev->type() == QEvent::MouseButtonPress) {
        enterExamMode();
        return true;
    }
    return QWidget::eventFilter(obj, ev);
}

void TopBarWindow::enterExamMode()
{
    if (m_examWindow && m_examWindow->isVisible()) return;

    if (!m_examWindow) {
        m_examWindow = new ExamWindow();
        connect(m_examWindow, &ExamWindow::destroyed, this,
                [this]() { m_examWindow = nullptr; });
        connect(m_examWindow, &ExamWindow::dismissed, this, [this]() {
#ifdef HAS_LAYER_SHELL
            if (m_usingLayerShell) {
                if (auto *sw = LayerShellQt::Window::get(windowHandle())) {
                    sw->setExclusiveZone(TOPBAR_EXCLUSIVE_ZONE ? 70 : 0);
                    sw->setLayer(TOPBAR_ALWAYS_ON_TOP
                        ? LayerShellQt::Window::LayerTop
                        : LayerShellQt::Window::LayerBottom);
                }
            }
#else
            raise();
#endif
        });
    }

    // 推到背景层 + 释放独占区 — 不 hide(), 保持窗口映射状态不变
#ifdef HAS_LAYER_SHELL
    if (m_usingLayerShell) {
        if (auto *sw = LayerShellQt::Window::get(windowHandle())) {
            sw->setExclusiveZone(0);
            sw->setLayer(LayerShellQt::Window::LayerBackground);
        }
    }
#else
    lower();
#endif

    // 获取中栏时钟区域在屏幕上的位置
    QRect clockRect = QRect(
        m_pulseRing->mapToGlobal(QPoint(0, 0)),
        QSize(m_pulseRing->width() + 8 + m_flipClock->width(),
              qMax(m_pulseRing->height(), m_flipClock->height())));

    m_examWindow->animateIn(clockRect);
}
