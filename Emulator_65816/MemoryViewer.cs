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
    public partial class MemoryViewer : Form
    {
        ROM rom;
        RAM ram;
        ERAM eram;
        Video video;

        public MemoryViewer(ROM _rom, RAM _ram, ERAM _eram, Video _video)
        {
            InitializeComponent();
        }
    }
}
