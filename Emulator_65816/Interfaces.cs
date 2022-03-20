using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emul816or
{
    interface IMemoryIO
    {
        uint Size
        {
            get;
        }
        uint BaseAddress
        {
            get;
        }
        bool Supports16Bit
        {
            get;
        }
        Byte this[uint index]
        {
            get;
            set;
        }
    }
}
