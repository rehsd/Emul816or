using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emul816or
{
    public class VIAOutChangedEventArgs : EventArgs
    {
        //public byte PortA;
        //public byte PortB;
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

        public enum REGISTERS : byte
        {
            VIA_PORTB   = 0x00,
            VIA_PORTA   = 0x01,
            VIA_DDRB    = 0x02,
            VIA_DDRA    = 0x03,
            VIA_T1C_L   = 0x04,
            VIA_T1C_H   = 0x05,
            VIA_T1L_L   = 0x06,
            VIA_T1L_H   = 0x07,
            VIA_T2C_L   = 0x08,
            VIA_T2C_H   = 0x09,
            VIA_SR      = 0x0A,
            VIA_ACR     = 0x0B,
            VIA_PCR     = 0x0C,
            VIA_IFR     = 0x0D,
            VIA_IER     = 0x0E
        }
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
            get
            {
                return data[index - baseAddress];
            }
            set
            {
                if(data[index - baseAddress] != value)
                {
                    //only update if the value has changed
                    data[index - baseAddress] = value;
                    Update();
                }
            }
        }

        public void Update()
        {
            VIAOutChangedEventArgs eventArgs = new();
            //eventArgs.PortB = data[0x00];
            //eventArgs.PortA = data[0x01];
            OnVIAOutChanged(eventArgs);
        }

        public VIA(uint address)
        {
            baseAddress = address;
            data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = 0;
            }
            data[0x02] = 0xFF;  //Set PortB as Output by default 
            data[0x03] = 0xFF;  //Set PortA as Output by default 
        }

        public void ResetInterrupt()
        {
            data[(int)REGISTERS.VIA_IFR] = (byte)(data[(int)REGISTERS.VIA_IFR] & 0b01111111);
            //data[(int)REGISTERS.VIA_IER] = (byte)(data[(int)REGISTERS.VIA_IER] & 0b11011111);
        }
    }
}
