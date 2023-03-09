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
    public delegate void RemoveEdgeDelegate(string name1, string name2);
    public partial class RemoveEdgeForm : Form {
        public RemoveEdgeDelegate RemoveEdge;
        public RemoveEdgeForm(RemoveEdgeDelegate removeEdge) {
            InitializeComponent();
            RemoveEdge = removeEdge;
        }

        private void buttonSubmit_Click(object sender, EventArgs e) {
            string from = textBoxFrom.Text;
            string to = textBoxTo.Text;
            RemoveEdge(from, to);
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
