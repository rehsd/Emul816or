// Reference designs:
// https://github.com/davepoo/6502Emulator
// https://github.com/andrew-jacobs/emu816
// https://github.com/visrealm/vrEmu6502
//
// Primarily adapted from https://github.com/andrew-jacobs/emu816
//
//------------------------------------------------------------------------------
// This work is made available under the terms of the Creative Commons
// Attribution-NonCommercial-ShareAlike 4.0 International license. Open the
// following URL to see the details.
//
// http://creativecommons.org/licenses/by-nc-sa/4.0/
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using Addr = System.UInt32;
using Word = System.UInt16;

namespace Emul816or
{
    public class StatusChangedEventArgs : EventArgs
    {
        public UInt16 A;
        public UInt16 X;
        public UInt16 Y;
        public UInt16 S;
        public UInt16 PC;
        public UInt16 SP;
        public int Cycles;
        public bool FlagC;
        public bool FlagZ;
        public bool FlagI;
        public bool FlagD;
        public bool FlagX;
        public bool FlagM;
        public bool FlagV;
        public bool FlagN;
        public bool FlagE;
        public Byte FlagB;
    }
    public class LogTextUpdateEventArgs : EventArgs
    {
        public string NewText;
    }

    public class CPU
    {
        protected virtual void OnStatusChanged(StatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }
        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        protected virtual void OnLogTextUpdate(LogTextUpdateEventArgs e)
        {
            LogTextUpdate?.Invoke(this, e);
        }
        public event EventHandler<LogTextUpdateEventArgs> LogTextUpdate;

        readonly RAM ramBasic;
        readonly ROM rom;
        readonly ERAM ramExtended;
        readonly VIA via1;
        readonly NullDevice nullDev;
        readonly Video video;
        public bool SuspendLogging;
        int cycles;
        bool interrupted;
        bool stopped;

        public int Cycles
        {
            get => cycles;
        }
        struct A
        {
            static public Byte b;
            static public Word w;
        }

        struct X
        {
            static public Byte b;
            static public Word w;
        }

        struct Y
        {
            static public Byte b;
            static public Word w;
        }
        struct SP
        {
            static public Byte b;
            static public Word w;
        }
        struct DP
        {
            //static public Byte b;
            static public Word w;
        }
        struct P
        {
            static public bool C;   //carry
            static public bool Z;   //zero
            static public bool I;   //IRQ disable
            static public bool D;   //decimal
            static public bool X;   //index register size 0=16, 1=8
            static public bool M;   //accumulator register size (native mode only) 0=16, 1=8
            static public bool V;   //overflow
            static public bool N;   //negative
            static public Byte b;
        }


        //Addr memMask;
        //Addr ramSize;
        //Byte pRAM  ***==> ramBasic;
        Byte pbr;
        Word pc;
        Byte dbr;
        bool E;

        public bool IsStopped()
        {
            return stopped;
        }
        public void Stop(bool changeRed = true)
        {
            stopped = true;
        }

