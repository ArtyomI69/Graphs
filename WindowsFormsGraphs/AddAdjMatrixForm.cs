using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace WindowsFormsGraphs {
    public delegate void CreateGraphDelegate(int[,] adjMatrix);
    public partial class AddAdjMatrixForm : Form {
        public CreateGraphDelegate CreateGraph;
        public AddAdjMatrixForm(CreateGraphDelegate createGraph) {
            InitializeComponent();
            CreateGraph = createGraph;
        }

        private void buttonSubmit_Click(object sender, EventArgs e) {
            try {
                string input = Regex.Replace(textBoxInput.Text, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline).TrimEnd();
                int N = input.Split('\n').Length;
                int[,] adjMatrix = new int[N, N];
                int i = 0, j = 0;
                foreach (string line in input.Split('\n')) {
                    string lineRemovedExtraSpaces = Regex.Replace(line, @"\s+", " ");
                    foreach (string el in lineRemovedExtraSpaces.Trim().Split(' ')) {
                        adjMatrix[i, j] = int.Parse(el);
                        j++;
                    }
                    j = 0;
                    i++;
                }
                CreateGraph(adjMatrix);
                Close();
            } catch {
                Close();
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
