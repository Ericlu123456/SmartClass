using System;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace smartClass.Services
{
    /// <summary>
    /// TTS 语音播报服务。用于上下课和值日提醒的配音。
    /// 使用 Windows 内置的 SpeechSynthesizer，支持中文语音。
    /// </summary>
    public static class SpeechService
    {
        private static readonly SpeechSynthesizer _synth;
        private static readonly object _lock = new object();
        private static bool _isSpeaking = false;

        static SpeechService()
        {
            _synth = new SpeechSynthesizer();
            _synth.SpeakCompleted += (s, e) => { _isSpeaking = false; };
            ConfigureVoice();
        }

        /// <summary>
        /// 异步播报文本（不阻塞 UI 线程，自动排队）
        /// </summary>
        public static void SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        _isSpeaking = true;
                        // 每个短语之间稍作停顿，避免语速过快
                        _synth.Rate = -1; // 稍慢
                        _synth.Volume = 100;
                        _synth.Speak(text);
                    }
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "语音播报失败");
                }
                finally
                {
                    _isSpeaking = false;
                }
            });
        }

        /// <summary>
        /// 取消当前正在播放的语音
        /// </summary>
        public static void Cancel()
        {
            try
            {
                lock (_lock)
                {
                    if (_isSpeaking)
                    {
                        _synth.SpeakAsyncCancelAll();
                        _isSpeaking = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "取消语音播报失败");
            }
        }

        /// <summary>
        /// 自动选择中文语音引擎，若无则使用默认引擎
        /// </summary>
        private static void ConfigureVoice()
        {
            try
            {
                // 优先选择简体中文女声 (Microsoft Huihui / Microsoft Kangkang)
                foreach (var voice in _synth.GetInstalledVoices())
                {
                    var info = voice.VoiceInfo;
                    if (info.Culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    {
                        _synth.SelectVoice(info.Name);
                        LogService.Log($"语音引擎已选择: {info.Name} ({info.Culture})");
                        return;
                    }
                }
                // 没有中文语音，使用系统默认
                LogService.Log($"未找到中文语音引擎，使用默认: {_synth.Voice.Name}");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "配置语音引擎失败");
            }
        }
    }
}