        bool ProcessingInterrupt = false;
        public void Step()
        {
            // Check for NMI/IRQ
            if(interrupted && !P.I && !ProcessingInterrupt)
            {
                ProcessInterrupt();
            }

            Byte cmd = GetByte(Join(pbr, pc++));
            WriteLog("\n" + (pc-1).ToString("X4") + " : " + OpCodeDescArray[cmd]);
            switch (cmd)
            {
                case 0x00: Op_brk(Am_immb()); break;
                case 0x01: Op_ora(Am_dpix()); break;
                case 0x02: Op_cop(Am_immb()); break;
                case 0x03: Op_ora(Am_srel()); break;
                case 0x04: Op_tsb(Am_dpag()); break;
                case 0x05: Op_ora(Am_dpag()); break;
                case 0x06: Op_asl(Am_dpag()); break;
                case 0x07: Op_ora(Am_dpil()); break;
                case 0x08: Op_php(Am_impl()); break;
                case 0x09: Op_ora(Am_immm()); break;
                case 0x0a: Op_asla(Am_acc()); break;
                case 0x0b: Op_phd(Am_impl()); break;
                case 0x0c: Op_tsb(Am_absl()); break;
                case 0x0d: Op_ora(Am_absl()); break;
                case 0x0e: Op_asl(Am_absl()); break;
                case 0x0f: Op_ora(Am_alng()); break;

                case 0x10: Op_bpl(Am_rela()); break;
                case 0x11: Op_ora(Am_dpiy()); break;
                case 0x12: Op_ora(Am_dpgi()); break;
                case 0x13: Op_ora(Am_sriy()); break;
                case 0x14: Op_trb(Am_dpag()); break;
                case 0x15: Op_ora(Am_dpgx()); break;
                case 0x16: Op_asl(Am_dpgx()); break;
                case 0x17: Op_ora(Am_dily()); break;
                case 0x18: Op_clc(Am_impl()); break;
                case 0x19: Op_ora(Am_absy()); break;
                case 0x1a: Op_inca(Am_acc()); break;
                case 0x1b: Op_tcs(Am_impl()); break;
                case 0x1c: Op_trb(Am_absl()); break;
                case 0x1d: Op_ora(Am_absx()); break;
                case 0x1e: Op_asl(Am_absx()); break;
                case 0x1f: Op_ora(Am_alnx()); break;

                case 0x20: Op_jsr(Am_absl()); break;
                case 0x21: Op_and(Am_dpix()); break;
                case 0x22: Op_jsl(Am_alng()); break;
                case 0x23: Op_and(Am_srel()); break;
                case 0x24: Op_bit(Am_dpag()); break;
                case 0x25: Op_and(Am_dpag()); break;
                case 0x26: Op_rol(Am_dpag()); break;
                case 0x27: Op_and(Am_dpil()); break;
                case 0x28: Op_plp(Am_impl()); break;
                case 0x29: Op_and(Am_immm()); break;
                case 0x2a: Op_rola(Am_acc()); break;
                case 0x2b: Op_pld(Am_impl()); break;
                case 0x2c: Op_bit(Am_absl()); break;
                case 0x2d: Op_and(Am_absl()); break;
                case 0x2e: Op_rol(Am_absl()); break;
                case 0x2f: Op_and(Am_alng()); break;

                case 0x30: Op_bmi(Am_rela()); break;
                case 0x31: Op_and(Am_dpiy()); break;
                case 0x32: Op_and(Am_dpgi()); break;
                case 0x33: Op_and(Am_sriy()); break;
                case 0x34: Op_bit(Am_dpgx()); break;
                case 0x35: Op_and(Am_dpgx()); break;
                case 0x36: Op_rol(Am_dpgx()); break;
                case 0x37: Op_and(Am_dily()); break;
                case 0x38: Op_sec(Am_impl()); break;
                case 0x39: Op_and(Am_absy()); break;
                case 0x3a: Op_deca(Am_acc()); break;
                case 0x3b: Op_tsc(Am_impl()); break;
                case 0x3c: Op_bit(Am_absx()); break;
                case 0x3d: Op_and(Am_absx()); break;
                case 0x3e: Op_rol(Am_absx()); break;
                case 0x3f: Op_and(Am_alnx()); break;

                case 0x40: Op_rti(Am_impl()); break;
                case 0x41: Op_eor(Am_dpix()); break;
                case 0x42: Op_wdm(Am_immb()); break;
                case 0x43: Op_eor(Am_srel()); break;
                case 0x44: Op_mvp(Am_immw()); break;
                case 0x45: Op_eor(Am_dpag()); break;
                case 0x46: Op_lsr(Am_dpag()); break;
                case 0x47: Op_eor(Am_dpil()); break;
                case 0x48: Op_pha(Am_impl()); break;
                case 0x49: Op_eor(Am_immm()); break;
                case 0x4a: Op_lsra(Am_impl()); break;
                case 0x4b: Op_phk(Am_impl()); break;
                case 0x4c: Op_jmp(Am_absl()); break;
                case 0x4d: Op_eor(Am_absl()); break;
                case 0x4e: Op_lsr(Am_absl()); break;
                case 0x4f: Op_eor(Am_alng()); break;

                case 0x50: Op_bvc(Am_rela()); break;
                case 0x51: Op_eor(Am_dpiy()); break;
                case 0x52: Op_eor(Am_dpgi()); break;
                case 0x53: Op_eor(Am_sriy()); break;
                case 0x54: Op_mvn(Am_immw()); break;
                case 0x55: Op_eor(Am_dpgx()); break;
                case 0x56: Op_lsr(Am_dpgx()); break;
                case 0x57: Op_eor(Am_dpil()); break;
                case 0x58: Op_cli(Am_impl()); break;
                case 0x59: Op_eor(Am_absy()); break;
                case 0x5a: Op_phy(Am_impl()); break;
                case 0x5b: Op_tcd(Am_impl()); break;
                case 0x5c: Op_jmp(Am_alng()); break;
                case 0x5d: Op_eor(Am_absx()); break;
                case 0x5e: Op_lsr(Am_absx()); break;
                case 0x5f: Op_eor(Am_alnx()); break;

                case 0x60: Op_rts(Am_impl()); break;
                case 0x61: Op_adc(Am_dpix()); break;
                case 0x62: Op_per(Am_lrel()); break;
                case 0x63: Op_adc(Am_srel()); break;
                case 0x64: Op_stz(Am_dpag()); break;
                case 0x65: Op_adc(Am_dpag()); break;
                case 0x66: Op_ror(Am_dpag()); break;
                case 0x67: Op_adc(Am_dpil()); break;
                case 0x68: Op_pla(Am_impl()); break;
                case 0x69: Op_adc(Am_immm()); break;
                case 0x6a: Op_rora(Am_impl()); break;
                case 0x6b: Op_rtl(Am_impl()); break;
                case 0x6c: Op_jmp(Am_absi()); break;
                case 0x6d: Op_adc(Am_absl()); break;
                case 0x6e: Op_ror(Am_absl()); break;
                case 0x6f: Op_adc(Am_alng()); break;

                case 0x70: Op_bvs(Am_rela()); break;
                case 0x71: Op_adc(Am_dpiy()); break;
                case 0x72: Op_adc(Am_dpgi()); break;
                case 0x73: Op_adc(Am_sriy()); break;
                case 0x74: Op_stz(Am_dpgx()); break;
                case 0x75: Op_adc(Am_dpgx()); break;
                case 0x76: Op_ror(Am_dpgx()); break;
                case 0x77: Op_adc(Am_dily()); break;
                case 0x78: Op_sei(Am_impl()); break;
                case 0x79: Op_adc(Am_absy()); break;
                case 0x7a: Op_ply(Am_impl()); break;
                case 0x7b: Op_tdc(Am_impl()); break;
                case 0x7c: Op_jmp(Am_abxi()); break;
                case 0x7d: Op_adc(Am_absx()); break;
                case 0x7e: Op_ror(Am_absx()); break;
                case 0x7f: Op_adc(Am_alnx()); break;

                case 0x80: Op_bra(Am_rela()); break;
                case 0x81: Op_sta(Am_dpix()); break;
                case 0x82: Op_brl(Am_lrel()); break;
                case 0x83: Op_sta(Am_srel()); break;
                case 0x84: Op_sty(Am_dpag()); break;
                case 0x85: Op_sta(Am_dpag()); break;
                case 0x86: Op_stx(Am_dpag()); break;
                case 0x87: Op_sta(Am_dpil()); break;
                case 0x88: Op_dey(Am_impl()); break;
                case 0x89: Op_biti(Am_immm()); break;
                case 0x8a: Op_txa(Am_impl()); break;
                case 0x8b: Op_phb(Am_impl()); break;
                case 0x8c: Op_sty(Am_absl()); break;
                case 0x8d: Op_sta(Am_absl()); break;
                case 0x8e: Op_stx(Am_absl()); break;
                case 0x8f: Op_sta(Am_alng()); break;

                case 0x90: Op_bcc(Am_rela()); break;
                case 0x91: Op_sta(Am_dpiy()); break;
                case 0x92: Op_sta(Am_dpgi()); break;
                case 0x93: Op_sta(Am_sriy()); break;
                case 0x94: Op_sty(Am_dpgx()); break;
                case 0x95: Op_sta(Am_dpgx()); break;
                case 0x96: Op_stx(Am_dpgy()); break;
                case 0x97: Op_sta(Am_dily()); break;
                case 0x98: Op_tya(Am_impl()); break;
                case 0x99: Op_sta(Am_absy()); break;
                case 0x9a: Op_txs(Am_impl()); break;
                case 0x9b: Op_txy(Am_impl()); break;
                case 0x9c: Op_stz(Am_absl()); break;
                case 0x9d: Op_sta(Am_absx()); break;
                case 0x9e: Op_stz(Am_absx()); break;
                case 0x9f: Op_sta(Am_alnx()); break;

                case 0xa0: Op_ldy(Am_immx()); break;
                case 0xa1: Op_lda(Am_dpix()); break;
                case 0xa2: Op_ldx(Am_immx()); break;
                case 0xa3: Op_lda(Am_srel()); break;
                case 0xa4: Op_ldy(Am_dpag()); break;
                case 0xa5: Op_lda(Am_dpag()); break;
                case 0xa6: Op_ldx(Am_dpag()); break;
                case 0xa7: Op_lda(Am_dpil()); break;
                case 0xa8: Op_tay(Am_impl()); break;
                case 0xa9: Op_lda(Am_immm()); break;
                case 0xaa: Op_tax(Am_impl()); break;
                case 0xab: Op_plb(Am_impl()); break;
                case 0xac: Op_ldy(Am_absl()); break;
                case 0xad: Op_lda(Am_absl()); break;
                case 0xae: Op_ldx(Am_absl()); break;
                case 0xaf: Op_lda(Am_alng()); break;

                case 0xb0: Op_bcs(Am_rela()); break;
                case 0xb1: Op_lda(Am_dpiy()); break;
                case 0xb2: Op_lda(Am_dpgi()); break;
                case 0xb3: Op_lda(Am_sriy()); break;
                case 0xb4: Op_ldy(Am_dpgx()); break;
                case 0xb5: Op_lda(Am_dpgx()); break;
                case 0xb6: Op_ldx(Am_dpgy()); break;
                case 0xb7: Op_lda(Am_dily()); break;
                case 0xb8: Op_clv(Am_impl()); break;
                case 0xb9: Op_lda(Am_absy()); break;
                case 0xba: Op_tsx(Am_impl()); break;
                case 0xbb: Op_tyx(Am_impl()); break;
                case 0xbc: Op_ldy(Am_absx()); break;
                case 0xbd: Op_lda(Am_absx()); break;
                case 0xbe: Op_ldx(Am_absy()); break;
                case 0xbf: Op_lda(Am_alnx()); break;

                case 0xc0: Op_cpy(Am_immx()); break;
                case 0xc1: Op_cmp(Am_dpix()); break;
                case 0xc2: Op_rep(Am_immb()); break;
                case 0xc3: Op_cmp(Am_srel()); break;
                case 0xc4: Op_cpy(Am_dpag()); break;
                case 0xc5: Op_cmp(Am_dpag()); break;
                case 0xc6: Op_dec(Am_dpag()); break;
                case 0xc7: Op_cmp(Am_dpil()); break;
                case 0xc8: Op_iny(Am_impl()); break;
                case 0xc9: Op_cmp(Am_immm()); break;
                case 0xca: Op_dex(Am_impl()); break;
                case 0xcb: Op_wai(Am_impl()); break;
                case 0xcc: Op_cpy(Am_absl()); break;
                case 0xcd: Op_cmp(Am_absl()); break;
                case 0xce: Op_dec(Am_absl()); break;
                case 0xcf: Op_cmp(Am_alng()); break;

                case 0xd0: Op_bne(Am_rela()); break;
                case 0xd1: Op_cmp(Am_dpiy()); break;
                case 0xd2: Op_cmp(Am_dpgi()); break;
                case 0xd3: Op_cmp(Am_sriy()); break;
                case 0xd4: Op_pei(Am_dpag()); break;
                case 0xd5: Op_cmp(Am_dpgx()); break;
                case 0xd6: Op_dec(Am_dpgx()); break;
                case 0xd7: Op_cmp(Am_dily()); break;
                case 0xd8: Op_cld(Am_impl()); break;
                case 0xd9: Op_cmp(Am_absy()); break;
                case 0xda: Op_phx(Am_impl()); break;
                case 0xdb: Op_stp(Am_impl()); break;
                case 0xdc: Op_jmp(Am_abil()); break;
                case 0xdd: Op_cmp(Am_absx()); break;
                case 0xde: Op_dec(Am_absx()); break;
                case 0xdf: Op_cmp(Am_alnx()); break;

                case 0xe0: Op_cpx(Am_immx()); break;
                case 0xe1: Op_sbc(Am_dpix()); break;
                case 0xe2: Op_sep(Am_immb()); break;
                case 0xe3: Op_sbc(Am_srel()); break;
                case 0xe4: Op_cpx(Am_dpag()); break;
                case 0xe5: Op_sbc(Am_dpag()); break;
                case 0xe6: Op_inc(Am_dpag()); break;
                case 0xe7: Op_sbc(Am_dpil()); break;
                case 0xe8: Op_inx(Am_impl()); break;
                case 0xe9: Op_sbc(Am_immm()); break;
                case 0xea: Op_nop(Am_impl()); break;
                case 0xeb: Op_xba(Am_impl()); break;
                case 0xec: Op_cpx(Am_absl()); break;
                case 0xed: Op_sbc(Am_absl()); break;
                case 0xee: Op_inc(Am_absl()); break;
                case 0xef: Op_sbc(Am_alng()); break;

                case 0xf0: Op_beq(Am_rela()); break;
                case 0xf1: Op_sbc(Am_dpiy()); break;
                case 0xf2: Op_sbc(Am_dpgi()); break;
                case 0xf3: Op_sbc(Am_sriy()); break;
                case 0xf4: Op_pea(Am_immw()); break;
                case 0xf5: Op_sbc(Am_dpgx()); break;
                case 0xf6: Op_inc(Am_dpgx()); break;
                case 0xf7: Op_sbc(Am_dily()); break;
                case 0xf8: Op_sed(Am_impl()); break;
                case 0xf9: Op_sbc(Am_absy()); break;
                case 0xfa: Op_plx(Am_impl()); break;
                case 0xfb: Op_xce(Am_impl()); break;
                case 0xfc: Op_jsr(Am_abxi()); break;
                case 0xfd: Op_sbc(Am_absx()); break;
                case 0xfe: Op_inc(Am_absx()); break;
                case 0xff: Op_sbc(Am_alnx()); break;
            }

            //UpdateStatusUI();
            StatusChangedEventArgs eventArgs = new();
            eventArgs.A = A.w;
            eventArgs.X = X.w;
            eventArgs.Y = Y.w;
            eventArgs.SP = SP.w;
            eventArgs.FlagB = P.b;
            eventArgs.FlagC = P.C;
            eventArgs.FlagD = P.D;
            eventArgs.FlagE = E;
            eventArgs.FlagI = P.I;
            eventArgs.FlagM = P.M;
            eventArgs.FlagN = P.N;
            eventArgs.FlagV = P.V;
            eventArgs.FlagX = P.X;
            eventArgs.FlagZ = P.Z;
            eventArgs.PC = pc;
            eventArgs.Cycles = cycles;
            OnStatusChanged(eventArgs);
        }

