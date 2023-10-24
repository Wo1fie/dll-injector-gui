using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace dll_injector_gui
{
    class FormattedFileInfo
    {
        FileInfo _fileInfo;
        public FormattedFileInfo(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }
        public FileInfo FileInfo { get { return _fileInfo; } }
        public override string ToString()
        {
            return $"{_fileInfo.Name}";
        }
    }
}
