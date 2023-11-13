using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Emul816or
{
    public partial class MemoryViewer : Form
    {
        ROM rom;
        RAM ram;
        ERAM eram;
        Video video;

        public MemoryViewer(ROM _rom, RAM _ram, ERAM _eram, Video _video)
        {
            rom = _rom;
            ram = _ram;
            eram = _eram;
            video = _video;
            InitializeComponent();
        }

        private void memoryDeviceCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (memoryDeviceCombo.SelectedIndex)
            {
                case 1:             //ROM
                    noteLabel.Text = "*First 32K addresses used by RAM\n Start at 008000";
                    FillRTB(rom);
                    JumpToAddress((rom.BaseAddress + 0x8000).ToString("X6"));
                    break;
                case 2:             //RAM
                    noteLabel.Text = "";
                    FillRTB(ram);
                    JumpToAddress(ram.BaseAddress.ToString("X6"));
                    break;
                case 3:             //ERAM
                    noteLabel.Text = "";
                    FillRTB(eram);
                    JumpToAddress(eram.BaseAddress.ToString("X6"));
                    break;
                case 4:             //VIDEO
                    noteLabel.Text = "";
                    FillRTB(video);
                    JumpToAddress(video.BaseAddress.ToString("X6"));
                    break;
                default:
                    noteLabel.Text = "";
                    rtb.Clear();
                    break;
            }

            void FillRTB(IMemoryIO device)
            {
                uint addr = device.BaseAddress;
                StringBuilder sb = new StringBuilder();
                string tmp = BitConverter.ToString(device.MemoryBytes).Replace('-', ' ');
                tmp = Regex.Replace(tmp, "(.{48})", "$1\n");
                foreach (string s in tmp.Split("\n"))
                {
                    sb.AppendLine(s.Insert(0, addr.ToString("X6") + ": "));
                    addr += 0x10;
                }
                rtb.Text = sb.ToString();
            }
        }

        void JumpToAddress(string addr)
        {
            int loc = rtb.Find(addr);
            if (loc > -1)
            {
                rtb.SelectionStart = loc;
                rtb.ScrollToCaret();
            }
            else
            {
                MessageBox.Show("No match");
            }
        }
        private void MemoryViewer_Load(object sender, EventArgs e)
        {
            noteLabel.Text = "";
        }

        private void jumpButton_Click(object sender, EventArgs e)
        {
            JumpToAddress(jumpTextBox.Text);
        }
    }
}
