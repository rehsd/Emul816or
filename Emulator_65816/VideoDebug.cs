using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Emul816or
{
    public partial class VideoDebug : Form
    {
        ERAM eram;
        Bitmap tilesBitmap;
        Bitmap spritesBitmap;

        public VideoDebug(ERAM _eram)
        {
            this.eram = _eram;
            InitializeComponent();
        }

        private void videoOutRefreshTimer_Tick(object sender, EventArgs e)
        {
            byte pixelColorTiles;
            byte pixelColorSprites;
            int redT, blueT, greenT, redS, blueS, greenS;

            for (Int16 y = 0; y < 240; y++)
            {
                for (Int16 x = 0; x < 320; x++)
                {
                    pixelColorTiles = eram.MemoryBytes[0x40000 + 512 * y + x];
                    pixelColorSprites = eram.MemoryBytes[0x60000 + 512 * y + x];

                    redT = (pixelColorTiles & 224);
                    greenT = (pixelColorTiles & 28) << 3;
                    blueT = (pixelColorTiles & 3) << 6;
                    
                    redS = (pixelColorSprites & 224);
                    greenS = (pixelColorSprites & 28) << 3;
                    blueS = (pixelColorSprites & 3) << 6;

                    tilesBitmap.SetPixel(x, y, Color.FromArgb(redT, greenT, blueT));
                    spritesBitmap.SetPixel(x, y, Color.FromArgb(redS, greenS, blueS));
                }
            }
            tilesPictureBox.Image = tilesBitmap;
            spritesPictureBox.Image = spritesBitmap;
        }

        private void VideoDebug_Load(object sender, EventArgs e)
        {
            videoOutRefreshTimer.Enabled = true;
            tilesBitmap = new Bitmap(320, 240);
            spritesBitmap = new Bitmap(320, 240);
        }
    }
}
