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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
