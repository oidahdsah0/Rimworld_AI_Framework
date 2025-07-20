using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using RimAI.Framework.Core;
using Verse;
using static RimAI.Framework.Core.RimAILogger;

namespace RimAI.Framework.Configuration
{
    /// <summary>
    /// RimAI 配置管理系统，提供集中的配置管理功能
    /// </summary>
    public class RimAIConfiguration : IDisposable
    {
        private static RimAIConfiguration _instance;
        private static readonly object _lockObject = new object();
        
        private readonly Dictionary<string, object> _settings;
        private readonly FileSystemWatcher _configWatcher;
        private readonly Timer _saveTimer;
        private readonly object _settingsLock = new object();
        
        private readonly string _configFilePath;
        private readonly string _backupFilePath;
        private bool _disposed;
        private bool _isDirty;
        
        // 配置变更事件
        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
        
        /// <summary>
        /// 获取配置管理器的单例实例
        /// </summary>
        public static RimAIConfiguration Instance
        {
            get
            {
                if (_instance == null || _instance._disposed)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null || _instance._disposed)
                        {
                            if (_instance?._disposed == true)
                            {
                                Warning("Configuration was disposed, creating new instance");
                            }
                            _instance = new RimAIConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 配置文件路径
        /// </summary>
        public string ConfigFilePath => _configFilePath;
        
        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public bool IsDirty => _isDirty;
        
        /// <summary>
        /// 配置项数量
        /// </summary>
        public int Count => _settings.Count;
        
        /// <summary>
        /// 是否已被释放
        /// </summary>
        public bool IsDisposed => _disposed;
        
        private RimAIConfiguration()
        {
            _settings = new Dictionary<string, object>();
            
            // 确定配置文件路径 - 放在MOD目录内
            var modPath = Path.GetDirectoryName(typeof(RimAIConfiguration).Assembly.Location);
            var configDir = Path.Combine(modPath, "..", "Config");
            Directory.CreateDirectory(configDir);
            
            _configFilePath = Path.Combine(configDir, "RimAI_Settings.json");
            _backupFilePath = _configFilePath + ".backup";
            
            // 初始化默认配置
            InitializeDefaults();
            
            // 加载现有配置
            LoadConfiguration();
            
            // 设置文件监控
            try
            {
                _configWatcher = new FileSystemWatcher(Path.GetDirectoryName(_configFilePath));
                _configWatcher.Filter = Path.GetFileName(_configFilePath);
                _configWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                _configWatcher.Changed += OnConfigFileChanged;
                _configWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Warning("Failed to setup config file watcher: {0}", ex.Message);
            }
            
            // 自动保存定时器 - 每30秒检查是否需要保存
            _saveTimer = new Timer(
                AutoSave,
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30)
            );
            
            Info("RimAI Configuration initialized. Config file: {0}", _configFilePath);
        }
        
        /// <summary>
        /// 初始化默认配置值
        /// </summary>
        private void InitializeDefaults()
        {
            var defaults = new Dictionary<string, object>
            {
                // API配置 - API key不设置默认值！
                // ["api.key"] = "", // 移除默认值，确保API key必须由用户提供
                ["api.endpoint"] = "https://api.openai.com/v1",
                ["api.model"] = "gpt-4o",
                ["api.temperature"] = 0.7f,
                ["api.maxTokens"] = 1000,
                ["api.enableStreaming"] = false,
                
                // 性能设置
                ["performance.timeoutSeconds"] = 30,
                ["performance.retryCount"] = 3,
                ["performance.maxConcurrentRequests"] = 5,
                
                // 缓存设置
                ["cache.enabled"] = true,
                ["cache.size"] = 1000,
                ["cache.ttlMinutes"] = 30,
                
                // 批处理设置
                ["batch.size"] = 5,
                ["batch.timeoutSeconds"] = 2,
                
                // 日志设置
                ["logging.enableDetailed"] = false,
                ["logging.level"] = 1, // Info
                
                // 健康检查设置
                ["health.enableChecks"] = true,
                ["health.intervalMinutes"] = 5,
                ["health.enableMemoryMonitoring"] = true,
                ["health.memoryThresholdMB"] = 100,
                
                // 嵌入设置 - embedding key也不设置默认值
                ["embedding.enabled"] = false,
                // ["embedding.key"] = "", // 移除默认值，如果需要embedding key必须由用户提供
                ["embedding.endpoint"] = "https://api.openai.com/v1",
                ["embedding.model"] = "text-embedding-3-small",
                
                // 遗留设置（保持向后兼容）
                ["Logging.Level"] = "Info",
                ["Logging.EnableDebug"] = false,
                ["Http.Timeout"] = 30000,
                ["Http.RetryCount"] = 3,
                ["Http.MaxConnections"] = 20,
                ["Cache.MaxSize"] = 1000,
                ["Cache.DefaultExpiration"] = 1800000, // 30分钟
                ["Performance.EnableBatching"] = true,
                ["Performance.EnableCaching"] = true,
                ["Security.ValidateSSL"] = true
            };
            
            foreach (var kvp in defaults)
            {
                _settings[kvp.Key] = kvp.Value;
            }
        }
        
        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public T Get<T>(string key, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;
                
            lock (_settingsLock)
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    try
                    {
                        if (value is T directValue)
                        {
                            return directValue;
                        }
                        
                        // 尝试类型转换
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (Exception ex)
                    {
                        Warning("Failed to convert config value '{0}' to type {1}: {2}", key, typeof(T).Name, ex.Message);
                        return defaultValue;
                    }
                }
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// 设置配置值
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        /// <param name="saveImmediately">是否立即保存</param>
        public void Set(string key, object value, bool saveImmediately = false)
        {
            if (string.IsNullOrEmpty(key))
                return;
                
            object oldValue = null;
            bool hasChanged = false;
            
            lock (_settingsLock)
            {
                _settings.TryGetValue(key, out oldValue);
                
                // 检查值是否真的发生了变化
                if (!Equals(oldValue, value))
                {
                    _settings[key] = value;
                    _isDirty = true;
                    hasChanged = true;
                }
            }
            
            if (hasChanged)
            {
                Debug("Configuration changed: {0} = {1} (was: {2})", key, value, oldValue);
                
                // 触发变更事件
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(key, value, oldValue));
                
                if (saveImmediately)
                {
                    SaveConfiguration();
                }
            }
        }
        
        /// <summary>
        /// 获取指定前缀的所有配置
        /// </summary>
        /// <param name="prefix">配置键前缀</param>
        /// <returns>匹配的配置项</returns>
        public Dictionary<string, object> GetByPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return new Dictionary<string, object>();
                
            var result = new Dictionary<string, object>();
            
            lock (_settingsLock)
            {
                foreach (var kvp in _settings)
                {
                    if (kvp.Key.StartsWith(prefix))
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 删除配置项
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>是否成功删除</returns>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
                
            bool removed = false;
            object oldValue = null;
            
            lock (_settingsLock)
            {
                if (_settings.TryGetValue(key, out oldValue))
                {
                    _settings.Remove(key);
                    _isDirty = true;
                    removed = true;
                }
            }
            
            if (removed)
            {
                Debug("Configuration removed: {0} (was: {1})", key, oldValue);
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(key, null, oldValue));
            }
            
            return removed;
        }
        
        /// <summary>
        /// 检查配置键是否存在
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>是否存在</returns>
        public bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
                
            lock (_settingsLock)
            {
                return _settings.ContainsKey(key);
            }
        }
        
