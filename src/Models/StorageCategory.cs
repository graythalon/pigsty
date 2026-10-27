using System;
using System.ComponentModel;

namespace Pigsty.Models
{
    public enum CategoryType
    {
        Overview,
        Photos,
        Videos,
        Audio,
        Documents,
        Apks,
        Downloads,
        Archives,
        LargeFiles,
        AllFiles
    }

    public class CategoryInfo : INotifyPropertyChanged
    {
        private int _fileCount;
        private long _totalSizeBytes;
        private string _formattedTotalSize;
        private double _percentage;
        private bool _isSelected;

        public CategoryType Type { get; set; }
        public string DisplayName { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public string ColorHex { get; set; }

        public int FileCount
        {
            get { return _fileCount; }
            set
            {
                if (_fileCount != value)
                {
                    _fileCount = value;
                    OnPropertyChanged("FileCount");
                }
            }
        }

        public long TotalSizeBytes
        {
            get { return _totalSizeBytes; }
            set
            {
                if (_totalSizeBytes != value)
                {
                    _totalSizeBytes = value;
                    OnPropertyChanged("TotalSizeBytes");
                }
            }
        }

        public string FormattedTotalSize
        {
            get { return _formattedTotalSize; }
            set
            {
                if (_formattedTotalSize != value)
                {
                    _formattedTotalSize = value;
                    OnPropertyChanged("FormattedTotalSize");
                }
            }
        }

        public double Percentage
        {
            get { return _percentage; }
            set
            {
                if (Math.Abs(_percentage - value) > 0.001)
                {
                    _percentage = value;
                    OnPropertyChanged("Percentage");
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

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
