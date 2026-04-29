using Microsoft.Win32;
using System;
using System.Windows;

namespace SYSTools.Updater.Services
{
    public class ThemeService
    {
        public event Action<bool> ThemeChanged;

        public bool IsDarkMode { get; private set; }

        public ThemeService()
        {
            IsDarkMode = IsSystemDarkMode();
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                IsDarkMode = IsSystemDarkMode();
                Application.Current.Dispatcher.Invoke(() => ThemeChanged?.Invoke(IsDarkMode));
            }
        }

        public bool IsSystemDarkMode()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("AppsUseLightTheme");
                        if (value != null)
                        {
                            return (int)value == 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeService] 读取注册表主题失败: {ex.Message}");
            }
            return false;
        }

        public void Cleanup()
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }
    }
} 