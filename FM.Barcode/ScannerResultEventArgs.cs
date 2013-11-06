using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace FM.Barcode
{
    public class ScannerResultEventArgs : EventArgs
    {
        private readonly Result _ScanResult;
        public Result ScanResult { get { return _ScanResult; } }

        public ScannerResultEventArgs(Result result)
        {
            _ScanResult = result;
        }
    }
}