        /// <summary>
        /// 获取所有配置键
        /// </summary>
        /// <returns>配置键列表</returns>
        public List<string> GetAllKeys()
        {
            lock (_settingsLock)
            {
                return new List<string>(_settings.Keys);
            }
        }
        
        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void SaveConfiguration()
        {
            if (_disposed)
                return;
                
            try
            {
                Dictionary<string, object> settingsCopy;
                
                lock (_settingsLock)
                {
                    settingsCopy = new Dictionary<string, object>(_settings);
                    _isDirty = false;
                }
                
                // 创建备份
                if (File.Exists(_configFilePath))
                {
                    File.Copy(_configFilePath, _backupFilePath, true);
                }
                
                // 保存到临时文件，然后替换
                var tempFile = _configFilePath + ".tmp";
                var json = JsonConvert.SerializeObject(settingsCopy, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(tempFile, json);
                
                // 原子替换
                if (File.Exists(_configFilePath))
                {
                    File.Delete(_configFilePath);
                }
                File.Move(tempFile, _configFilePath);
                
                Debug("Configuration saved to: {0}", _configFilePath);
            }
            catch (Exception ex)
            {
                Error("Failed to save configuration: {0}", ex.Message);
                
                // 尝试从备份恢复
                if (File.Exists(_backupFilePath))
                {
                    try
                    {
                        File.Copy(_backupFilePath, _configFilePath, true);
                        Warning("Configuration restored from backup");
                    }
                    catch (Exception restoreEx)
                    {
                        Error("Failed to restore configuration from backup: {0}", restoreEx.Message);
                    }
                }
            }
        }
        
        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public void LoadConfiguration()
        {
            if (!File.Exists(_configFilePath))
            {
                Debug("Configuration file not found, using defaults");
                return;
            }
            
            try
            {
                var json = File.ReadAllText(_configFilePath);
                var loadedSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                
                if (loadedSettings != null)
                {
                    lock (_settingsLock)
                    {
                        // 合并加载的配置和默认配置
                        foreach (var kvp in loadedSettings)
                        {
                            _settings[kvp.Key] = kvp.Value;
                        }
                        _isDirty = false;
                    }
                    
                    Info("Configuration loaded from: {0} ({1} settings)", _configFilePath, loadedSettings.Count);
                }
            }
            catch (Exception ex)
            {
                Error("Failed to load configuration: {0}", ex.Message);
                
                // 尝试从备份加载
                if (File.Exists(_backupFilePath))
                {
                    try
                    {
                        var backupJson = File.ReadAllText(_backupFilePath);
                        var backupSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(backupJson);
                        
                        if (backupSettings != null)
                        {
                            lock (_settingsLock)
                            {
                                foreach (var kvp in backupSettings)
                                {
                                    _settings[kvp.Key] = kvp.Value;
                                }
                                _isDirty = false;
                            }
                            
                            Warning("Configuration loaded from backup");
                        }
                    }
                    catch (Exception backupEx)
                    {
                        Error("Failed to load configuration backup: {0}", backupEx.Message);
                    }
                }
            }
        }
        
        /// <summary>
        /// 重置为默认配置
        /// </summary>
        /// <param name="saveImmediately">是否立即保存</param>
        public void ResetToDefaults(bool saveImmediately = true)
        {
            lock (_settingsLock)
            {
                _settings.Clear();
                InitializeDefaults();
                _isDirty = true;
            }
            
            Info("Configuration reset to defaults");
            
            if (saveImmediately)
            {
                SaveConfiguration();
            }
        }
        
        /// <summary>
        /// 获取配置摘要信息
        /// </summary>
        public ConfigurationInfo GetInfo()
        {
            lock (_settingsLock)
            {
                return new ConfigurationInfo
                {
                    ConfigFilePath = _configFilePath,
                    BackupFilePath = _backupFilePath,
                    SettingsCount = _settings.Count,
                    IsDirty = _isDirty,
                    FileExists = File.Exists(_configFilePath),
                    BackupExists = File.Exists(_backupFilePath),
                    LastModified = File.Exists(_configFilePath) ? File.GetLastWriteTime(_configFilePath) : (DateTime?)null
                };
            }
        }
        
        /// <summary>
        /// 配置文件变更事件处理
        /// </summary>
        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_disposed)
                return;
                
