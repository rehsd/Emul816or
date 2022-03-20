using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator_65816
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