        public enum PinState
        {
            High,
            Low
        }
        public void SetIRQB(PinState newState)
        {
            if(newState == PinState.High)
            {
                //P.I = false;
            }
            else
            {
                //P.I = true;
                interrupted = true;
            }
        }


        void ProcessInterrupt()
        {
            //TO DO Update for emulation mode, value this section of code
            ProcessingInterrupt = true;
            PushByte(pbr);
            PushWord(pc);
            PushByte(P.b);

            //P.I = true;   //already set
            P.D = false;
            pbr = 0;

            pc = GetWord(0xFFEE);
            cycles += 8;    //TO DO Is this right?


            //PushWord(pc);
            //Word interruptVector = GetWord(0xFFEE);
            WriteLog("\nInterrupt vector to " + pc.ToString("X4"));
            //UpdateProgramCounter(interruptVector);

            //BTemporary - Need to determine how to best handle when an interrupt is being processed
            //P.I = false;
        }
        Byte GetByte(Addr ea, bool logIt = false)
        {
            IMemoryIO mem;
            mem = GetDeviceByAddress(ea);
            if(logIt)
            {
                WriteLog("\t" + mem[ea].ToString("X2") + "\t from \t" + ea.ToString("X4"));
            }
            return mem[ea];
        }
        Word GetWord(Addr ea, bool logIt = false)
        {
            Word tmp;
            if (GetDeviceByAddress(ea).Supports16Bit)
            {
                tmp = (ushort)Join(GetByte(ea + 0), GetByte(ea + 1));
            }
            else
            {
                tmp = (ushort)(Join(GetByte(ea + 0),0));
            }
            if (logIt)
            {
                WriteLog("\t" + tmp.ToString("X4") + "\t from \t" + ea.ToString("x4"));
            }
            return tmp;
        }
        Addr GetAddr(Addr ea)
        {
            return (Join(GetByte(ea + 2), GetWord(ea + 0)));
        }
        void SetByte(Addr ea, Byte data, bool logIt = false)
        {
            IMemoryIO mem;
            mem = GetDeviceByAddress(ea);
            if(logIt)
            {
                WriteLog("\t" + mem[ea].ToString("X2") + "\t to \t" + ea.ToString("X4"));
            }
            mem[ea] = data;
        }
        void SetWord(Addr ea, Word data, bool logIt = false)
        {
            if (logIt)
            {
                WriteLog("\t" + data.ToString("X4") + "\t to \t" + ea.ToString("X4"));
            }
            SetByte(ea + 0, Lo(data));
            if (GetDeviceByAddress(ea).Supports16Bit)
            {
                SetByte(ea + 1, Hi(data));
            }
        }

        void PushByte(Byte value)
        {
            SetByte(SP.w, value);

            if (E)
                --SP.b;
            else
                --SP.w;
        }

        void PushWord(Word value)
        {
            PushByte(Hi(value));
            PushByte(Lo(value));
        }

        Byte PullByte()
        {
            if (E)
                ++SP.b;
            else
                ++SP.w;

            return (GetByte(SP.w));
        }

        Word PullWord()
        {
            Byte l = PullByte();
            Byte h = PullByte();

            return ((ushort)Join(l, h));
        }

        static Byte Lo(Word value)
        {
            return (Byte)value;
        }

        static Byte Hi(Word value)
        {
            return Lo((Byte)(value >> 8));
        }

        static Addr Bank(Byte b)
        {
            return (uint)(b << 16);
        }

        static Addr Join(Byte l, Byte h)
        {
            return (Addr)(l | (h << 8));
        }

        static Addr Join(Byte b, Word a)
        {
            return (Bank(b) | a);
        }

        static Word Swap(Word value)
        {
            return (Word)((value >> 8) | (value << 8));
        }

        void BYTES(Word val)
        {
            pc += val;
        }


        // Absolute - a
        Addr Am_absl()
        {
            Addr ea = Join(dbr, GetWord(Bank(pbr) | pc));
            BYTES(2);
            cycles += 2;
            return ea;
        }

        // Absolute Indexed X - a,X
        Addr Am_absx()
        {
            Addr ea = Join(dbr, GetWord(Bank(pbr) | pc)) + X.w;
            BYTES(2);
            cycles += 2;
            return ea;
        }

        // Absolutie Indexed Y - a,Y
        Addr Am_absy()
        {
            Addr ea = Join(dbr, GetWord(Bank(pbr) | pc)) + Y.w;
            BYTES(2);
            cycles += 2;
            return ea;
        }

        // Absolute Indirect - (a)
        Addr Am_absi()
        {
            Addr ia = Join(0, GetWord(Bank(pbr) | pc));
            BYTES(2);
            cycles += 4;
            return Join(0, GetWord(ia));
        }

        // Absolute Indexed Indirect - (a,X)
        Addr Am_abxi()
        {
            Addr ia = Join(pbr, GetWord(Join(pbr, pc))) + X.w;
            BYTES(2);
            cycles += 4;
            return Join(pbr, GetWord(ia));
        }

        // Absolute Long - >a
        Addr Am_alng()
        {
            Addr ea = GetAddr(Join(pbr, pc));
            BYTES(3);
            cycles += 3;
            return ea;
        }

        // Absolute Long Indexed - >a,X
        Addr Am_alnx()
        {
            Addr ea = GetAddr(Join(pbr, pc)) + X.w;
            BYTES(3);
            cycles += 3;
            return ea;
        }

        // Absolute Indirect Long - [a]
        Addr Am_abil()
        {
            Addr ia = Bank(0) | GetWord(Join(pbr, pc));
            BYTES(2);
            cycles += 5;
            return GetAddr(ia);
        }

        // Direct Page - d
        Addr Am_dpag()
        {
            Byte offset = GetByte(Bank(pbr) | pc);
            BYTES(1);
            cycles += 1;
            return (Bank(0) | (Word)(DP.w + offset));
        }

