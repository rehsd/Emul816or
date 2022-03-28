//https://eater.net/datasheets/HD44780.pdf
//Pseudo emulator for 1602 LCD.
//Only emulating 4-bit mode. DB0-3 are not used.
//Example connection to Port B on VIA:
//  PB0 - DB4
//  PB1 - DB5
//  PB2 - DB6
//  PB3 - DB7
//  PB4 - RS
//  PB5 - RW
//  PB6 - E
//  PB7 - unused
//
//On VIA:
//  E  = %01000000
//  RW = %00100000
//  RS = %00010000


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Emul816or
{
    internal class LCD1602
    {
        public enum ModeTypes
        {
            Mode4Bit,
            Mode8Bit
        }
        private byte data;
        public ModeTypes Mode = ModeTypes.Mode8Bit; //Starts in 8-bit mode
        private UInt16 modeSetStep = 1;
        private byte highNibble;
        private byte lowNibble;
        private UInt16 highNibbleCount;
        private UInt16 lowNibbleCount;
        const byte E = 0b01000000;
        const byte RW = 0b00100000;
        const byte RS = 0b00010000;
        RichTextBox rtbOut;
        GroupBox HostingGroupBox;

        public void Reset(bool clear = false)
        {
            if(clear)
            {
                rtbOut.Clear();
            }
            modeSetStep = 1;
            highNibble = 0;
            lowNibble = 0;
            highNibbleCount = 0;
            lowNibbleCount = 0;
        }
        public LCD1602(GroupBox hostingGroupBox)
        {
            HostingGroupBox = hostingGroupBox;
            rtbOut = new RichTextBox();
            rtbOut.Font = new Font("Courier New", 12);
            rtbOut.Width = (int)(rtbOut.Font.Size * 1.2 * 16);  //Attempting to deal with different screen resolutions and scaling
            rtbOut.Height = 100;
            rtbOut.BackColor = Color.Blue;
            rtbOut.ForeColor = Color.White;
            HostingGroupBox.Controls.Add(rtbOut);
        }
        public byte GetValue()
        {
            return data;       //TO DO: Return actual value, masked for which bits should be returned
        }
        public void SetValue(byte newVal)
        {
            data = newVal;
            if(Mode == ModeTypes.Mode8Bit)
            {
                switch (newVal)
                {
                    case 0b00000010:            //Set 4-bit mode, steps 1 & 3
                        if (modeSetStep == 1)
                        {
                            modeSetStep = 2;
                        }
                        else if (modeSetStep == 3)
                        {
                            Mode = ModeTypes.Mode4Bit;
                        }
                        return;
                    case 0b01000010:            //Set 4-bit mode, step 2
                        modeSetStep = 3;
                        return;
                    default:
                        //unable to process
                        return;
                }
            }
            else
            {
                Process4bitCommandPart(newVal);
            }

        }
        private void Process4bitCommandPart(byte cmd)
        {   //example cmd: 00101000  (0010:1000)
            //00101000, shift right 4 bits         receive --> 00000010
            //ora E                                receive --> 01000010
            //clear E                              receive --> 00000010
            //send low bits                        receive --> 00001000
            //ora E                                receive --> 01001000
            //clear E                              receive --> 00001000

            //example char: 01000001 (A)        0000 0100 (h)
                                            //  0000 0001 (l)

            //Expect three high nibbles, followed by three low nibbles

            byte cmdWithoutFlagsLow = (byte)(cmd & 0b00001111);
            byte cmdWithoutFlagsHigh = cmdWithoutFlagsLow;

            
            if((highNibbleCount == 1) && (highNibble == cmdWithoutFlagsHigh))
            {
                highNibbleCount++;
            }
            else if((highNibbleCount == 2) && (highNibble == cmdWithoutFlagsHigh))
            {
                highNibbleCount++;
            }
            else if(highNibbleCount == 3)   //expecting three low nibbles
            {
                if((lowNibbleCount==1) && (lowNibble == cmdWithoutFlagsLow))
                {
                    lowNibbleCount++;
                }
                else if((lowNibbleCount == 2) && (lowNibble == cmdWithoutFlagsLow))
                {
                    Process4bitCommandComplete((byte)((highNibble << 4) | lowNibble), (Convert.ToBoolean(cmd & RS)));
                }
                else
                {
                    lowNibble = cmdWithoutFlagsLow;
                    lowNibbleCount = 1;
                }
            }
            else
            {
                highNibble = cmdWithoutFlagsHigh;
                highNibbleCount = 1;
                lowNibbleCount = 0;
            }
        }

        private void Process4bitCommandComplete(byte cmd, bool RSset)
        {

            //if RS is set, cmd is char to print
            if(!RSset)
            {
                switch (cmd)
                {
                    case 0b00101000:        //Set 4-bit mode; 2-line display; 5x8 font     ;See page 24 of HD44780.pdf
                        break;
                    case 0b00001110:        //Display on; cursor on; blink off
                        break;
                    case 0b00000110:        //Increment and shift cursor; don't shift display
                        break;
                    case 0b00000001:        //Clear display
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if(rtbOut.TextLength == 32)
                {
                    rtbOut.Clear();     //Actual LCD would just rollover
                }
                rtbOut.AppendText(((char)cmd).ToString());
            }

            lowNibble = 0;
            highNibble = 0;
            highNibbleCount = 0;
            lowNibbleCount = 0;

        }
    }
}
