using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emul816or
{
    public class NullDevice : IMemoryIO
    {
        public uint Size
        {
            get
            {
                throw new Exception("Access (read size) to unsupported memory address!");
            }
        }
        public uint BaseAddress
        {
            get
            {
                throw new Exception("Access (read base address) to unsupported memory address!");
            }
        }

        public bool Supports16Bit
        {
            get
            {
                throw new Exception("Access (supports 16 bit) to unsupported memory address!");
            }
        }

        public byte[] MemoryBytes => throw new NotImplementedException();

        public byte this[uint index]
        {
            get
            {
                throw new Exception("Access (read) to unsupported memory address!");
            }
            set
            {
                throw new Exception("Access (write) to unsupported memory address!");
            }
        }
    }
}
