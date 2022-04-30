//represents a PSG: AY-3-8910 or YM2149
//register data: https://f.rdw.se/AY-3-8910-datasheet.pdf
//BC1 and BDIR coming from one VIA port
//Data on the other VIA port
//Starting with basic tones, only writing to PSGs. Reading from PSG I/O not supported.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualBasic.Devices;

namespace Emul816or
{
    public class PSG
    {
        const uint size = 16;
        const uint baseAddress = 0x100000;
        private bool supports16bit = true;
        //private bool bdir;
        //private bool bc1;
        //assuming bc2 is tied high
        private byte currentRegister;   //0x00-0x0F
        //private byte incomingData;      //hold incoming data prior to receiving appropriate bus control signals
        //private byte outgoingData;      //hold outgoing data prior to receiving appropriate bus control signals
        private byte tempData;

        byte[] RegisterValues;
        Thread thread_PSG1_ChA;

        public enum REGISTERS
        {
            Reg0_ChA_TonePeriod_Fine,
            Reg1_ChA_TonePeriod_Course,
            Reg2_ChB_TonePeriod_Fine,
            Reg3_ChB_TonePeriod_Course,
            Reg4_ChC_TonePeriod_Fine,
            Reg5_ChC_TonePeriod_Course,
            Reg6_NoisePeriod,
            Reg7_EnableB,
            Reg8_ChA_Amplitude,
            Reg9_ChB_Amplitude,
            RegA_ChC_Amplitude,
            RegB_EnvelopePeriod_Fine,
            RegC_EnvelopePeriod_Course,
            RegD_EnvelopeShapeCycle,
            RegE_IO_PortA_Data,
            RegF_IO_PortB_Data
        }
        public uint Size
        {
            get => size;
        }
        public uint BaseAddress
        {
            get => baseAddress;
        }
        public void SetBusControl(bool BDIR, bool BC1)
        {
            //this.bdir = BDIR;
            //this.bc1 = BC1;

            //setreg        = NACT -> writeValue -> INTAK -> NACT
            //readdata      = NACT -> changeDDRtoRead(00) -> writeZero -> DTB -> changeDDRtoWrite(FF) -> NACT
            //writedata     = NACT -> writeValue -> DWS -> NACT
            if (!BDIR && !BC1)       //inactive  (NACT)
            {
                tempData = 0;
            }
            else if (!BDIR && BC1)   //read      (DTB)
            {
                //readdata
                //not supported yet
                tempData=  RegisterValues[currentRegister];
            }
            else if (BDIR  && !BC1)   //write     (DWS)
            {
                //writedata
                RegisterValues[currentRegister] = tempData;
                Play();
            }
            else if (BDIR && BC1)    //latch     (INTAK)
            {
                //setreg
                if (tempData >= 0 && tempData <= 15)
                {
                    currentRegister = tempData;
                }
            }
        }
        public Byte Value
        {
            get
            {
                return tempData;
            }
            set
            {
                tempData = value;
            }
        }
        public bool Supports16Bit
        {
            get => supports16bit;
        }

        public PSG()
        {
            RegisterValues = new byte[16];
            RegisterValues[(byte)PSG.REGISTERS.Reg0_ChA_TonePeriod_Fine] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg1_ChA_TonePeriod_Course] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg2_ChB_TonePeriod_Fine] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg3_ChB_TonePeriod_Course] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg4_ChC_TonePeriod_Fine] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg5_ChC_TonePeriod_Course] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg6_NoisePeriod] = 0;               //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.Reg7_EnableB] = 0b11111111;          //turn off noise and tone
            RegisterValues[(byte)PSG.REGISTERS.Reg8_ChA_Amplitude] = 15;            //max volume
            RegisterValues[(byte)PSG.REGISTERS.Reg9_ChB_Amplitude] = 15;            //max volume
            RegisterValues[(byte)PSG.REGISTERS.RegA_ChC_Amplitude] = 15;            //max volume
            RegisterValues[(byte)PSG.REGISTERS.RegB_EnvelopePeriod_Fine] = 0;       //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.RegC_EnvelopePeriod_Course] = 0;     //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.RegD_EnvelopeShapeCycle] = 0;        //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.RegE_IO_PortA_Data] = 0;             //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.RegF_IO_PortB_Data] = 0;             //not implementing yet
        }

        private void Play()
        {
            //read all registers and update sound output of PSG
            bool chAenable = !(Convert.ToBoolean(RegisterValues[(int)REGISTERS.Reg7_EnableB] & (byte)0x00000001));
            if (chAenable)
            {
                int frequency = RegisterValues[(int)REGISTERS.Reg1_ChA_TonePeriod_Course];
                //TO DO - Frequency calc based on Course and Fine
                PSG p = new PSG();
                thread_PSG1_ChA = new Thread(PSG.PlayTone);
                thread_PSG1_ChA.Start(1000);    //frequency
            }
            else
            {
                //turn off sound
                if(thread_PSG1_ChA != null)
                {
                    thread_PSG1_ChA.Interrupt();
                }
            }
        }

        public static void PlayTone(object data)
        {
            UInt16 i = (UInt16)((int)data & 0b0111111111111111);
            //testing with Beep for now
            //need to find a better way to play a tone, as multiple, simultaneous tones will be needed
            //Console.Beep(i,int.MaxValue-1);
            Console.Beep(i,1000);
            
            //Audio x = new Audio();
            //x.Play()
        }
    }
}
