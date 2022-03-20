using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emul816or
{
    public class RAM : IMemoryIO
    {
        private byte[] data;
        const uint size = 32768;
        const uint baseAddress = 0x000000;
        private bool supports16bit = true;
        public uint Size
        {
            get => size;
        }
        public uint BaseAddress
        {
            get => baseAddress;
        }
        public byte this[uint index]
        {
            get => data[index - baseAddress];
            set => data[index - baseAddress] = value;
        }
        public bool Supports16Bit
        {
            get => supports16bit;
        }
        public RAM()
        {
            data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = 0;
            }
        }
    }
}
