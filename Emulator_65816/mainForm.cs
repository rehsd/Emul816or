using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emul816or;
using Microsoft.Win32;

namespace Emul816or
{
    public partial class mainForm : Form
    {
        CPU cpu;

        string ROMlocation;

        public bool SuspendLogging;
        public ushort speed;
        bool breakActive;
        int cyclesPrev = 0;
        const int MAXLENGTH = 500;

        ROM rom;
        RAM ram;
        ERAM eram;
        VIA via1;
        Video video;
        NullDevice nullDev;
        public Bitmap[] frameBuffer;
        public int ActiveFrame;

        LCD1602 lcd;

        public mainForm()
        {
            InitializeComponent();
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
;           ReloadForm();
        }

        private void ReloadForm()
        {
            //read last ROM location
            RegistryKey keyROM = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Emul816or");
            if (keyROM != null)
            {
                ROMlocation = keyROM.GetValue("MRU_ROM").ToString();
            }
            else
            {
                MessageBox.Show("No ROM information. Please select ROM for initialization.", "ROM required");
                GetROM();
            }
            LoadObjects();
            speed = 800;
            frameBuffer = new Bitmap[2];
            frameBuffer[0] = new Bitmap(320, 240);
            frameBuffer[1] = new Bitmap(320, 240);
            ActiveFrame = 0;
            LoadScanCodes();
            loggingToolStripMenuItem1.Checked = true;

        }

        private void GetROM()
        {
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                { 
                    ROMlocation = openFileDialog1.FileName;
                    currentROMLabel.Text = ROMlocation;
                    //store for next open
                    RegistryKey keyROM = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Emul816or");
                    keyROM.SetValue("MRU_ROM", openFileDialog1.FileName);
                    keyROM.Close();
                }
                else
                {
                    //do something...
                }
            }
        }
        private void loggingToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            loggingToolStripMenuItem1.Checked = !loggingToolStripMenuItem1.Checked;

