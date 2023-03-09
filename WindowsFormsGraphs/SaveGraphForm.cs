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
    public delegate void SaveGraphToFileDelegate(string fileName);
    public partial class SaveGraphForm : Form {
        public SaveGraphToFileDelegate SaveGraphToFile;
        public SaveGraphForm(SaveGraphToFileDelegate saveGraphToFile) {
            InitializeComponent();
            SaveGraphToFile = saveGraphToFile;
        }

        private void buttonSubmit_Click(object sender, EventArgs e) {
            try {
                if (textBoxInput.Text.Length == 0) return;
                SaveGraphToFile(textBoxInput.Text);
            } catch {
            }
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
