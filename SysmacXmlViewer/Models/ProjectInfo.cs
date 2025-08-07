using System.ComponentModel;

namespace SysmacXmlViewer.Models
{
    public class ProjectInfo : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _tracking = string.Empty;
        private string _deviceType = string.Empty;
        private string _deviceModelFull = string.Empty;
        private string _unitVersion = string.Empty;
        private string _version = string.Empty;
        private bool _enableOffset;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Tracking
        {
            get => _tracking;
            set
            {
                _tracking = value;
                OnPropertyChanged();
            }
        }

        public string DeviceType
        {
            get => _deviceType;
            set
            {
                _deviceType = value;
                OnPropertyChanged();
            }
        }

        public string DeviceModelFull
        {
            get => _deviceModelFull;
            set
            {
                _deviceModelFull = value;
                OnPropertyChanged();
            }
        }

        public string UnitVersion
        {
            get => _unitVersion;
            set
            {
                _unitVersion = value;
                OnPropertyChanged();
            }
        }

        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged();
            }
        }

        public bool EnableOffset
        {
            get => _enableOffset;
            set
            {
                _enableOffset = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 