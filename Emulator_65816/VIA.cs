using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emul816or
{
    public class VIAOutChangedEventArgs : EventArgs
    {
        public byte PortA;
        public byte PortB;
    }

    public class VIA : IMemoryIO
    {
        protected virtual void OnVIAOutChanged(VIAOutChangedEventArgs e)
        {
            EventHandler<VIAOutChangedEventArgs> handler = VIAOutChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<VIAOutChangedEventArgs> VIAOutChanged;

        private byte[] data;   //to store registers
        const uint size = 16;  //16 registers
        private uint baseAddress;   //set in constructor
        private bool supports16bit = false;
        public bool SuspendLogging = false;

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
            //set => data[index-baseAddress] = value;
            set
            {
                data[index - baseAddress] = value;
                if (index - baseAddress == 0x00 || index - baseAddress == 0x01)
                {
                    Update();
                }
            }
        }

        public void Update()
        {
            if (SuspendLogging) { return; }
            VIAOutChangedEventArgs eventArgs = new();
            eventArgs.PortB = data[0x00];
            eventArgs.PortA = data[0x01];
            OnVIAOutChanged(eventArgs);
        }
        //private void setPortBitDisplay(PictureBox pb, bool value)
        //{
        //    if (value)
        //    {
        //        pb.BackColor = Color.Red;
        //    }
        //    else
        //    {
        //        pb.BackColor = Color.Black;
        //    }
        //}

        public VIA(uint address)
        {
            baseAddress = address;
            data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = 0;
            }
        }
    }
}
