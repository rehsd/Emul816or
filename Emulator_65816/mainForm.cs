using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
        VIA via1;   //PS2 keyboard, misc control signals (e.g., sound card)
        VIA via2;   //LCD, bar graph
        VIA via3;   //USB mouse
        VIA via4;   //Joystick
        VIA via5;   //VIA test harness
        Video video;
        Sound sound;
        NullDevice nullDev;
        public Bitmap[] frameBuffer;
        public int ActiveFrame;

        LCD1602 lcd;

        private int mousePreviousX = 0;
        private int mousePreviousY = 0;

        bool captureMouse = false;

        Dictionary<string, string> debugLabels;
        Dictionary<string, string> debugCode;
        bool debugFilesLoaded = false;

        public mainForm()
        {
            InitializeComponent();
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            ReloadForm();
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
                openFileDialog1.Filter = "bin files (*.bin)|*.bin";
                openFileDialog1.FileName = "";
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
            if (sender == null)
            {
                //only do this if manually calling this event (not from actual click)
                loggingToolStripMenuItem1.Checked = !loggingToolStripMenuItem1.Checked;
            }

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
            WriteLog("*** Loading Debug Info... ");
            WriteLog("Complete ***\n");
            debugToolStripMenuItem.Enabled = true;
            LoadDebugData();
            video.Reset();
            cpu.Reset();
            cyclesTimer.Enabled = true;
            videoOutRefreshTimer.Enabled = true;
            lcd.Reset();
            Run();
        }

        void LoadDebugData()
        {
            try
            {
                debugLabels = new Dictionary<string, string>();
                debugCode = new Dictionary<string, string>();
                string dir = Path.GetDirectoryName(ROMlocation) + "\\";
                string debugLabelsFile = dir + "debug.txt";
                string debugCodeFile = dir + "debugcode.txt";
                StreamReader labelsFile = File.OpenText(debugLabelsFile);
                StreamReader codeFile = File.OpenText(debugCodeFile);

                while (!labelsFile.EndOfStream)
                {
                    string[] s = labelsFile.ReadLine().Split(" ");
                    string theAddr = s[1];
                    if(theAddr.Length == 5)
                    {
                        theAddr = theAddr.Insert(1, "00");
                    }
                    debugLabels.Add(s[0], theAddr);
                }

                while (!codeFile.EndOfStream)
                {
                    string[] s = codeFile.ReadLine().Split(" ");
                    if (s[0].Trim().Length > 0)
                    {
                        string theKey = s[0];
                        if(theKey.Length==5)
                        {
                            theKey = theKey.Insert(1, "00");
                        }
                        string theRest = "";
                        for (int i = 1; i < s.Length; i++)
                        {
                            theRest += s[i] + " ";
                        }
                        debugCode.Add(theKey, theRest);
                    }
                }
                cpu.debugLabels = this.debugLabels;
                cpu.debugCode = this.debugCode;
                cpu.SetDebugInfo(debugLabels, debugCode);
                debugFilesLoaded = true;
            }
            catch (Exception)
            {
                cpu.SetDebugInfo(null, null);
                debugFilesLoaded = false;
            }
        }
        void LoadObjects()
        {
            currentROMLabel.Text = ROMlocation;
            WriteLog("Initializing...\n");
            ram = new RAM();
            WriteLog("RAM added\t\t\t0x000000-0x007FFF\n");
            rom = new ROM(ROMlocation);
            WriteLog("ROM added\t\t\t0x008000-0x07FFFF\n");
            eram = new ERAM();
            WriteLog("ERAM added\t\t\t0x080000-0x0FFFFF\n");
            via1 = new VIA(0x108000);   //keyboard on Port A, bus signals EXT on Port B
            via1.VIAOutChanged += Via1_VIAOutChanged;
            WriteLog("VIA1 (PS2 KBD) added\t\t0x108000-0x10800F\n");
            via2 = new VIA(0x104000);  //LCD add-in card
            via2.VIAOutChanged += Via2_VIAOutChanged;
            WriteLog("VIA2 (LCD, BARGRAPH) added\t0x104000-0x10400F\n");
            via3 = new VIA(0x102000);  //USB mouse
            via3.VIAOutChanged += Via3_VIAOutChanged;
            WriteLog("VIA3 (USB MOUSE) added\t0x102000-0x10200F\n");
            via4 = new VIA(0x101000);  //Joystick
            via4.VIAOutChanged += Via4_VIAOutChanged;
            WriteLog("VIA4 (JOYSTICK) added\t0x101000-0x10100F\n");
            via5 = new VIA(0x100800);  //VIA test harness
            via5.VIAOutChanged += Via5_VIAOutChanged;
            WriteLog("VIA5 (VIA TEST) added\t0x100800-0x10080F\n");
            sound = new Sound();
            WriteLog("SOUND added\t\t\t0x100000-0x1007FF\n");
            video = new Video();
            WriteLog("VIDEO added\t\t\t0x200000-0x21FFFF\n");
            videoOutRefreshTimer.Enabled = true;
            nullDev = new NullDevice();
            WriteLog("NullDev added\t\t\t0x******\n");
            cpu = new CPU(rom, ram, eram, via1, via2, via3, video, sound, nullDev);
            cpu.StatusChanged += cpu_StatusChanged;
            cpu.LogTextUpdate += cpu_LogTextUpdate;

            lcd = new LCD1602(LCDgroupBox);
        }

        private void Via5_VIAOutChanged(object sender, VIAOutChangedEventArgs e)
        {
            //VIA test harness
            //PortA is connected to PortB. Direction of out/in changes in assembly code. Used to test VIA ICs.
            //Check direction of ports, and read/write accordingly
            //Assuming that all bits in a port are of the same direction -- not mixing input/output in the same port
            VIA v = (VIA)sender;
            if(v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_DDRA] == 255 && v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_DDRB] == 00)      //Port A is output
            {
                //copy output of port A to input of port B
                v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTB] = v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTA];
            }
            else if(v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_DDRA] == 0 && v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_DDRB] == 255)   //Port A is input
            {
                //copy output of port B to input of port A
                v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTA] = v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTB];
            }
            else
            {
                throw new Exception("Unexpected value for port direction on VIA5");
            }
        }

        private void Via4_VIAOutChanged(object sender, VIAOutChangedEventArgs e)
        {
            //Joystick
            //Input only, so likely won't use this event
        }

        private void Via3_VIAOutChanged(object sender, VIAOutChangedEventArgs e)
        {
            //USB mouse
            //Input only, so likely won't use this event
        }

        private void Via2_VIAOutChanged(object sender, VIAOutChangedEventArgs e)
        {
            //output for LCD and bar graph

            VIA v = (VIA)sender;
            //PortA and PortB are output only (PortA=LCD, PortB=bar graph)
            byte newVal = v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTB];     //0x00 = PortB (adding purely as a reminder) - TO DO: setup constants for the registers
            UpdateVIA2BarGraph(newVal);


            if (v[v.BaseAddress + 0x03] > 0)
            {
                //some bits are set to output (out from VIA)
                byte val = (byte)((byte)(v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_DDRA] & v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTA]) | (byte)((byte)~v[v.BaseAddress + (uint)VIA.REGISTERS.VIA_DDRA] & lcd.GetValue()));
                lcd.SetValue(val);  //need to look at individual bits
            }
            //if (v[v.BaseAddress + 0x03] < 255)        //if any of the direction bits are 0, the VIA needs to read back the value from the LCD (e.g., wait)
            //{
            //    //some bits are set as input (into VIA)
            //    byte val = (byte)((byte)(~v[v.BaseAddress + 0x03] & v[v.BaseAddress + 0x01]) | (byte)((byte)v[v.BaseAddress + 0x32] & lcd.GetValue()));
            //    v[v.BaseAddress + 0x01] = val;
            //}
        }

        private void Via1_VIAOutChanged(object sender, VIAOutChangedEventArgs e)
        {
            //PS/2 keyboard input, Control signals output
        }

        void UpdateVIA2BarGraph(byte portB)
        {
            //00=B
            //01=A
            groupBox1.SuspendLayout();
            setPortBitDisplay(via2_portB_0, (portB & 0b10000000) > 0);
            setPortBitDisplay(via2_portB_1, (portB & 0b01000000) > 0);
            setPortBitDisplay(via2_portB_2, (portB & 0b00100000) > 0);
            setPortBitDisplay(via2_portB_3, (portB & 0b00010000) > 0);
            setPortBitDisplay(via2_portB_4, (portB & 0b00001000) > 0);
            setPortBitDisplay(via2_portB_5, (portB & 0b00000100) > 0);
            setPortBitDisplay(via2_portB_6, (portB & 0b00000010) > 0);
            setPortBitDisplay(via2_portB_7, (portB & 0b00000001) > 0);

            //setPortBitDisplay(via2_portA_7, (portA & 0b10000000) > 0);
            //setPortBitDisplay(via2_portA_6, (portA & 0b01000000) > 0);
            //setPortBitDisplay(via2_portA_5, (portA & 0b00100000) > 0);
            //setPortBitDisplay(via2_portA_4, (portA & 0b00010000) > 0);
            //setPortBitDisplay(via2_portA_3, (portA & 0b00001000) > 0);
            //setPortBitDisplay(via2_portA_2, (portA & 0b00000100) > 0);
            //setPortBitDisplay(via2_portA_1, (portA & 0b00000010) > 0);
            //setPortBitDisplay(via2_portA_0, (portA & 0b00000001) > 0);

            //groupBox1.Refresh();
            groupBox1.ResumeLayout();
        }
        private static void setPortBitDisplay(PictureBox pb, bool value)
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
            if (cpu.IsStopped() && !this.IsDisposed)
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

        static string BoolToString(bool boolVal)
        {
            if (boolVal)
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
            MemoryViewer mv = new(rom, ram, eram, video);
            mv.Show();
        }

        private void mainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (stepToolStripMenuItem.Enabled && e.KeyCode == Keys.F10)
            {
                cpu.Step();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                centerMouseForCaptureToolStripMenuItem_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                resetToolStripMenuItem_Click(null, null);
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
            else if (e.KeyCode == Keys.F9)
            {
                manualSpeedToolStripTextBox2_Click(null, null);
                e.SuppressKeyPress = true;
            }

        }

        private void cyclesTimer_Tick(object sender, EventArgs e)
        {
            if (!cpu.IsStopped())
            {
                int cyclesCurrent = cpu.Cycles;
                int clockEquiv = cyclesCurrent - cyclesPrev;
                if (clockEquiv < 1000)
                {
                    clockEquivLabel.Text = clockEquiv.ToString("D") + " Hz";
                }
                else if (clockEquiv < 1000000)
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
            //from Windows API
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
            if (IsFilteredKey(e.KeyCode))
            {
                return;
            }

            lcd.Reset(false);
            //Put the key scancode value on the VIA port (to be read by interrupt handler)
            //via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTA] = virtualKeyScanCodes[e.KeyValue];
            //via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] = (byte)(via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] | 0b00000010);     //set CA1 as the source of the interrupt

            byte scanCode;
            switch (e.KeyValue)
            {
                case 0x0D:              //CR / ENTER / RETURN
                    scanCode = 0x5A;
                    break;
                case 0x10:              //left, right shift
                    scanCode = 0x12;
                    break;
                case 0x1B:              //ESC
                    scanCode = 0x76;
                    break;
                case 0x20:              //SPACE
                    scanCode = 0x29;
                    break;
                case 0x21:              //!
                    scanCode = 0x16;
                    break;
                case 0x22:              //"
                    scanCode = 0x52;
                    break;
                case 0x23:              //#
                    scanCode = 0x26;
                    break;
                case 0x24:              //$
                    scanCode = 0x25;
                    break;
                case 0x25:              //%
                    scanCode = 0x2E;
                    break;
                case 0x26:              //&
                    scanCode = 0x3D;
                    break;
                case 0x27:              //'
                    scanCode = 0x52;
                    break;
                case 0x28:              //(
                    scanCode = 0x46;
                    break;
                case 0x29:              //)
                    scanCode = 0x45;
                    break;
                case 0x2A:              //*
                    scanCode = 0x3E;
                    break;
                case 0x2B:              //+
                    scanCode = 0x55;
                    break;
                case 0x2C:              //,
                    scanCode = 0x41;
                    break;
                case 0x2D:              //-
                    scanCode = 0x4E;
                    break;
                case 0x2E:              //.
                    scanCode = 0x49;
                    break;
                case 0x2F:              ///
                    scanCode = 0x4A;
                    break;
                case 0x30:              //0
                    scanCode = 0x45;
                    break;
                case 0x31:              //1
                    scanCode = 0x16;
                    break;
                case 0x32:              //2
                    scanCode = 0x1E;
                    break;
                case 0x33:              //3
                    scanCode = 0x26;
                    break;
                case 0x34:              //4
                    scanCode = 0x25;
                    break;
                case 0x35:              //5
                    scanCode = 0x2E;
                    break;
                case 0x36:              //6
                    scanCode = 0x36;
                    break;
                case 0x37:              //7
                    scanCode = 0x3D;
                    break;
                case 0x38:              //8
                    scanCode = 0x3E;
                    break;
                case 0x39:              //9
                    scanCode = 0x46;
                    break;
                case 0x3A:              //:
                    scanCode = 0x4C;
                    break;
                case 0x3B:              //;
                    scanCode = 0x4C;
                    break;
                case 0x3C:              //<
                    scanCode = 0x41;
                    break;
                case 0x3D:              //=
                    scanCode = 0x55;
                    break;
                case 0x3E:              //>
                    scanCode = 0x49;
                    break;
                case 0x3F:              //?
                    scanCode = 0x4A;
                    break;
                case 0x40:              //@
                    scanCode = 0x1E;
                    break;
                case 0x41:              //A
                    scanCode = 0x1C;
                    break;
                case 0x42:              //B
                    scanCode = 0x32;
                    break;
                case 0x43:              //C
                    scanCode = 0x21;
                    break;
                case 0x44:              //D
                    scanCode = 0x23;
                    break;
                case 0x45:              //E
                    scanCode = 0x24;
                    break;
                case 0x46:              //F
                    scanCode = 0x2B;
                    break;
                case 0x47:              //G
                    scanCode = 0x34;
                    break;
                case 0x48:              //H
                    scanCode = 0x33;
                    break;
                case 0x49:              //I
                    scanCode = 0x43;
                    break;
                case 0x4A:              //J
                    scanCode = 0x3B;
                    break;
                case 0x4B:              //K
                    scanCode = 0x42;
                    break;
                case 0x4C:              //L
                    scanCode = 0x4B;
                    break;
                case 0x4D:              //M
                    scanCode = 0x3A;
                    break;
                case 0x4E:              //N
                    scanCode = 0x31;
                    break;
                case 0x4F:              //O
                    scanCode = 0x44;
                    break;
                case 0x50:              //P
                    scanCode = 0x4D;
                    break;
                case 0x51:              //Q
                    scanCode = 0x15;
                    break;
                case 0x52:              //R
                    scanCode = 0x2D;
                    break;
                case 0x53:              //S
                    scanCode = 0x1B;
                    break;
                case 0x54:              //T
                    scanCode = 0x2C;
                    break;
                case 0x55:              //U
                    scanCode = 0x3C;
                    break;
                case 0x56:              //V
                    scanCode = 0x2A;
                    break;
                case 0x57:              //W
                    scanCode = 0x1D;
                    break;
                case 0x58:              //X
                    scanCode = 0x22;
                    break;
                case 0x59:              //Y
                    scanCode = 0x35;
                    break;
                case 0x5A:              //Z
                    scanCode = 0x1A;
                    break;
                case 0x5B:              //[
                    scanCode = 0x54;
                    break;
                case 0xDC:              //\
                    scanCode = 0x5D;
                    break;
                case 0x5D:              //]
                    scanCode = 0x5B;
                    break;
                case 0x5E:              //^
                    scanCode = 0x36;
                    break;
                case 0x5F:              //_
                    scanCode = 0x4E;
                    break;
                case 0x60:              //`
                    scanCode = 0x0E;
                    break;
                case 0x7B:              //{
                    scanCode = 0x54;
                    break;
                case 0x7C:              //|
                    scanCode = 0x5D;
                    break;
                case 0x7D:              //}
                    scanCode = 0x5B;
                    break;
                case 0x7E:              //~
                    scanCode = 0x0E;
                    break;
                default:
                    scanCode = 0;
                    break;
            }
            via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTA] = scanCode;
            via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] = (byte)(via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] | 0b10000000);
            via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] = (byte)(via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] | 0b00000010);     //set CA1 as the source of the interrupt   (T1, T2, CB1, CB2, SR, CA1, CA2)

            //Trigger interrupt on CPU         --normally done with signal from VIA to CPU
            //cpu.SetIRQB(CPU.PinState.Low, true);
            InterruptAndWait(true);
            via1.ResetInterrupt();

        }

        void InterruptAndWait(bool keyboardAsSource=false)
        {
            processingPictureBox.BackColor = Color.Red;
            processingPictureBox.Refresh();
            cpu.SetIRQB(CPU.PinState.Low, true, keyboardAsSource);
            processingPictureBox.BackColor = Color.LightGray;
            processingPictureBox.Refresh();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadForm();
        }

        bool IsFilteredKey(Keys k)
        {
            if (k == Keys.F2 || k == Keys.F5 || k == Keys.F6 || k == Keys.F7 || k == Keys.F8 || k == Keys.F9 || k == Keys.F10 || k == Keys.F11 || k == Keys.F12 || k == Keys.Alt || k == Keys.Tab || k == Keys.Menu || k == Keys.ControlKey)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private void keyboardInputRichTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (IsFilteredKey(e.KeyCode))
            {
                return;
            }
            else
            {
                lcd.Reset(false);
                byte scanCode;

                scanCode = 0xF0;
                via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTA] = scanCode;
                via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] = (byte)(via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] | 0b10000000);
                via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] = (byte)(via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] | 0b00000010);     //set CA1 as the source of the interrupt   (T1, T2, CB1, CB2, SR, CA1, CA2)
                                                                                                                                                      //cpu.SetIRQB(CPU.PinState.Low, true);    //let processor complete to RTI
                InterruptAndWait(true);
                via1.ResetInterrupt();

                switch (e.KeyValue)
                {
                    case 0x0D:              //CR / ENTER / RETURN
                        scanCode = 0x5A;
                        break;
                    case 0x10:              //left, right shift
                        scanCode = 0x12;
                        break;
                    case 0x1B:              //ESC
                        scanCode = 0x76;
                        break;
                    case 0x20:              //SPACE
                        scanCode = 0x29;
                        break;
                    case 0x21:              //!
                        scanCode = 0x16;
                        break;
                    case 0x22:              //"
                        scanCode = 0x52;
                        break;
                    case 0x23:              //#
                        scanCode = 0x26;
                        break;
                    case 0x24:              //$
                        scanCode = 0x25;
                        break;
                    case 0x25:              //%
                        scanCode = 0x2E;
                        break;
                    case 0x26:              //&
                        scanCode = 0x3D;
                        break;
                    case 0x27:              //'
                        scanCode = 0x52;
                        break;
                    case 0x28:              //(
                        scanCode = 0x46;
                        break;
                    case 0x29:              //)
                        scanCode = 0x45;
                        break;
                    case 0x2A:              //*
                        scanCode = 0x3E;
                        break;
                    case 0x2B:              //+
                        scanCode = 0x55;
                        break;
                    case 0x2C:              //,
                        scanCode = 0x41;
                        break;
                    case 0x2D:              //-
                        scanCode = 0x4E;
                        break;
                    case 0x2E:              //.
                        scanCode = 0x49;
                        break;
                    case 0x2F:              ///
                        scanCode = 0x4A;
                        break;
                    case 0x30:              //0
                        scanCode = 0x45;
                        break;
                    case 0x31:              //1
                        scanCode = 0x16;
                        break;
                    case 0x32:              //2
                        scanCode = 0x1E;
                        break;
                    case 0x33:              //3
                        scanCode = 0x26;
                        break;
                    case 0x34:              //4
                        scanCode = 0x25;
                        break;
                    case 0x35:              //5
                        scanCode = 0x2E;
                        break;
                    case 0x36:              //6
                        scanCode = 0x36;
                        break;
                    case 0x37:              //7
                        scanCode = 0x3D;
                        break;
                    case 0x38:              //8
                        scanCode = 0x3E;
                        break;
                    case 0x39:              //9
                        scanCode = 0x46;
                        break;
                    case 0x3A:              //:
                        scanCode = 0x4C;
                        break;
                    case 0x3B:              //;
                        scanCode = 0x4C;
                        break;
                    case 0x3C:              //<
                        scanCode = 0x41;
                        break;
                    case 0x3D:              //=
                        scanCode = 0x55;
                        break;
                    case 0x3E:              //>
                        scanCode = 0x49;
                        break;
                    case 0x3F:              //?
                        scanCode = 0x4A;
                        break;
                    case 0x40:              //@
                        scanCode = 0x1E;
                        break;
                    case 0x41:              //A
                        scanCode = 0x1C;
                        break;
                    case 0x42:              //B
                        scanCode = 0x32;
                        break;
                    case 0x43:              //C
                        scanCode = 0x21;
                        break;
                    case 0x44:              //D
                        scanCode = 0x23;
                        break;
                    case 0x45:              //E
                        scanCode = 0x24;
                        break;
                    case 0x46:              //F
                        scanCode = 0x2B;
                        break;
                    case 0x47:              //G
                        scanCode = 0x34;
                        break;
                    case 0x48:              //H
                        scanCode = 0x33;
                        break;
                    case 0x49:              //I
                        scanCode = 0x43;
                        break;
                    case 0x4A:              //J
                        scanCode = 0x3B;
                        break;
                    case 0x4B:              //K
                        scanCode = 0x42;
                        break;
                    case 0x4C:              //L
                        scanCode = 0x4B;
                        break;
                    case 0x4D:              //M
                        scanCode = 0x3A;
                        break;
                    case 0x4E:              //N
                        scanCode = 0x31;
                        break;
                    case 0x4F:              //O
                        scanCode = 0x44;
                        break;
                    case 0x50:              //P
                        scanCode = 0x4D;
                        break;
                    case 0x51:              //Q
                        scanCode = 0x15;
                        break;
                    case 0x52:              //R
                        scanCode = 0x2D;
                        break;
                    case 0x53:              //S
                        scanCode = 0x1B;
                        break;
                    case 0x54:              //T
                        scanCode = 0x2C;
                        break;
                    case 0x55:              //U
                        scanCode = 0x3C;
                        break;
                    case 0x56:              //V
                        scanCode = 0x2A;
                        break;
                    case 0x57:              //W
                        scanCode = 0x1D;
                        break;
                    case 0x58:              //X
                        scanCode = 0x22;
                        break;
                    case 0x59:              //Y
                        scanCode = 0x35;
                        break;
                    case 0x5A:              //Z
                        scanCode = 0x1A;
                        break;
                    case 0x5B:              //[
                        scanCode = 0x54;
                        break;
                    case 0xDC:              //\
                        scanCode = 0x5D;
                        break;
                    case 0x5D:              //]
                        scanCode = 0x5B;
                        break;
                    case 0x5E:              //^
                        scanCode = 0x36;
                        break;
                    case 0x5F:              //_
                        scanCode = 0x4E;
                        break;
                    case 0x60:              //`
                        scanCode = 0x0E;
                        break;
                    case 0x7B:              //{
                        scanCode = 0x54;
                        break;
                    case 0x7C:              //|
                        scanCode = 0x5D;
                        break;
                    case 0x7D:              //}
                        scanCode = 0x5B;
                        break;
                    case 0x7E:              //~
                        scanCode = 0x0E;
                        break;
                    default:
                        scanCode = 0;
                        break;
                }

                via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTA] = scanCode;
                via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] = (byte)(via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] | 0b10000000);
                via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] = (byte)(via1[via1.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] | 0b00000010);     //set CA1 as the source of the interrupt   (T1, T2, CB1, CB2, SR, CA1, CA2)
                                                                                                                                                      //cpu.SetIRQB(CPU.PinState.Low, true);    //let processor complete to RTI
                InterruptAndWait(true);
                via1.ResetInterrupt();
            }
        }

        private void videoOutPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!captureMouse)
            {
                return;
            }
            // Six bits on VIA required
            // Two MSB bits unused. Next four bits for mouse move direction. Two LSB bits for mouse buttons.
            // xxMMMMBB
            // 
            // Mouse direction bits
            //      0000    No mouse movement
            //      0001    Up
            //      0010    RightUp
            //      0011    Right
            //      0100    RightDown
            //      0101    Down
            //      0110    LeftDown
            //      0111    Left
            //      1000    LeftUp
            //      1001 to 1111 are unused combinations
            //  
            // Mouse buttons bits
            //      00      No mouse buttons pressed
            //      01      Left button down
            //      10      Middle button down
            //      11      Right button down

            int relativeX, relativeY;
            relativeX = e.Location.X * 320 / videoOutPictureBox.Width;
            relativeY = e.Location.Y * 240 / videoOutPictureBox.Height;

            mousePosLabel.Text = relativeX.ToString() + "," + relativeY.ToString();

            byte mouseData = 0;
            if (relativeY < mousePreviousY && relativeX < mousePreviousX)        //leftup
            {
                mouseData = 0b00100000;
            }
            else if (relativeY < mousePreviousY && relativeX > mousePreviousX)   //rightup
            {
                mouseData = 0b00001000;
            }
            else if (relativeY > mousePreviousY && relativeX < mousePreviousX)   //leftdown
            {
                mouseData = 0b00011000;
            }
            else if (relativeY > mousePreviousY && relativeX > mousePreviousX)   //rightdown
            {
                mouseData = 0b00010000;
            }
            else if (relativeY > mousePreviousY)   //down
            {
                mouseData = 0b00010100;
            }
            else if (relativeY < mousePreviousY)   //up
            {
                mouseData = 0b00000100;
            }
            else if (relativeX > mousePreviousX)   //right
            {
                mouseData = 0b00001100;
            }
            else if (relativeX < mousePreviousX)   //left
            {
                mouseData = 0b00011100;
            }

            via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTB] = (byte)(mouseData | (byte)(via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTB] & (byte)0b11000011));
            via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] = (byte)(via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] | 0b10000000);     //negative bit, 128 position
            via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] = (byte)(via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] | 0b00100000);     //set CB1 as the source of the interrupt   (T1, T2, CB1, CB2, SR, CA1, CA2)


            //Trigger interrupt on CPU         --normally done with signal from VIA to CPU
            //cpu.SetIRQB(CPU.PinState.Low, true);
            InterruptAndWait();
            via3.ResetInterrupt();

            mousePreviousX = relativeX;
            mousePreviousY = relativeY;

            //TO DO - Mouse clicking
        }

        private void videoOutPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
           if(!captureMouse)
            {
                return;
            }
            byte mouseData = 0;

            if (e.Button == MouseButtons.Left)
            {
                mouseData = 0b00000001;
            }
            else if (e.Button == MouseButtons.Right)
            {
                mouseData = 0b00000011;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                mouseData = 0b00000010;
            }

            via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTB] = (byte)(mouseData | (byte)(via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTB] & (byte)0b11111100));
            via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] = (byte)(via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] | 0b10000000);     //negative bit, 128 position
            via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] = (byte)(via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] | 0b00100000);     //set CB1 as the source of the interrupt   (T1, T2, CB1, CB2, SR, CA1, CA2)

            //Trigger interrupt on CPU         --normally done with signal from VIA to CPU
            //cpu.SetIRQB(CPU.PinState.Low, true);
            InterruptAndWait();
            via3.ResetInterrupt();
        }

        private void centerMouseForCaptureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!captureMouse)
            {
                this.Cursor = new Cursor(Cursor.Current.Handle);
                Cursor.Position = new Point(videoOutPictureBox.Left + (videoOutPictureBox.Width / 2),
                    videoOutPictureBox.Top + (videoOutPictureBox.Height / 2));
                keyboardInputRichTextBox.Focus();
                //Cursor.Clip = new Rectangle(this.Location, this.Size);
                captureMouse = true;
                centerMouseForCaptureToolStripMenuItem.Checked = true;
            }
            else
            {
                centerMouseForCaptureToolStripMenuItem.Checked = false;
                captureMouse = false;
            }
        }

        private void videoOutPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (!captureMouse)
            {
                return;
            }
            byte mouseData = 0;

            via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTB] = (byte)(mouseData | (byte)(via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_PORTB] & (byte)0b11111100));
            via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] = (byte)(via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IFR] | 0b10000000);     //negative bit, 128 position
            via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] = (byte)(via3[via3.BaseAddress + (uint)VIA.REGISTERS.VIA_IER] | 0b00100000);     //set CB1 as the source of the interrupt   (T1, T2, CB1, CB2, SR, CA1, CA2)

            //Trigger interrupt on CPU         --normally done with signal from VIA to CPU
            //cpu.SetIRQB(CPU.PinState.Low, true);
            InterruptAndWait();
            via3.ResetInterrupt();
        }

        private void viewDebugCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugCodeViewer frm = new DebugCodeViewer(debugCode);
            frm.Show();
        }

        private void viewDebugLabelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugLabelViewer frm = new DebugLabelViewer(debugLabels);
            frm.Show();
        }
    }
}