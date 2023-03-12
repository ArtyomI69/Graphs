using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsGraphs {
    public delegate void AddEdgeDelegate(string from, string to, int weight, bool bothWays);
    public partial class AddEdgeForm : Form {
        AddEdgeDelegate AddEdge;
        public AddEdgeForm(AddEdgeDelegate addEdge) {
            InitializeComponent();
            AddEdge = addEdge;
        }

        private void buttonSubmit_Click(object sender, EventArgs e) {
            string from = textBoxFrom.Text;
            string to = textBoxTo.Text;
            int weight = (int)numericUpDownWeight.Value;
            bool bothWays = buttonUndirected.Checked;
            AddEdge(from, to, weight, bothWays);
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
