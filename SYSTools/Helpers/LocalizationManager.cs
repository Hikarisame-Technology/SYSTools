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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                }
            }
        }

        public string this[string key] =>
            Lang.ResourceManager.GetString(key, _currentCulture) ?? string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
    
} 