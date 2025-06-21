using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using SYSTools.Helpers;
using System.Windows.Media;

namespace SYSTools.Model
{
    public class ToolItem : INotifyPropertyChanged
    {
        private ImageSource? _iconSource;
        public ImageSource? IconSource
        {
            get => _iconSource;
            set
            {
                if (_iconSource != value)
                {
                    _iconSource = value;
                    OnPropertyChanged(nameof(IconSource));
                }
            }
        }
        private string _name = string.Empty;
        private string _iconPath = string.Empty;
        private string _exePath = string.Empty;
        private string _arguments = string.Empty;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string IconPath
        {
            get => _iconPath;
            set
            {
                if (_iconPath != value)
                {
                    _iconPath = value;
                    OnPropertyChanged(nameof(IconPath));
                }
            }
        }

        public string ExePath
        {
            get => _exePath;
            set
            {
                if (_exePath != value)
                {
                    _exePath = value;
                    OnPropertyChanged(nameof(ExePath));
                }
            }
        }

        public string Arguments
        {
            get => _arguments;
            set
            {
                if (_arguments != value)
                {
                    _arguments = value;
                    OnPropertyChanged(nameof(Arguments));
                }
            }
        }

        public ICommand LaunchCommand { get; }

        public ToolItem()
        {
            LaunchCommand = new RelayCommand(_ => Launch());
        }

        private void Launch()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Arguments = Arguments,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法启动工具 '{Name}': {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}