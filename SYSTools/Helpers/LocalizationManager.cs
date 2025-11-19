using System;
using System.ComponentModel;
using System.Globalization;
using SYSTools.Properties;
using System.Windows.Markup;
using System.Windows.Data;

namespace SYSTools.Helpers
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private LocalizationManager() { }

        public static LocalizationManager Instance { get; } = new LocalizationManager();

        private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;
        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    // 传递属性名以便订阅者识别变化，同时兼容空字符串（触发所有绑定更新）
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                }
            }
        }

        public string this[string key] =>
            Lang.ResourceManager.GetString(key, _currentCulture) ?? string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
    
} 