            // 延迟一点再加载，避免文件还在写入过程中
            Task.Delay(100).ContinueWith(_ =>
            {
                if (!_disposed)
                {
                    Debug("Configuration file changed, reloading...");
                    LoadConfiguration();
                }
            });
        }
        
        /// <summary>
        /// 自动保存处理
        /// </summary>
        private void AutoSave(object state)
        {
            if (_disposed || !_isDirty)
                return;
                
            try
            {
                SaveConfiguration();
            }
            catch (Exception ex)
            {
                Error("Auto-save failed: {0}", ex.Message);
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            try
            {
                _configWatcher?.Dispose();
                _saveTimer?.Dispose();
                
                // 保存未保存的更改
                if (_isDirty)
                {
                    SaveConfiguration();
                }
                
                var info = GetInfo();
                Info("RimAI Configuration disposed. Final state - Settings: {0}, Saved: {1}", 
                     info.SettingsCount, !info.IsDirty);
            }
            catch (Exception ex)
            {
                Error("Error disposing RimAI Configuration: {0}", ex.Message);
            }
        }
    }
    
    /// <summary>
    /// 配置变更事件参数
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; }
        public object NewValue { get; }
        public object OldValue { get; }
        
        public ConfigurationChangedEventArgs(string key, object newValue, object oldValue)
        {
            Key = key;
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
    
    /// <summary>
    /// 配置信息
    /// </summary>
    public class ConfigurationInfo
    {
        public string ConfigFilePath { get; set; }
        public string BackupFilePath { get; set; }
        public int SettingsCount { get; set; }
        public bool IsDirty { get; set; }
        public bool FileExists { get; set; }
        public bool BackupExists { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