        // Direct Page Indexed X - d,X
        Addr Am_dpgx()
        {
            Byte offset = (Byte)(GetByte(Bank(pbr) | pc) + X.b);
            BYTES(1);
            cycles += 1;
            return (Bank(0) | (Word)(DP.w + offset));
        }

        // Direct Page Indexed Y - d,Y
        Addr Am_dpgy()
        {
            Byte offset = (byte)(GetByte(Bank(pbr) | pc) + Y.b);
            BYTES(1);
            cycles += 1;
            return (Bank(0) | (Word)(DP.w + offset));
        }

        // Direct Page Indirect - (d)
        Addr Am_dpgi()
        {
            Byte disp = GetByte(Bank(pbr) | pc);
            BYTES(1);
            cycles += 3;
            return (Bank(dbr) | GetWord(Bank(0) | (Word)(DP.w + disp)));
        }

        // Direct Page Indexed Indirect - (d,x)
        Addr Am_dpix()
        {
            Byte disp = GetByte(Join(pbr, pc));
            BYTES(1);
            cycles += 3;
            return (Bank(dbr) | GetWord(Bank(0) | (Word)(DP.w + disp + X.w)));
        }

        // Direct Page Indirect Indexed - (d),Y
        Addr Am_dpiy()
        {
            Byte disp = GetByte(Join(pbr, pc));
            BYTES(1);
            cycles += 3;
            uint ea = (uint)(Bank(0) | (Byte)(DP.w + disp));
            return (uint)((Addr)(dbr << 16) | (Word)((Join(GetByte(ea + 0), GetByte(ea + 1))) + Y.w));
        }

        // Direct Page Indirect Long - [d]
        Addr Am_dpil()
        {
            Byte disp = GetByte(Join(pbr, pc));
            BYTES(1);
            cycles += 4;
            return GetAddr(Bank(0) | (Word)(DP.w + disp));
        }

        // Direct Page Indirect Long Indexed - [d],Y
        Addr Am_dily()
        {
            Byte disp = GetByte(Join(pbr, pc));
            BYTES(1);
            cycles += 4;
            return GetAddr(Bank(0) | (Word)(DP.w + disp)) + Y.w;
        }

        // Implied/Stack
        Addr Am_impl()
        {
            BYTES(0);
            return 0;
        }

        // Accumulator
        Addr Am_acc()
        {
            BYTES(0);
            return 0;
        }

        // Immediate Byte
        Addr Am_immb()
        {
            Addr ea = Bank(pbr) | pc;
            BYTES(1);
            cycles += 0;
            return ea;
        }

        // Immediate Word
        Addr Am_immw()
        {
            Addr ea = Bank(pbr) | pc;
            BYTES(2);
            cycles += 1;
            return ea;
        }

        // Immediate based on size of A/M
        Addr Am_immm()
        {
            Addr ea = Join(pbr, pc);
            uint size = (uint)((E || P.M) ? 1 : 2);
            BYTES((Word)size);
            cycles += (int)(size - 1);
            return ea;
        }

        // Immediate based on size of X/Y
        Addr Am_immx()
        {
            Addr ea = Join(pbr, pc);
            uint size = (uint)((E || P.X) ? 1 : 2);
            BYTES((Word)size);
            cycles += (int)(size - 1);
            return ea;
        }

        // Long Relative - d
        Addr Am_lrel()
        {
            Word disp = GetWord(Join(pbr, pc));
            BYTES(2);
            cycles += 2;
            return Bank(pbr) | (Word)(pc + (short)disp);
        }

        // Relative - d
        Addr Am_rela()
        {
            Byte disp = GetByte(Join(pbr, pc));
            BYTES(1);
            cycles += 1;
            return Bank(pbr) | (Word)(pc + (sbyte)disp);
        }

        // Stack Relative - d,S
        Addr Am_srel()
        {
            Byte disp = GetByte(Join(pbr, pc));
            BYTES(1);
            cycles += 1;
            if (E)
            {
                return Bank(0) | Join((byte)(SP.b + disp), Hi(SP.w));
            }
            else
            {
                return Bank(0) | (Word)(SP.w + disp);
            }
        }

        // Stack Relative Indirect Indexed Y - (d,S),Y
        Addr Am_sriy()
        {
            Byte disp = GetByte(Join(pbr, pc));
            Word ia;
            BYTES(1);
            cycles += 3;
            if (E)
            {
                ia = GetWord(Join((byte)(SP.b + disp), Hi(SP.w)));
            }
            else
            {
                ia = GetWord((uint)(Bank(0) | (Word)(SP.w + disp)));
            }
            return Bank(dbr) | (Word)(ia + Y.w);
        }

        // Set the Negative flag
        static void SetN(uint flag)
        {
            P.N = Convert.ToBoolean(flag);
        }

        // Set the Overflow flag
        static void SetV(uint flag)
        {
            P.V = Convert.ToBoolean(flag);
        }

        // Set the decimal flag
        static void SetD(uint flag)
        {
            P.D = Convert.ToBoolean(flag);
        }

        // Set the Interrupt Disable flag
        static void SetI(uint flag)
        {
            P.I = Convert.ToBoolean(flag);
        }

        // Set the Zero flag
        static void SetZ(uint flag)
        {
            P.Z = Convert.ToBoolean(flag);
        }

        // Set the Carry flag
        static void SetC(uint flag)
        {
            P.C = Convert.ToBoolean(flag);
        }

        // Set the Negative and Zero flags from a byte value
        static void SetNZ_B(Byte value)
        {
            SetN((uint)(value & 0x80));
            if (value == 0)
            {
                SetZ(1);
            }
            else
            {
                SetZ(0);
            }
        }

        // Set the Negative and Zero flags from a word value
        static void SetNZ_W(Word value)
        {
            SetN((uint)(value & 0x8000));
            if (value == 0)
            {
                SetZ(1);
            }
            else
            {
                SetZ(0);
            }
        }

        void Op_adc(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea, true);
                Word temp = (ushort)(A.b + data + Convert.ToInt16(P.C));

                if (P.D)
                {
                    if ((temp & 0x0f) > 0x09) temp += 0x06;
                    if ((temp & 0xf0) > 0x90) temp += 0x60;
                }

