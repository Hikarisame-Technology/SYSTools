using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SYSTools.Model
{
    public class GlobalSettings : INotifyPropertyChanged
    {
        private static GlobalSettings _instance = new GlobalSettings();
        public static GlobalSettings Instance => _instance;

        private string _backgroundImagePath;
        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set
            {
                if (_backgroundImagePath != value)
                {
                    _backgroundImagePath = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _backgroundImageBlurRadius;
        public double BackgroundImageBlurRadius
        {
            get => _backgroundImageBlurRadius;
            set
            {
                if (_backgroundImageBlurRadius != value)
                {
                    _backgroundImageBlurRadius = value;
                    OnPropertyChanged();
                    Properties.Settings.Default.BackgroundImageBlurRadius =
                        _backgroundImageBlurRadius;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
