using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emul816or
{
    public class ROM : IMemoryIO
    {
        private byte[] data;
        const uint size = 524288;
        const uint baseAddress = 0x000000;  //Accessible ROM starts at 0x008000. The first 32k addresses, starting at 0x000000 are used by basic RAM. Handled by address decode logic.
        private bool supports16bit = true;
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

        public byte this[uint index]
        {
            get => data[index - baseAddress];
            set => data[index - baseAddress] = value;
        }

        public ROM()
        {
            //no file specified, set to all zeros
            data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = 0;
            }
        }
        public ROM(string romFile)
        {
            data = new byte[size];
            FileStream fs = File.OpenRead(romFile);
            for (int i = 0; i < size; i++)
            {
                data[i] = (byte)fs.ReadByte();
            }
        }
    }
}