                SetC((uint)(temp & 0x100));
                SetV((uint)((~(A.b ^ data)) & (A.b ^ temp) & 0x80));
                SetNZ_B(A.b = Lo(temp));
                cycles += 2;
            }
            else
            {
                Word data = GetWord(ea, true);
                int temp = A.w + data + Convert.ToInt16(P.C);

                if (P.D)
                {
                    if ((temp & 0x000f) > 0x0009) temp += 0x0006;
                    if ((temp & 0x00f0) > 0x0090) temp += 0x0060;
                    if ((temp & 0x0f00) > 0x0900) temp += 0x0600;
                    if ((temp & 0xf000) > 0x9000) temp += 0x6000;
                }

                SetC((uint)(temp & 0x10000));
                SetV((uint)((~(A.w ^ data)) & (A.w ^ temp) & 0x8000));
                SetNZ_W(A.w = (Word)temp);
                cycles += 2;
            }
        }

        void Op_and(Addr ea)
        {
            if (E || P.M)
            {
                SetNZ_B(A.b &= GetByte(ea));
                cycles += 2;
            }
            else
            {
                SetNZ_W(A.w &= GetWord(ea));
                cycles += 3;
            }
        }

        void Op_asl(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);

                SetC((uint)(data & 0x80));
                SetNZ_B(data <<= 1);
                SetByte(ea, data);
                cycles += 4;
            }
            else
            {
                Word data = GetWord(ea);

                SetC((uint)(data & 0x8000));
                SetNZ_W(data <<= 1);
                SetWord(ea, data);
                cycles += 5;
            }
        }

        void Op_asla(Addr ea)
        {
            if (E || P.M)
            {
                SetC((uint)(A.b & 0x80));
                SetNZ_B(A.b <<= 1);
                SetByte(ea, A.b);
            }
            else
            {
                SetC((uint)(A.w & 0x8000));
                SetNZ_W(A.w <<= 1);
                SetWord(ea, A.w);
            }
            cycles += 2;
        }

        void Op_bcc(Addr ea)
        {
            if (!P.C)
            {
                if (E && Convert.ToBoolean((pc ^ ea) & 0xff00))
                {
                    ++cycles;
                }
                pc = (Word)ea;
                cycles += 3;
            }
            else
            {
                cycles += 2;
            }
        }

        void Op_bcs(Addr ea)
        {
            if (P.C)
            {
                if (E && Convert.ToBoolean((pc ^ ea) & 0xff00))
                {
                    ++cycles;
                }
                pc = (Word)ea;
                cycles += 3;
            }
            else
                cycles += 2;
        }

        void Op_beq(Addr ea)
        {
            if (P.Z)
            {
                if (E && Convert.ToBoolean((pc ^ ea) & 0xff00)) ++cycles;
                pc = (Word)ea;
                cycles += 3;
            }
            else
                cycles += 2;
        }

        void Op_bit(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);
                if ((A.b & data) == 0)
                {
                    SetZ(1);
                }
                else
                {
                    SetZ(0);
                }
                SetN((uint)(data & 0x80));
                SetV((uint)(data & 0x40));
                cycles += 2;
            }
            else
            {
                Word data = GetWord(ea);
                if ((A.w & data) == 0)
                {
                    SetZ(1);
                }
                else
                {
                    SetZ(0);
                }
                SetN((uint)(data & 0x8000));
                SetV((uint)(data & 0x4000));
                cycles += 3;
            }
        }

        void Op_biti(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);
                if ((A.b & data) == 0)
                {
                    SetZ(1);
                }
                else
                {
                    SetZ(0);
                }
            }
            else
            {
                Word data = GetWord(ea);
                if ((A.w & data) == 0)
                {
                    SetZ(1);
                }
                else
                {
                    SetZ(0);
                }
            }
            cycles += 2;
        }

        void Op_bmi(Addr ea)
        {
            if (P.N)
            {
                if (E && Convert.ToBoolean((pc ^ ea) & 0xff00)) ++cycles;
                pc = (Word)ea;
                cycles += 3;
            }
            else
                cycles += 2;
        }

        void Op_bne(Addr ea)
        {
            if (!P.Z)
            {
                if (E && Convert.ToBoolean((pc ^ ea) & 0xff00)) ++cycles;
                pc = (Word)ea;
                cycles += 3;
            }
            else
                cycles += 2;
        }

        void Op_bpl(Addr ea)
        {
            if (!P.N)
            {
                if (E && Convert.ToBoolean((pc ^ ea) & 0xff00)) ++cycles;
                pc = (Word)ea;
                cycles += 3;
            }
            else
                cycles += 2;
        }

        void Op_bra(Addr ea)
        {
            if (E && Convert.ToBoolean((pc ^ ea) & 0xff00)) ++cycles;
            pc = (Word)ea;
            cycles += 3;
        }

        void Op_brk(Addr ea)
        {
            if (E)
            {
                PushWord(pc);
                PushByte((byte)(P.b | 0x10));

                P.I = true;
                P.D = false;
                pbr = 0;

                pc = GetWord(0xfffe);
                cycles += 7;
            }
            else
            {
                PushByte(pbr);
                PushWord(pc);
                PushByte(P.b);

                P.I = true;
                P.D = false;
                pbr = 0;

                pc = GetWord(0xffe6);
                cycles += 8;
            }
        }

        void Op_brl(Addr ea)
        {
            pc = (Word)ea;
            cycles += 3;
        }

        void Op_bvc(Addr ea)
        {
            if (!P.V)
            {
                if (E && Convert.ToBoolean((pc ^ ea) & 0xff00)) ++cycles;
                pc = (Word)ea;
                cycles += 3;
            }
            else
                cycles += 2;
        }

        void Op_bvs(Addr ea)
        {
            if (P.V)
            {
                if (E && Convert.ToBoolean((pc ^ ea) & 0xff00)) ++cycles;
                pc = (Word)ea;
                cycles += 3;
            }
            else
                cycles += 2;
        }

        void Op_clc(Addr ea)
        {
            SetC(0);
            cycles += 2;
        }

        void Op_cld(Addr ea)
        {
            SetD(0);
            cycles += 2;
        }

        void Op_cli(Addr ea)
        {
            SetI(0);
            cycles += 2;
        }

        void Op_clv(Addr ea)
        {
            SetV(0);
            cycles += 2;
        }
        void Op_cmp(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea, true);
                Word temp = (ushort)(A.b - data);

                SetC((uint)(temp & 0x100));
                SetNZ_B(Lo(temp));
                cycles += 2;
            }
            else
            {
                Word data = GetWord(ea, true);
                Addr temp = (uint)(A.w - data);

                if (A.w >= data)
                {
                    SetC(1);
                }
                else
                {
                    SetC(0);
                }
                //SetC((uint)(temp & 0x10000L));
                SetNZ_W((Word)temp);
                cycles += 3;
            }
        }

        void Op_cop(Addr ea)
        {
            if (E)
            {
                PushWord(pc);
                PushByte(P.b);

                P.I = true;
                P.D = false;
                pbr = 0;

                pc = GetWord(0xfff4);
                cycles += 7;
            }
            else
            {
                PushByte(pbr);
                PushWord(pc);
                PushByte(P.b);

                P.I = true;
                P.D = false;
                pbr = 0;

                pc = GetWord(0xffe4);
                cycles += 8;
            }
        }

        void Op_cpx(Addr ea)
        {
            if (E || P.X)
            {
                Byte data = GetByte(ea);
                Word temp = (ushort)(X.b - data);

                SetC((uint)(temp & 0x100));
                SetNZ_B(Lo(temp));
                cycles += 2;
            }
            else
            {
                Word data = GetWord(ea);
                Addr temp = (uint)(X.w - data);

                if(X.w >= data)
                {
                    SetC(1);
                }
                else
                {
                    SetC(0);
                }
                //SetC((uint)temp & 0x10000);
                SetNZ_W((Word)temp);
                cycles += 3;
            }
        }

        void Op_cpy(Addr ea)
        {
            if (E || P.X)
            {
                Byte data = GetByte(ea);
                Word temp = (ushort)(Y.b - data);

                SetC((uint)(temp & 0x100));
                SetNZ_B(Lo(temp));
                cycles += 2;
            }
            else
            {
                Word data = GetWord(ea);
                Addr temp = (uint)(Y.w - data);

                if (Y.w >= data)
                {
                    SetC(1);
                }
                else
                {
                    SetC(0);
                }
                //SetC(temp & 0x10000);
                SetNZ_W((Word)temp);
                cycles += 3;
            }
        }

        void Op_dec(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);

                SetByte(ea, --data);
                SetNZ_B(data);
                cycles += 4;
            }
            else
            {
                Word data = GetWord(ea);

                SetWord(ea, --data);
                SetNZ_W(data);
                cycles += 5;
            }
        }

        void Op_deca(Addr ea)
        {
            if (E || P.M)
                SetNZ_B(--A.b);
            else
                SetNZ_W(--A.w);

            cycles += 2;
        }

        void Op_dex(Addr ea)
        {
            if (E || P.X)
                SetNZ_B(X.b -= 1);
            else
                SetNZ_W(X.w -= 1);

            cycles += 2;
        }

        void Op_dey(Addr ea)
        {
            if (E || P.X)
                SetNZ_B(Y.b -= 1);
            else
                SetNZ_W(Y.w -= 1);

            cycles += 2;
        }

        void Op_eor(Addr ea)
        {
            if (E || P.M)
            {
                SetNZ_B(A.b ^= GetByte(ea));
                cycles += 2;
            }
            else
            {
                SetNZ_W(A.w ^= GetWord(ea));
                cycles += 3;
            }
        }

        void Op_inc(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);
                SetByte(ea, ++data);
                SetNZ_B(data);
                cycles += 4;
            }
            else
            {
                Word data = GetWord(ea);
                SetWord(ea, ++data);
                SetNZ_W(data);
                cycles += 5;
            }
        }

        void Op_inca(Addr ea)
        {
            if (E || P.M)
                SetNZ_B(++A.b);
            else
                SetNZ_W(++A.w);
            cycles += 2;
        }

        void Op_inx(Addr ea)
        {
            if (E || P.X)
                SetNZ_B(++X.b);
            else
                SetNZ_W(++X.w);
            cycles += 2;
        }

        void Op_iny(Addr ea)
        {
            if (E || P.X)
                SetNZ_B(++Y.b);
            else
                SetNZ_W(++Y.w);
            cycles += 2;
        }

        void Op_jmp(Addr ea)
        {
            pbr = Lo((ushort)(ea >> 16));
            pc = (Word)ea;
            cycles += 1;
        }

        void Op_jsl(Addr ea)
        {
            PushByte(pbr);
            PushWord((ushort)(pc - 1));

            pbr = Lo((ushort)(ea >> 16));
            pc = (Word)ea;
            cycles += 5;
        }

        void Op_jsr(Addr ea)
        {
            PushWord((ushort)(pc - 1));
            pc = (Word)ea;
            cycles += 4;
        }

        void Op_lda(Addr ea)
        {
            if (E || P.M)
            {
                SetNZ_B(A.b = GetByte(ea, true));
                cycles += 2;
            }
            else
            {
                SetNZ_W(A.w = GetWord(ea, true));
                cycles += 3;
            }
        }

        void Op_ldx(Addr ea)
        {
            if (E || P.X)
            {
                SetNZ_B(Lo(X.w = GetByte(ea, true)));
                cycles += 2;
            }
            else
            {
                SetNZ_W(X.w = GetWord(ea, true));
                cycles += 3;
            }
        }

        void Op_ldy(Addr ea)
        {
            if (E || P.X)
            {
                SetNZ_B(Lo(Y.w = GetByte(ea, true)));
                cycles += 2;
            }
            else
            {
                SetNZ_W(Y.w = GetWord(ea, true));
                cycles += 3;
            }
        }

        void Op_lsr(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);
                SetC((uint)(data & 0x01));
                SetNZ_B(data >>= 1);
                SetByte(ea, data);
                cycles += 4;
            }
            else
            {
                Word data = GetWord(ea);
                SetC((uint)(data & 0x0001));
                SetNZ_W(data >>= 1);
                SetWord(ea, data);
                cycles += 5;
            }
        }

        void Op_lsra(Addr ea)
        {
            if (E || P.M)
            {
                SetC((uint)(A.b & 0x01));
                SetNZ_B(A.b >>= 1);
                SetByte(ea, A.b);
            }
            else
            {
                SetC((uint)(A.w & 0x0001));
                SetNZ_W(A.w >>= 1);
                SetWord(ea, A.w);
            }
            cycles += 2;
        }

        void Op_mvn(Addr ea)
        {
            Byte src = GetByte(ea + 1);
            Byte dst = GetByte(ea + 0);
            SetByte(Join(dbr = dst, Y.w++), GetByte(Join(src, X.w++)));
            if (--A.w != 0xffff) pc -= 3;
            cycles += 7;
        }

        void Op_mvp(Addr ea)
        {
            Byte src = GetByte(ea + 1);
            Byte dst = GetByte(ea + 0);
            SetByte(Join(dbr = dst, Y.w--), GetByte(Join(src, X.w--)));
            if (--A.w != 0xffff) pc -= 3;
            cycles += 7;
        }

        void Op_nop(Addr ea)
        {
            cycles += 2;
        }

        void Op_ora(Addr ea)
        {
            if (E || P.M)
            {
                SetNZ_B(A.b |= GetByte(ea));
                cycles += 2;
            }
            else
            {
                SetNZ_W(A.w |= GetWord(ea));
                cycles += 3;
            }
        }

        void Op_pea(Addr ea)
        {
            PushWord(GetWord(ea));
            cycles += 5;
        }

        void Op_pei(Addr ea)
        {
            PushWord(GetWord(ea));
            cycles += 6;
        }

        void Op_per(Addr ea)
        {
            PushWord((Word)ea);
            cycles += 6;
        }

        void Op_pha(Addr ea)
        {
            if (E || P.M)
            {
                PushByte(A.b);
                cycles += 3;
            }
            else
            {
                PushWord(A.w);
                cycles += 4;
            }
        }

        void Op_phb(Addr ea)
        {
            PushByte(dbr);
            cycles += 3;
        }

        void Op_phd(Addr ea)
        {
            PushWord(DP.w);
            cycles += 4;
        }

        void Op_phk(Addr ea)
        {
            PushByte(pbr);
            cycles += 3;
        }

        void Op_php(Addr ea)
        {
            PushByte(P.b);
            cycles += 3;
        }

        void Op_phx(Addr ea)
        {
            if (E || P.X)
            {
                PushByte(X.b);
                cycles += 3;
            }
            else
            {
                PushWord(X.w);
                cycles += 4;
            }
        }

        void Op_phy(Addr ea)
        {
            if (E || P.X)
            {
                PushByte(Y.b);
                cycles += 3;
            }
            else
            {
                PushWord(Y.w);
                cycles += 4;
            }
        }

        void Op_pla(Addr ea)
        {
            if (E || P.M)
            {
                SetNZ_B(A.b = PullByte());
                cycles += 4;
            }
            else
            {
                SetNZ_W(A.w = PullWord());
                cycles += 5;
            }
        }

        void Op_plb(Addr ea)
        {
            SetNZ_B(dbr = PullByte());
            cycles += 4;
        }

        void Op_pld(Addr ea)
        {
            SetNZ_W(DP.w = PullWord());
            cycles += 5;
        }

        void Op_plk(Addr ea)
        {
            SetNZ_B(dbr = PullByte());
            cycles += 4;
        }

        void Op_plp(Addr ea)
        {
            if (E)
                P.b = (byte)(PullByte() | 0x30);
            else
            {
                P.b = PullByte();

                if (P.X)
                {
                    X.w = X.b;
                    Y.w = Y.b;
                }
            }
            cycles += 4;
        }

        void Op_plx(Addr ea)
        {
            if (E || P.X)
            {
                SetNZ_B(Lo(X.w = PullByte()));
                cycles += 4;
            }
            else
            {
                SetNZ_W(X.w = PullWord());
                cycles += 5;
            }
        }

        void Op_ply(Addr ea)
        {
            if (E || P.X)
            {
                SetNZ_B(Lo(Y.w = PullByte()));
                cycles += 4;
            }
            else
            {
                SetNZ_W(Y.w = PullWord());
                cycles += 5;
            }
        }

        void Op_rep(Addr ea)
        {
            P.b = (byte)(P.b & ~GetByte(ea));
            if (E) P.M = P.X = true;
            cycles += 3;
        }

        void Op_rol(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);
                Byte carry = (byte)(P.C ? 0x01 : 0x00);

                SetC((uint)(data & 0x80));
                SetNZ_B(data = (byte)((data << 1) | carry));
                SetByte(ea, data);
                cycles += 4;
            }
            else
            {
                Word data = GetWord(ea);
                Word carry = (ushort)(P.C ? 0x0001 : 0x0000);

                SetC((uint)(data & 0x8000));
                SetNZ_W(data = (ushort)((data << 1) | carry));
                SetWord(ea, data);
                cycles += 5;
            }
        }

        void Op_rola(Addr ea)
        {
            if (E || P.M)
            {
                Byte carry = (byte)(P.C ? 0x01 : 0x00);
                SetC((uint)(A.b & 0x80));
                SetNZ_B(A.b = (byte)((A.b << 1) | carry));
            }
            else
            {
                Word carry = (ushort)(P.C ? 0x0001 : 0x0000);

                SetC((uint)(A.w & 0x8000));
                SetNZ_W(A.w = (ushort)((A.w << 1) | carry));
            }
            cycles += 2;
        }

        void Op_ror(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);
                Byte carry = (byte)(P.C ? 0x80 : 0x00);

                SetC((uint)(data & 0x01));
                SetNZ_B(data = (byte)((data >> 1) | carry));
                SetByte(ea, data);
                cycles += 4;
            }
            else
            {
                Word data = GetWord(ea);
                Word carry = (ushort)(P.C ? 0x8000 : 0x0000);

                SetC((uint)(data & 0x0001));
                SetNZ_W(data = (ushort)((data >> 1) | carry));
                SetWord(ea, data);
                cycles += 5;
            }
        }

        void Op_rora(Addr ea)
        {
            if (E || P.M)
            {
                Byte carry = (byte)(P.C ? 0x80 : 0x00);

                SetC((uint)(A.b & 0x01));
                SetNZ_B(A.b = (byte)((A.b >> 1) | carry));
            }
            else
            {
                Word carry = (ushort)(P.C ? 0x8000 : 0x0000);

                SetC((uint)(A.w & 0x0001));
                SetNZ_W(A.w = (ushort)((A.w >> 1) | carry));
            }
            cycles += 2;
        }

        void Op_rti(Addr ea)
        {
            if (E)
            {
                P.b = PullByte();
                pc = PullWord();
                cycles += 6;
            }
            else
            {
                P.b = PullByte();
                pc = PullWord();
                pbr = PullByte();
                cycles += 7;
            }
            P.I = false;
            interrupted = false;
            ProcessingInterrupt = false;
        }

        void Op_rtl(Addr ea)
        {
            pc = (ushort)(PullWord() + 1);
            pbr = PullByte();
            cycles += 6;
        }

        void Op_rts(Addr ea)
        {
            pc = (ushort)(PullWord() + 1);
            cycles += 6;
        }

        void Op_sbc(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = (byte)~GetByte(ea);
                Word temp = (ushort)(A.b + data + Convert.ToInt16(P.C));

                if (P.D)
                {
                    if ((temp & 0x0f) > 0x09) temp += 0x06;
                    if ((temp & 0xf0) > 0x90) temp += 0x60;
                }

                SetC((uint)(temp & 0x100));
                SetV((uint)((~(A.b ^ data)) & (A.b ^ temp) & 0x80));
                SetNZ_B(A.b = Lo(temp));
                cycles += 2;
            }
            else
            {
                Word data = (ushort)~GetWord(ea);
                int temp = A.w + data + Convert.ToInt16(P.C);

                if (P.D)
                {
                    if ((temp & 0x000f) > 0x0009) temp += 0x0006;
                    if ((temp & 0x00f0) > 0x0090) temp += 0x0060;
                    if ((temp & 0x0f00) > 0x0900) temp += 0x0600;
                    if ((temp & 0xf000) > 0x9000) temp += 0x6000;
                }

                SetC((uint)(temp & 0x10000));
                SetV((uint)((~(A.w ^ data)) & (A.w ^ temp) & 0x8000));
                SetNZ_W(A.w = (Word)temp);
                cycles += 3;
            }
        }

        void Op_sec(Addr ea)
        {
            SetC(1);
            cycles += 2;
        }

        void Op_sed(Addr ea)
        {
            SetD(1);
            cycles += 2;
        }

        void Op_sei(Addr ea)
        {
            //SetI(1);      //To do... implement SEI
            cycles += 2;
        }

        void Op_sep(Addr ea)
        {
            P.b |= GetByte(ea);
            if (E) P.M = P.X = true;

            if (P.X)
            {
                X.w = X.b;
                Y.w = Y.b;
            }
            cycles += 3;
        }

        void Op_sta(Addr ea)
        {
            if (E || P.M)
            {
                SetByte(ea, A.b, true);
                cycles += 2;
            }
            else
            {
                SetWord(ea, A.w, true);
                cycles += 3;
            }
        }

        void Op_stp(Addr ea)
        {
            if (!interrupted)
            {
                pc -= 1;
            }
            else
            {
                interrupted = false;
            }
            cycles += 3;
            Stop();
        }

        void Op_stx(Addr ea)
        {
            if (E || P.X)
            {
                SetByte(ea, X.b, true);
                cycles += 2;
            }
            else
            {
                SetWord(ea, X.w, true);
                cycles += 3;
            }
        }

        void Op_sty(Addr ea)
        {
            if (E || P.X)
            {
                SetByte(ea, Y.b, true);
                cycles += 2;
            }
            else
            {
                SetWord(ea, Y.w, true);
                cycles += 3;
            }
        }

        void Op_stz(Addr ea)
        {
            if (E || P.M)
            {
                SetByte(ea, 0, true);
                cycles += 2;
            }
            else
            {
                SetWord(ea, 0, true);
                cycles += 3;
            }
        }

        void Op_tax(Addr ea)
        {
            if (E || P.X)
                SetNZ_B(Lo(X.w = A.b));
            else
            {
                SetNZ_W(X.w = A.w);
                WriteLog(" :TAX of " + A.w.ToString("X4"));
            }

            cycles += 2;
        }

        void Op_tay(Addr ea)
        {
            if (E || P.X)
                SetNZ_B(Lo(Y.w = A.b));
            else
                SetNZ_W(Y.w = A.w);

            cycles += 2;
        }

        void Op_tcd(Addr ea)
        {
            DP.w = A.w;
            cycles += 2;
        }

        void Op_tdc(Addr ea)
        {
            if (E || P.M)
                SetNZ_B(Lo(A.w = DP.w));
            else
                SetNZ_W(A.w = DP.w);

            cycles += 2;
        }

        void Op_tcs(Addr ea)
        {
            SP.w = (ushort)(E ? (0x0100 | A.b) : A.w);
            cycles += 2;
        }

        void Op_trb(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);

                SetByte(ea, (byte)(data & ~A.b));
                if ((A.b & data) == 0)
                {
                    SetZ(1);
                }
                else
                {
                    SetZ(0);
                }
                cycles += 4;
            }
            else
            {
                Word data = GetWord(ea);
                SetWord(ea, (ushort)(data & ~A.w));
                if ((A.w & data) == 0)
                {
                    SetZ(1);
                }
                else
                {
                    SetZ(0);
                }
                cycles += 5;
            }
        }

        void Op_tsb(Addr ea)
        {
            if (E || P.M)
            {
                Byte data = GetByte(ea);
                SetByte(ea, (byte)(data | A.b));
                if ((A.b & data) == 0)
                {
                    SetZ(1);
                }
                else
                {
                    SetZ(0);
                }
                cycles += 4;
            }
            else
            {
                Word data = GetWord(ea);
                SetWord(ea, (ushort)(data | A.w));
                if ((A.w & data) == 0)
                {
                    SetZ(1);
                }
                else
                {
                    SetZ(0);
                }
                cycles += 5;
            }
        }

        void Op_tsc(Addr ea)
        {
            if (E || P.M)
                SetNZ_B(Lo(A.w = SP.w));
            else
                SetNZ_W(A.w = SP.w);

            cycles += 2;
        }

        void Op_tsx(Addr ea)
        {
            if (E)
                SetNZ_B(X.b = SP.b);
            else
                SetNZ_W(X.w = SP.w);

            cycles += 2;
        }

        void Op_txa(Addr ea)
        {
            if (E || P.M)
                SetNZ_B(A.b = X.b);
            else
            {
                SetNZ_W(A.w = X.w);
                WriteLog(" :TXA of " + A.w.ToString("X4"));                
            }

            cycles += 2;
        }

        void Op_txs(Addr ea)
        {
            if (E)
                SP.w = (ushort)(0x0100 | X.b);
            else
                SP.w = X.w;

            cycles += 2;
        }

        void Op_txy(Addr ea)
        {
            if (E || P.X)
                SetNZ_B(Lo(Y.w = X.w));
            else
                SetNZ_W(Y.w = X.w);

            cycles += 2;
        }

        void Op_tya(Addr ea)
        {
            if (E || P.M)
                SetNZ_B(A.b = Y.b);
            else
                SetNZ_W(A.w = Y.w);

            cycles += 2;
        }

        void Op_tyx(Addr ea)
        {
            if (E || P.X)
                SetNZ_B(Lo(X.w = Y.w));
            else
                SetNZ_W(X.w = Y.w);

            cycles += 2;
        }

        void Op_wai(Addr ea)
        {
            if (!interrupted)
            {
                pc -= 1;
            }
            else
                interrupted = false;

            cycles += 3;
        }

        static void Op_wdm(Addr ea)
        {
            throw new Exception("Op_wdm not implemented!");
        }

        void Op_xba(Addr ea)
        {
            A.w = Swap(A.w);
            SetNZ_B(A.b);
            cycles += 3;
        }

        void Op_xce(Addr ea)
        {
            (P.C, E) = (E, P.C);        //tuple
            if (E)
            {
                P.b |= 0x30;
                SP.w = (ushort)(0x0100 | SP.b);
            }
            cycles += 2;
        }

        public CPU(ROM _rom, RAM _ram, ERAM _eram, VIA _via1, Video _video, NullDevice _nulldev)
        {
            rom = _rom;
            ramBasic = _ram;
            ramExtended = _eram;
            via1 = _via1;
            video = _video;
            nullDev = _nulldev;
        }
     
        void WriteLog(string textToWrite)
        {

            if(SuspendLogging) { return; }
            LogTextUpdateEventArgs eventArgs = new();
            eventArgs.NewText = textToWrite;
            OnLogTextUpdate(eventArgs);
        }

        public void Reset()
        {
            cycles = 0;
            E = true;
            pbr = 0x00;
            dbr = 0x00;
            DP.w = 0x0000;

            UpdateStackPointer(0x0100);

            Word startPC = GetWord(0xFFFC);
            WriteLog("Reset vector to " + startPC.ToString("X4") + "\n");
            UpdateProgramCounter(startPC);

            P.b = 0x34;

            stopped = false;
            interrupted = false;

            WriteLog("Running!\n");
        }
        IMemoryIO GetDeviceByAddress(uint address)
        {
            if (address <= 0x007FFF)                              //Basic RAM
            {
                return ramBasic;
            }
            else if (address <= 0x07FFFF)                       //ROM
            {
                return rom;
            }
            else if (address <= 0x0FFFFF)                       //Extended RAM
            {
                return ramExtended;
            }
            else if (address >= 0x108000 && address <= 0x10800F) //VIA1
            {
                return via1;
            }
            else if (address >= 0x200000 && address <= 0x21FFFF) //Video
            {
                return video;
            }
            else
            {
                return nullDev;
            }
        }
        void UpdateStackPointer(ushort newVal)
        {
            SP.w = newVal;
            if (SuspendLogging) { return; }
        }
        void UpdateProgramCounter(Word newVal)
        {
            pc = newVal;
            if (SuspendLogging) { return; }
        }

        readonly string[] OpCodeDescArray = new string[256]
        {
            "00 BRK INT",
            "01 ORA DPIX",
            "02 COP INT",
            "03 ORA SR",
            "04 TSB DP",
            "05 ORA DP",
            "06 ASL DP",
            "07 ORA DPIL",
            "08 PHP",
            "09 ORA I",
            "0A ASL ACCUM",
            "0B PHD",
            "0C TSB ABS",
            "0D ORA ABS",
            "0E ASL ABS",
            "0F ORA ABSL",
            "10 BPL",
            "11 ORA DPIIY",
            "12 ORA DPI",
            "13 ORA SRIIY",
            "14 TRB DP",
            "15 ORA DPIX",
            "16 ASL DPIX",
            "17 ORA DPILIY",
            "18 CLC",
            "19 ORA ABSIY",
            "1A INC ACCUM",
            "1B TCS",
            "1C TRB ABS",
            "1D ORA ABSIX",
            "1E ASL ABSIX",
            "1F ORA AIY",
            "20 JSR ABS",
            "21 AND DPIX",
            "22 JSLL ABSL",
            "23 AND SR",
            "24 BIT DP",
            "25 AND DP",
            "26 ROL DP",
            "27 AND DPIL",
            "28 PLP",
            "29 AND I",
            "2A ROL ACCUM",
            "2B PLD",
            "2C BIT ABS",
            "2D AND ABS",
            "2E ROL ABS",
            "2F AND ABSL",
            "30 BMI",
            "31 AND DPIIY",
            "32 AND DPI",
            "33 AND SRIIY",
            "34 BIT DPIX",
            "35 AND DPIX",
            "36 ROL DPIX",
            "37 AND DPILIY",
            "38 SEC",
            "39 AND ABSIY",
            "3A DEC ACCUM",
            "3B TSC",
            "3C BIT ABSIX",
            "3D AND ABSIX",
            "3E ROL ABSIX",
            "3F AND ABSLIX",
            "40 RTI",
            "41 EOR DPIX",
            "42 WDM",
            "43 EOR SR",
            "44 MVP BLKMV",
            "45 EOR DP",
            "46 LSR DP",
            "47 EOR DPIL",
            "48 PHA",
            "49 EOR I",
            "4A LSR ACCUM",
            "4B PHK",
            "4C JMP ABS",
            "4D EOR ABS",
            "4E LSR ABS",
            "4F EOR ABSL",
            "50 BVC",
            "51 EOR DPIIY",
            "52 EOR DPI",
            "53 EOR SRIIY",
            "54 MVN BLKMV",
            "55 EOR DPIX",
            "56 LSR DPIX",
            "57 EOR DPILIY",
            "58 CLI",
            "59 EOR ABSIY",
            "5A PHY",
            "5B TCD",
            "5C JMP ABSL",
            "5D EOR ABSIX",
            "5E LSR ABSIX",
            "5F EOR ABSLIX",
            "60 RTS SR",
            "61 ADC DPIIX",
            "62 PER SR",
            "63 ADC SR",
            "64 STZ DP",
            "65 ADC DP",
            "66 ROR DP",
            "67 ADC DPIL",
            "68 PLA SR",
            "69 ADC I",
            "6A ROR ACCUM",
            "6B RTL SR",
            "6C JMP ABSI",
            "6D ADC ABS",
            "6E ROR ABS",
            "6F ADC ABSL",
            "70 BVS PCR",
            "71 ADC DPIIY",
            "72 ADC DPI",
            "73 ADC SRIIY",
            "74 STZ DPIX",
            "75 ADC DPIX",
            "76 ADC DPIX",
            "77 ADC DPILIY",
            "78 SEI",
            "79 ADC ABSIY",
            "7A PLY",
            "7B TDC",
            "7C JMP AII",
            "7D ADC AIX",
            "7E ROR AIX",
            "7F ADC ALIX",
            "80 BRA",
            "81 STA DPIIX",
            "82 BRL PCRL",
            "83 STA SR",
            "84 STY DP",
            "85 STA DP",
            "86 STX DP",
            "87 STA DPIL",
            "88 DEY",
            "89 BIT I",
            "8A TXA",
            "8B PHB",
            "8C STY ABS",
            "8D STA ABS",
            "8E STX ABS",
            "8F STA ABSL",
            "90 BCC PCR",
            "91 STA DPIIY",
            "92 STA DPI",
            "93 STA SRIIY",
            "94 SY DPIX",
            "95 STA DPIX",
            "96 STX DPIY",
            "97 STA DPILIY",
            "98 TYA I",
            "99 STA ABSIY",
            "9A TXS",
            "9B TXY",
            "9C STZ ABS",
            "9D STA ABSIX",
            "9E STZ ABSIX",
            "9F STA ABSLIX",
            "A0 LDY I",
            "A1 LDA DPIIX",
            "A2 LDX I",
            "A3 LDA SR",
            "A4 LDY DP",
            "A5 LDA DP",
            "A6 LDX DP",
            "A7 LDA DPIL",
            "A8 TAY",
            "A9 LDA I",
            "AA TAX I",
            "AB PLB",
            "AC LDY ABS",
            "AD LDA ABS",
            "AE LDX ABS",
            "AF LDA ABSL",
            "B0 BCS PCR",
            "B1 LDA DPIIY",
            "B2 LDA DPI",
            "B3 LDA SRIIY",
            "B4 LDY DPIX",
            "B5 LDA DPIX",
            "B6 LDX DPIY",
            "B7 LDA DPILIY",
            "B8 CLV",
            "B9 LDA ABSIY",
            "BA TSX",
            "BB TYX",
            "BC LDY ABSIX",
            "BD LDA ABSIX",
            "BE LDX ABSIY",
            "BF LDA ABSLIX",
            "C0 CPY I",
            "C1 CMP DPIIX",
            "C2 REP I",
            "C3 CMP SR",
            "C4 CPY DP",
            "C5 CMP DP",
            "C6 DEC DP",
            "C7 CMP DPIL",
            "C8 INY",
            "C9 CMP I",
            "CA DEX",
            "CB WAI",
            "CC CPY ABS",
            "CD CMP ABS",
            "CE DEC ABS",
            "CF CMP ABSL",
            "D0 BNE PCR",
            "D1 CMP DPIIY",
            "D2 CMP DPI",
            "D3 CMP SRIIY",
            "D4 PEI DPI",
            "D5 CMP DPIX",
            "D6 DEC DPIX",
            "D7 CMP DPILIY",
            "D8 CLD",
            "D9 CMP ABSIY",
            "DA PHX",
            "DB STP",
            "DC JMP ABSIL",
            "DD CMP ABSIX",
            "DE DEC ABSIX",
            "DF CMP ABSLIX",
            "E0 CPX I",
            "E1 SBC DPIIX",
            "E2 SEP I",
            "E3 SBC SR",
            "E4 CPX DP",
            "E5 SBC DP",
            "E6 INC DP",
            "E7 SBC DPIL",
            "E8 INX",
            "E9 SBC I",
            "EA NOP",
            "EB XBA",
            "EC CPX ABS",
            "ED SBC ABS",
            "EE INC ABS",
            "EF SBC ABSL",
            "F0 BEQ PCR",
            "F1 SBC DPIIY",
            "F2 SBC DPI",
            "F3 SBC SRIIY",
            "F4 PEA",
            "F5 SBC DPIX",
            "F6 INC DPIX",
            "F7 SBC DPILIY",
            "F8 SED",
            "F9 SBC ABSIY",
            "FA PLX",
            "FB XCE",
            "FC JSR ABSII",
            "FD SBC ABSIX",
            "FE SBC ABSIX",
            "FF SBC ABSLIX"
        };
    }


}
