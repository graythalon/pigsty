using System;
using System.Collections.Generic;
using System.ComponentModel;
using Pigsty.Helpers;

namespace Pigsty.Models
{
    public class StorageVolumeInfo
    {
        public string Name { get; set; }
        public long TotalBytes { get; set; }
        public long FreeBytes { get; set; }
        public dynamic ShellFolder { get; set; }
    }

    public class AndroidDevice : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _storageName;
        private long _totalSpaceBytes;
        private long _freeSpaceBytes;
        private bool _isDemo;

        public AndroidDevice()
        {
            StorageVolumes = new List<StorageVolumeInfo>();
        }

        public string Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string StorageName
        {
            get { return _storageName; }
            set
            {
                if (_storageName != value)
                {
                    _storageName = value;
                    OnPropertyChanged("StorageName");
                }
            }
        }

        public bool IsDemo
        {
            get { return _isDemo; }
            set
            {
                if (_isDemo != value)
                {
                    _isDemo = value;
                    OnPropertyChanged("IsDemo");
                }
            }
        }

        public long TotalSpaceBytes
        {
            get { return _totalSpaceBytes; }
            set
            {
                if (_totalSpaceBytes != value)
                {
                    _totalSpaceBytes = value;
                    OnPropertyChanged("TotalSpaceBytes");
                    OnPropertyChanged("UsedSpaceBytes");
                    OnPropertyChanged("FormattedTotalSpace");
                    OnPropertyChanged("FormattedUsedSpace");
                    OnPropertyChanged("PercentUsed");
                }
            }
        }

        public long FreeSpaceBytes
        {
            get { return _freeSpaceBytes; }
            set
            {
                if (_freeSpaceBytes != value)
                {
                    _freeSpaceBytes = value;
                    OnPropertyChanged("FreeSpaceBytes");
                    OnPropertyChanged("UsedSpaceBytes");
                    OnPropertyChanged("FormattedFreeSpace");
                    OnPropertyChanged("FormattedUsedSpace");
                    OnPropertyChanged("PercentUsed");
                }
            }
        }

        public long UsedSpaceBytes
        {
            get
            {
                long used = _totalSpaceBytes - _freeSpaceBytes;
                return used > 0 ? used : 0;
            }
        }

        public double PercentUsed
        {
            get
            {
                if (_totalSpaceBytes <= 0) return 0.0;
                double pct = ((double)UsedSpaceBytes / _totalSpaceBytes) * 100.0;
                return Math.Min(100.0, Math.Max(0.0, pct));
            }
        }

        public string FormattedTotalSpace
        {
            get { return SizeFormatter.FormatBytes(_totalSpaceBytes); }
        }

        public string FormattedUsedSpace
        {
            get { return SizeFormatter.FormatBytes(UsedSpaceBytes); }
        }

        public string FormattedFreeSpace
        {
            get { return SizeFormatter.FormatBytes(_freeSpaceBytes); }
        }

        public List<StorageVolumeInfo> StorageVolumes { get; set; }
        public dynamic RootShellFolder { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
