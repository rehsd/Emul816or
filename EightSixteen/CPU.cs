using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EightSixteen
{
    public class CPU
    {
        ushort   ProgramCounter;
        ushort   StackPointer;
        byte     A, X, Y;
        
        public CPU()
        {
        }

        
        struct FlagsStruct
        {
            public bool Carry;
            public bool Zero;
            public bool InterruptDisable;
            public bool DecimalMode;
            public bool BreakCommand;
            public bool Overflow;
            public bool Negative;
            public void Reset()
            {
                Carry = false;
                Zero = false;
                InterruptDisable = false;
                DecimalMode = false;
                BreakCommand = false;
                Overflow = false;
                Negative = false;
            }
        }
        class RAM
        {
            const uint SIZE = 32768 * 8;
            public Byte[] Data;
            public RAM()
            {
                Data = new byte[SIZE];
            }
        }
        class ERAM
        {

        }


        FlagsStruct Flags;

        RichTextBox logRTB;
        void writeLog(string textToWrite)
        {
            if (logRTB != null)
            {
                logRTB.Text += textToWrite;
            }
        }

        void Reset()
        {
            writeLog("Reset...\n\r");
            ProgramCounter = 0xFFFC;
            StackPointer = 0x0100;
            Flags.Reset();
            A = X = Y = 0;
            RAM basicRAM = new RAM();
            
            
        }
    }
}
