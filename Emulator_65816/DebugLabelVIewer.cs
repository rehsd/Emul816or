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
    public partial class DebugLabelViewer : Form
    {
        public Dictionary<string, string> debugLabels;

        public DebugLabelViewer(Dictionary<string, string> _debugLabels)
        {
            InitializeComponent();
            debugLabels = _debugLabels;
        }

        private void DebugLabelVIewer_Load(object sender, EventArgs e)
        {
            DataSet ds = new DataSet();
            DataTable dt = ds.Tables.Add("tbl");
            dt.Columns.Add("Label");
            dt.Columns.Add("Address");

            foreach (KeyValuePair<string, string> kvp in debugLabels.OrderBy(x => x.Key).ToArray())
            {
                dt.Rows.Add(kvp.Key, kvp.Value);
            }

            dataGridView1.DataSource = dt;
            dataGridView1.AutoResizeColumn(0);
            dataGridView1.AutoResizeColumn(1);
            dataGridView1.Columns[0].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns[1].SortMode = DataGridViewColumnSortMode.Automatic;
        }
    }
}
