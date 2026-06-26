using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using smartClass.Models;

namespace smartClass.Services
{
    /// <summary>
    /// 线程安全的应用状态持久化服务。
    /// 特性：原子写入（先写 .tmp 再重命名）、自动备份（.bak）、加载失败时从备份恢复、重试机制。
    /// </summary>
    public static class StorageService
    {
        private static readonly string StateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appstate.json");
        private static readonly string TempFile = StateFile + ".tmp";
        private static readonly string BackupFile = StateFile + ".bak";
        private static readonly object _lockObject = new object();
        private const int MaxRetries = 5;
        private const int RetryDelayMs = 100;

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true // 兼容不同大小写的 JSON
        };

        /// <summary>
        /// 加载应用状态。优先读取主文件，失败时尝试从备份恢复。
        /// 任何情况下都不抛出异常，保证返回一个可用的 AppState。
        /// </summary>
        public static AppState Load()
        {
            lock (_lockObject)
            {
                // 尝试从主文件加载
                var result = TryLoadFromFile(StateFile);
                if (result != null)
                    return result;

                // 主文件失败，尝试从备份恢复
                LogService.Log("主配置文件加载失败，尝试从备份恢复");
                result = TryLoadFromFile(BackupFile);
                if (result != null)
                {
                    LogService.Log("从备份文件恢复成功");
                    // 将恢复的数据写回主文件
                    SaveInternal(result);
                    return result;
                }

                // 两者都失败，创建全新的默认状态
                LogService.LogError("配置文件加载", "主文件和备份均无法读取，使用默认配置");
                var defaultState = new AppState();
                SaveInternal(defaultState);
                return defaultState;
            }
        }

        /// <summary>
        /// 保存应用状态。先写临时文件，成功后原子替换目标文件。
        /// 保留上一次成功保存的备份。
        /// </summary>
        public static void Save(AppState state)
        {
            if (state == null)
            {
                LogService.LogError("StorageService.Save", "尝试保存 null 状态，操作被拒绝");
                return;
            }

            lock (_lockObject)
            {
                SaveInternal(state);
            }
        }

        /// <summary>
        /// 内部保存逻辑（调用方必须持有锁）
        /// </summary>
        private static void SaveInternal(AppState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, SerializerOptions);

                // 验证序列化后的 JSON 可以反序列化
                try
                {
                    var validation = JsonSerializer.Deserialize<AppState>(json, SerializerOptions);
                    if (validation == null)
                    {
                        throw new InvalidOperationException("序列化后反序列化验证返回 null");
                    }
                }
                catch (Exception vex)
                {
                    LogService.Log(vex, "保存前JSON验证失败，拒绝写入以避免数据损坏");
                    return; // 验证失败，不写入，保护现有数据
                }

                // 原子写入：先写临时文件
                WriteFileWithRetry(TempFile, json);

                // 备份当前主文件（如果存在）
                if (File.Exists(StateFile))
                {
                    try
                    {
                        File.Copy(StateFile, BackupFile, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        LogService.Log(ex, "备份旧配置文件失败(非致命)");
                    }
                }

                // 原子替换：将临时文件重命名为正式文件
                File.Move(TempFile, StateFile, overwrite: true);
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "保存配置失败");

                // 清理可能残留的临时文件
                try { if (File.Exists(TempFile)) File.Delete(TempFile); } catch { }
            }
        }

        /// <summary>
        /// 尝试从指定路径加载 AppState。成功返回对象，失败返回 null。
        /// </summary>
        private static AppState TryLoadFromFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                var json = ReadFileWithRetry(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LogService.LogError("配置加载", $"文件 {Path.GetFileName(path)} 为空");
                    return null;
                }

                var state = JsonSerializer.Deserialize<AppState>(json, SerializerOptions);
                if (state == null)
                {
                    LogService.LogError("配置加载", $"文件 {Path.GetFileName(path)} 反序列化结果为 null");
                    return null;
                }

                // 修复可能的 null 集合（JSON 中缺失的属性）
                state.Students ??= new System.Collections.Generic.List<Student>();
                state.Courses ??= new System.Collections.Generic.List<Course>();
                state.DutyGroups ??= new System.Collections.Generic.List<DutyGroup>();
                state.DailyDuties ??= new System.Collections.Generic.List<DailyDuty>();

                // 修复无效的 FontSize
                if (double.IsNaN(state.FontSize) || double.IsInfinity(state.FontSize) || state.FontSize < 1)
                    state.FontSize = 14.0;

                // 修复无效的关机时间
                if (string.IsNullOrWhiteSpace(state.AutoShutdownTime))
                    state.AutoShutdownTime = "23:00";

                return state;
            }
            catch (JsonException ex)
            {
                LogService.Log(ex, $"JSON 解析失败: {Path.GetFileName(path)}");
                return null;
            }
            catch (Exception ex)
            {
                LogService.Log(ex, $"读取文件失败: {Path.GetFileName(path)}");
                return null;
            }
        }

        private static string ReadFileWithRetry(string path)
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch (IOException) when (i < MaxRetries - 1)
                {
                    Thread.Sleep(RetryDelayMs);
                }
            }
            // 最后一次尝试，让异常向上传播
            return File.ReadAllText(path);
        }

        private static void WriteFileWithRetry(string path, string content)
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    File.WriteAllText(path, content);
                    return;
                }
                catch (IOException) when (i < MaxRetries - 1)
                {
                    Thread.Sleep(RetryDelayMs);
                }
            }
            // 最后一次尝试，让异常向上传播
            File.WriteAllText(path, content);
        }
    }
}
