using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emul816or;

namespace Emul816or
{
    public partial class mainForm : Form
    {
        CPU cpu;
        string ROMlocation;
        public bool SuspendLogging;
        public ushort speed;
        private UInt16 pixelSize =2;
        bool breakActive;

        ROM rom;
        RAM ram;
        ERAM eram;
        VIA via1;
        Video video;
        NullDevice nullDev;
        public Bitmap[] frameBuffer;
        public int ActiveFrame;

        public mainForm()
        {
            InitializeComponent();
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            LoadObjects();
            speed = 800;
            frameBuffer = new Bitmap[2];
            frameBuffer[0] = new Bitmap(320, 240);
            ActiveFrame = 0;
        }

        private void loggingToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if(!loggingToolStripMenuItem1.Checked)
            {
                fastestToolStripMenuItem1.Checked = true;
                speed = 1000;
            }
            cpu.SuspendLogging = !loggingToolStripMenuItem1.Checked;
            this.SuspendLogging = cpu.SuspendLogging;
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cpu.Stop(false);
            SuspendLogging = true;
            WriteLog("\n*************** STOP *****************\n");

        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //logText.Clear();
            SuspendLogging = false;
            WriteLog("\n*************** RESET ****************\n");
            video.Reset();
            cpu.Reset();
            Run();
        }

        void LoadObjects()
        {
            ROMlocation = @"d:\65816\ROM\g4.bin";
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
            video = new Video(this);
            WriteLog("VIDEO added:   0x200000.\n");
            nullDev = new NullDevice();
            WriteLog("NullDev added: 0x******.\n");
            cpu = new CPU(rom, ram, eram, via1, video, nullDev);
            cpu.StatusChanged += cpu_StatusChanged;
            cpu.LogTextUpdate += cpu_LogTextUpdate;
        }

        private void Via1_VIAOutChanged(object sender, VIAOutChangedEventArgs e)
        {
            if (SuspendLogging) { return; }
            UpdateVIABarGraphs(e.PortA, e.PortB);
        }

        void UpdateVIABarGraphs(byte portA, byte portB)
        {
            //00=B
            //01=A
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

            groupBox1.Refresh();
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
            if(cpu.IsStopped())
            {
                WriteLog("\n*************** STP ******************\n");
            }
        }
        void WriteLog(string newText)
        {
            if (logText.Lines.Length > 200)
            {
                List<string> lines = logText.Lines.ToList();
                lines.RemoveRange(0, lines.Count - 100);        //only keep 100 lines
                logText.Lines = lines.ToArray();
            }
            logText.AppendText(newText);
            logText.Refresh();
            logText.SelectionStart = logText.Text.Length;
            logText.ScrollToCaret();
        }

        private void cpu_LogTextUpdate(object sender, LogTextUpdateEventArgs e)
        {
            WriteLog(e.NewText);
        }

        private void cpu_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (SuspendLogging) { return; }

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
            statusGroup.Refresh();
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
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                ROMlocation = openFileDialog1.FileName;
                currentROMLabel.Text = ROMlocation;
            }
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
            }
            e.SuppressKeyPress = true;
        }
    }
}


