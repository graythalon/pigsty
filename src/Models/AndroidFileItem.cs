using System;
using System.ComponentModel;
using Pigsty.Helpers;

namespace Pigsty.Models
{
    public class AndroidFileItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _name;
        private long _sizeBytes;
        private DateTime? _dateModified;
        private string _directoryPath;
        private CategoryType _category;
        private bool _isFolder;

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

        public string Extension
        {
            get
            {
                if (string.IsNullOrEmpty(_name)) return "";
                int idx = _name.LastIndexOf('.');
                return idx >= 0 ? _name.Substring(idx).ToLowerInvariant() : "";
            }
        }

        public long SizeBytes
        {
            get { return _sizeBytes; }
            set
            {
                if (_sizeBytes != value)
                {
                    _sizeBytes = value;
                    OnPropertyChanged("SizeBytes");
                    OnPropertyChanged("FormattedSize");
                    OnPropertyChanged("IsLargeFile");
                }
            }
        }

        public string FormattedSize
        {
            get
            {
                if (IsFolder) return "--";
                return SizeFormatter.FormatBytes(_sizeBytes);
            }
        }

        public DateTime? DateModified
        {
            get { return _dateModified; }
            set
            {
                if (_dateModified != value)
                {
                    _dateModified = value;
                    OnPropertyChanged("DateModified");
                    OnPropertyChanged("FormattedDate");
                }
            }
        }

        public string FormattedDate
        {
            get
            {
                if (!_dateModified.HasValue) return "-";
                return _dateModified.Value.ToString("yyyy-MM-dd HH:mm");
            }
        }

        public string DirectoryPath
        {
            get { return _directoryPath; }
            set
            {
                if (_directoryPath != value)
                {
                    _directoryPath = value;
                    OnPropertyChanged("DirectoryPath");
                }
            }
        }

        public CategoryType Category
        {
            get { return _category; }
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged("Category");
                    OnPropertyChanged("CategoryName");
                }
            }
        }

        public string CategoryName
        {
            get
            {
                switch (_category)
                {
                    case CategoryType.Photos: return "Photos";
                    case CategoryType.Videos: return "Videos";
                    case CategoryType.Audio: return "Audio";
                    case CategoryType.Documents: return "Documents";
                    case CategoryType.Apks: return "APKs";
                    case CategoryType.Downloads: return "Downloads";
                    case CategoryType.Archives: return "Archives";
                    case CategoryType.LargeFiles: return "Large File";
                    default: return "Other";
                }
            }
        }

        public string IconGlyph
        {
            get
            {
                if (IsFolder) return "📁";
                return FileFormatHelper.GetGlyphForExtension(Extension);
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

        public bool IsLargeFile
        {
            get { return _sizeBytes >= FileFormatHelper.LargeFileThresholdBytes; }
        }

        public bool IsFolder
        {
            get { return _isFolder; }
            set
            {
                if (_isFolder != value)
                {
                    _isFolder = value;
                    OnPropertyChanged("IsFolder");
                    OnPropertyChanged("FormattedSize");
                    OnPropertyChanged("IconGlyph");
                }
            }
        }

        // Native Shell references for real MTP transfers
        public dynamic ShellItem { get; set; }
        public dynamic ParentFolder { get; set; }

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
