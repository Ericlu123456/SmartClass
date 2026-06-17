using System;
using System.Diagnostics;
using System.Threading;

namespace smartClass.Services
{
    /// <summary>
    /// 管理应用单实例运行
    /// </summary>
    public class SingleInstanceManager
    {
        private readonly string _mutexName;
        private Mutex _mutex;

        public SingleInstanceManager(string appName = "SmartClass")
        {
            _mutexName = $"Global\\{appName}_SingleInstance_{Environment.MachineName}";
        }

        /// <summary>
        /// 检查是否已有其他实例运行
        /// </summary>
        /// <returns>如果已有实例运行则返回 true，否则返回 false</returns>
        public bool IsAnotherInstanceRunning()
        {
            try
            {
                _mutex = new Mutex(true, _mutexName, out bool createdNew);

                if (createdNew)
                {
                    // 成功创建互斥体，说明这是第一个实例
                    return false;
                }
                else
                {
                    // 互斥体已存在，说明已有其他实例在运行
                    _mutex?.Dispose();
                    _mutex = null;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to check single instance: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 释放互斥体资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                    _mutex = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to dispose mutex: {ex.Message}");
            }
        }
    }
}