            ClearSpeedChecks();
            if (!loggingToolStripMenuItem1.Checked)
            {
                fastestToolStripMenuItem1.Checked = true;
                speed = 1000;
            }
            else
            {
                eight00ToolStripMenuItem.Checked = true;
                speed = 800;
            }
            cpu.SuspendLogging = !loggingToolStripMenuItem1.Checked;
            this.SuspendLogging = cpu.SuspendLogging;
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cyclesTimer.Enabled = false;
            cpu.Stop(false);
            SuspendLogging = true;
            WriteLog("\n*************** STOP *****************\n");

        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TO DO Reset all state data from all objects of concern
            SuspendLogging = false;
            loggingToolStripMenuItem1.Checked = true;
            WriteLog("\n*************** RESET ****************\n");
            video.Reset();
            cpu.Reset();
            cyclesTimer.Enabled = true;
            videoOutRefreshTimer.Enabled = true;
            lcd.Reset();
            Run();
        }

        void LoadObjects()
        {
            currentROMLabel.Text = ROMlocation;

            ram = new RAM();
            WriteLog("RAM added:     0x000000-0x007FFF.\n");
            rom = new ROM(ROMlocation);
            WriteLog("ROM added:     0x008000-0x07FFFF.\n");
            eram = new ERAM();
            WriteLog("ERAM added:    0x080000-0x0FFFFF.\n");
            via1 = new VIA(0x108000);
            via1.VIAOutChanged += Via1_VIAOutChanged;
            WriteLog("VIA added:     0x108000.\n");
            video = new Video();
            WriteLog("VIDEO added:   0x200000.\n");
            videoOutRefreshTimer.Enabled = true;
            nullDev = new NullDevice();
            WriteLog("NullDev added: 0x******.\n");
            cpu = new CPU(rom, ram, eram, via1, video, nullDev);
            cpu.StatusChanged += cpu_StatusChanged;
            cpu.LogTextUpdate += cpu_LogTextUpdate;

            lcd = new LCD1602(LCDgroupBox);
        }

        private void Via1_VIAOutChanged(object sender, VIAOutChangedEventArgs e)
        {
            if (SuspendLogging) { return; }
            //Check which "devices" is connected to the VIA
            //UpdateVIABarGraphs(e.PortA, e.PortB);
            
            VIA v = (VIA)sender;

            if (v[v.BaseAddress + 0x02] > 0)        
            {
                //some bits are set to output (out from VIA)
                byte val = (byte)((byte)(v[v.BaseAddress + 0x02] & v[v.BaseAddress + 0x00]) | (byte)((byte)~v[v.BaseAddress + 0x02] & lcd.GetValue()));
                lcd.SetValue(val);  //need to look at individual bits
            }
            //if (v[v.BaseAddress + 0x02] < 255)        //if any of the direction bits are 0, the VIA needs to read back the value from the LCD (e.g., wait)
            //{
            //    //some bits are set as input (into VIA)
            //    byte val = (byte)((byte)(~v[v.BaseAddress + 0x02] & v[v.BaseAddress + 0x00]) | (byte)((byte)v[v.BaseAddress + 0x02] & lcd.GetValue()));
            //    v[v.BaseAddress + 0x00] = val;
            //}

        }

        void UpdateVIABarGraphs(byte portA, byte portB)
        {
            //00=B
            //01=A
            groupBox1.SuspendLayout();
            setPortBitDisplay(via1_portB_7, (portB & 0b10000000) > 0);
            setPortBitDisplay(via1_portB_6, (portB & 0b01000000) > 0);
            setPortBitDisplay(via1_portB_5, (portB & 0b00100000) > 0);
            setPortBitDisplay(via1_portB_4, (portB & 0b00010000) > 0);
            setPortBitDisplay(via1_portB_3, (portB & 0b00001000) > 0);
            setPortBitDisplay(via1_portB_2, (portB & 0b00000100) > 0);
            setPortBitDisplay(via1_portB_1, (portB & 0b00000010) > 0);
            setPortBitDisplay(via1_portB_0, (portB & 0b00000001) > 0);

            setPortBitDisplay(via1_portA_7, (portA & 0b10000000) > 0);
            setPortBitDisplay(via1_portA_6, (portA & 0b01000000) > 0);
            setPortBitDisplay(via1_portA_5, (portA & 0b00100000) > 0);
            setPortBitDisplay(via1_portA_4, (portA & 0b00010000) > 0);
            setPortBitDisplay(via1_portA_3, (portA & 0b00001000) > 0);
            setPortBitDisplay(via1_portA_2, (portA & 0b00000100) > 0);
            setPortBitDisplay(via1_portA_1, (portA & 0b00000010) > 0);
            setPortBitDisplay(via1_portA_0, (portA & 0b00000001) > 0);

            //groupBox1.Refresh();
            groupBox1.ResumeLayout();
        }
        private void setPortBitDisplay(PictureBox pb, bool value)
        {
            if (value)
            {
                pb.BackColor = Color.Red;
            }
            else
            {
                pb.BackColor = Color.Black;
            }
        }
        void Run()
        {
            while (!cpu.IsStopped() && !breakActive)
            {
                cpu.Step();
                Application.DoEvents();
                System.Threading.Thread.Sleep(1000 - speed);
            }
            if(cpu.IsStopped() && !this.IsDisposed)
            {
                WriteLog("\n*************** STP ******************\n");
            }
        }
        void WriteLog(string newText)
        {
            logText.SuspendLayout();
            if (logText.Lines.Length > MAXLENGTH)
            {
                List<string> lines = logText.Lines.ToList();
                lines.RemoveRange(0, lines.Count - MAXLENGTH);        //only keep 100 lines
                logText.Lines = lines.ToArray();
            }
            logText.AppendText(newText);
            logText.SelectionStart = logText.Text.Length;
            logText.ScrollToCaret();
            logText.ResumeLayout();
        }

        private void cpu_LogTextUpdate(object sender, LogTextUpdateEventArgs e)
        {
            WriteLog(e.NewText);
        }

        private void cpu_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (SuspendLogging) { return; }

            statusGroup.SuspendLayout();
            statusA.Text = e.A.ToString("X4");
            statusX.Text = e.X.ToString("X4");
            statusY.Text = e.Y.ToString("X4");
            statusS.Text = e.SP.ToString("X4");
            statusD.Text = BoolToString(e.FlagD);
            flagsN.Text = BoolToString(e.FlagN);
            flagsV.Text = BoolToString(e.FlagV);
            flagsM.Text = BoolToString(e.FlagM);
            flagsX.Text = BoolToString(e.FlagX);
            flagsD.Text = BoolToString(e.FlagD);
            flagsI.Text = BoolToString(e.FlagI);
            flagsZ.Text = BoolToString(e.FlagZ);
            flagsC.Text = BoolToString(e.FlagC);
            flagsE.Text = BoolToString(e.FlagE);
            statusPC.Text = e.PC.ToString("X4");
            statusCycles.Text = e.Cycles.ToString("X8");
            //statusGroup.Refresh();
            statusGroup.ResumeLayout();
        }

        string BoolToString(bool boolVal)
        {
            if(boolVal)
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }
        void ClearSpeedChecks()
        {
            slowestToolStripMenuItem1.Checked = false;
            fastestToolStripMenuItem1.Checked = false;
            eight00ToolStripMenuItem.Checked = false;
        }
        private void slowestToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ClearSpeedChecks();
            speed = 0;
        }

        private void fastestToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ClearSpeedChecks();
            speed = 1000;
        }

        private void manualSpeedToolStripTextBox2_Click(object sender, EventArgs e)
        {
            ClearSpeedChecks();
            speed = UInt16.Parse(manualSpeedToolStripTextBox2.Text);
        }

        private void eight00ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearSpeedChecks();
            speed = 800;
        }

        
        private void openRomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetROM();
        }

        private void breakToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!breakActive)
            {
                breakActive = true;
                breakToolStripMenuItem.Text = "Res&ume";
                stepToolStripMenuItem.Enabled = true;
            }
            else
            {
                breakActive = false;
                breakToolStripMenuItem.Text = "&Break";
                stepToolStripMenuItem.Enabled = false;
                Run();
            }
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cpu.Step();
        }

        private void memoryViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryViewer mv = new MemoryViewer(rom, ram, eram, video);
            mv.Show();
        }

        private void mainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (stepToolStripMenuItem.Enabled && e.KeyCode == Keys.F10)
            {
                cpu.Step();
                e.SuppressKeyPress = true;
            }
            else if(e.KeyCode == Keys.F5)
            {
                resetToolStripMenuItem_Click(null,null);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F6)
            {
                fastestToolStripMenuItem1_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F7)
            {
                breakToolStripMenuItem_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F8)
            {
                loggingToolStripMenuItem1_Click(null, null);
                e.SuppressKeyPress = true;
            }

        }

        private void cyclesTimer_Tick(object sender, EventArgs e)
        {
            if(!cpu.IsStopped())
            {
                int cyclesCurrent = cpu.Cycles;
                int clockEquiv = cyclesCurrent - cyclesPrev;
                if(clockEquiv < 1000)
                {
                    clockEquivLabel.Text = clockEquiv.ToString("D") + " Hz";
                }
                else if(clockEquiv < 1000000)
                {
                    clockEquivLabel.Text = (clockEquiv / (decimal)1000).ToString("F2") + " kHz";
                }
                else
                {
                    clockEquivLabel.Text = (clockEquiv / (decimal)1000000).ToString("F2") + " MHz";
                }
                cyclesPrev = cyclesCurrent;

            }
        }

        private void videoOutRefreshTimer_Tick(object sender, EventArgs e)
        {
            //Grabs whatever is currently in video memory. Not sync'd the vram updates in any way. Tearing will be visible.
            int newFrame;
            if (ActiveFrame == 1)
            {
                newFrame = 0;
            }
            else
            {
                newFrame = 1;
            }

            byte pixelColor;
            int red, blue, green;

            for (Int16 y = 0; y < 240; y++)
            {
                for (Int16 x = 0; x < 320; x++)
                {
                    pixelColor = video.MemoryBytes[512 * y + x];

                    red = (pixelColor & 224);
                    green = (pixelColor & 28) << 3;
                    blue = (pixelColor & 3) << 6;

                    frameBuffer[newFrame].SetPixel(x, y, Color.FromArgb(red, green, blue));
                }
            }
            videoOutPictureBox.Image = frameBuffer[newFrame];
            ActiveFrame = newFrame;
            videoFPSLabel.Text = (1000 / videoOutRefreshTimer.Interval).ToString();
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!cpu.IsStopped())
            {
                cpu.Stop();
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MapVirtualKeyExW", ExactSpelling = true)]
        internal static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

        byte[] virtualKeyScanCodes;
        private void LoadScanCodes()
        {
            virtualKeyScanCodes = new byte[256];
            for (uint scanCode = 0x00; scanCode <= 0xff; scanCode++)
            {
                uint virtualKeyCode = MapVirtualKeyEx(scanCode, 1, System.Windows.Forms.InputLanguage.CurrentInputLanguage.Handle);
                if (virtualKeyCode != 0)
                {
                    virtualKeyScanCodes[virtualKeyCode] = (byte)scanCode;
                }
            }
        }
        private void keyboardInputRichTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            lcd.Reset(false);
            //Put the key scancode value on the VIA port (to be read by interrupt handler)
            via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTA] = virtualKeyScanCodes[e.KeyValue];
            via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] = (byte)(via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] | 0b00000010);     //set CA1 as the source of the interrupt

            //Trigger interrupt on CPU         --normally done with signal from VIA to CPU
            cpu.SetIRQB(CPU.PinState.Low);
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadForm();
        }
    }
}


