using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace dll_injector_gui
{
    class FormattedProcess
    {
        Process _process;
        public FormattedProcess(Process process)
        {
            _process = process;
        }
        public Process Process {get {return _process; }}

        public override string ToString()
        {
            return $"{_process.ProcessName} - {_process.Id}";
        }
    }
}
