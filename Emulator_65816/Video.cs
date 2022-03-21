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
        private mainForm hostForm;
        private byte[] data;
        const uint baseAddress = 0x200000;
        const uint size = 131072;  //resolution in memory: 512x256, output resolution: 320x240
        private bool supports16bit = false;


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
                UpdatePixel(index - baseAddress, value);
            }
        }

        public UInt16 PIXELSIZE = 1;
        const int PIXELSTART_X = 0;    //15
        const int PIXELSTART_Y = 0;    //120

        public Video(mainForm hf)
        {
            hostForm = hf;
            //Reset();
        }

        public void Reset()
        {
            Random rand = new Random();
            data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = (byte)rand.Next(0, 255);
            }
            Refresh();
        }
        public void Refresh()
        {
            //only worry about the visible portion from video memory - 320x240

            int newFrame, oldFrame;
            //if (hostForm.ActiveFrame == 1)
            //{
            //    newFrame = 0;
            //    oldFrame = 1;
            //}
            //else
            //{
            //    newFrame = 1;
            //    oldFrame = 0;
            //}

            //testing single frame for now
            newFrame = 0;
            // hostForm.frameBuffer[newFrame] = new Bitmap(320, 240);
            Graphics g = Graphics.FromImage(hostForm.frameBuffer[newFrame]);

            for (Int16 y = 0; y < 240; y++)
            {
                for (Int16 x = 0; x < 320; x++)
                {
                    byte pixelColor = data[512 * y + x];
                    int red, blue, green;

                    red = (pixelColor & 224);
                    green = (pixelColor & 28) << 3;
                    blue = (pixelColor & 3) << 6;

                    SolidBrush sb = new SolidBrush(Color.FromArgb(red, green, blue));
                    //Graphics g = hostForm.CreateGraphics();
                    g.FillRectangle(sb, PIXELSTART_X + x * PIXELSIZE, PIXELSTART_Y + y * PIXELSIZE, PIXELSIZE, PIXELSIZE);
                }
            }
            hostForm.videoOutPictureBox.Image = hostForm.frameBuffer[newFrame];
            hostForm.videoOutPictureBox.Refresh();
            hostForm.ActiveFrame = newFrame;
        }

        public void UpdatePixel(uint relativePixelAddress, byte newColor)
        {
            int red, blue, green;
            Int16 x, y;


            y = (short)(relativePixelAddress / 512);
            x = (short)(relativePixelAddress % 512);

            if (x < 320 && y < 240)
            {
                Graphics g = Graphics.FromImage(hostForm.frameBuffer[0]);
                
                red = (newColor & 224);
                green = (newColor & 28) << 3;
                blue = (newColor & 3) << 6;
                SolidBrush sb = new SolidBrush(Color.FromArgb(red, green, blue));
                //Graphics g = hostForm.CreateGraphics();
                g.FillRectangle(sb, PIXELSTART_X + x * PIXELSIZE, PIXELSTART_Y + y * PIXELSIZE, PIXELSIZE, PIXELSIZE);



                if (!hostForm.SuspendLogging)
                {
                    //if logging, allow pixel by pixel refresh (slow)
                    hostForm.videoOutPictureBox.Image = hostForm.frameBuffer[0];
                    hostForm.videoOutPictureBox.Refresh();
                    //hostForm.ActiveFrame = newFrame;
                    hostForm.pixelColorLabel.Text = newColor.ToString("X2");
                    hostForm.pixelColorLabel.Refresh();
                }
                else if(x==319 && y==239)
                {
                    //if not logging, want to run as fast as possible
                    //only update the output on the last pixel of the frame
                    hostForm.videoOutPictureBox.Image = hostForm.frameBuffer[0];
                    hostForm.videoOutPictureBox.Refresh();
                }


            }

        }
    }

}
