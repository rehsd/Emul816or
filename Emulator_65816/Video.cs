using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emul816or
{
    public class Video : IMemoryIO
    {
        private byte[] data;
        const uint baseAddress = 0x200000;
        const uint size = 131072;  //resolution in memory: 512x256, output resolution: 320x240
        private readonly bool supports16bit = false;

        public uint Size
        {
            get => size;
        }
        public uint BaseAddress
        {
            get => baseAddress;
        }
        public bool Supports16Bit
        {
            get => supports16bit;
        }

        public byte[] MemoryBytes => data;

        public Byte this[uint index]
        {
            get => data[index - baseAddress];
            set
            {
                data[index - baseAddress] = value;
            }
        }

        public Video()
        {
            Reset();
        }

        public void Reset()
        {
            Random rand = new();
            data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = (byte)rand.Next(0, 255);
            }
        }
    }

}
