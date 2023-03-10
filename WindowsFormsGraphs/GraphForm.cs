using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace WindowsFormsGraphs {
    public enum SelectedMode {
        AddVertex,
        DeleteVertex
    }

    public partial class GraphForm : Form {
        #region Поля
        Graph graph;
        SelectedMode selectedMode = SelectedMode.AddVertex;
        CancellationTokenSource tokenSource;
        bool isAlgorithmRunning = false;

        Bitmap bm;
        Graphics g;
        #endregion

        #region Конструктор и Resize обработчик событий
        public GraphForm() {
            InitializeComponent();

            InitializaeState();
        }

        private void GraphForm_Resize(object sender, EventArgs e) {
            InitializaeState();
        }

        private void InitializaeState() {
            ConfigureBitmapSize();

            buttonAddVert.BackColor = Color.Gray;
            buttonDelVert.BackColor = Color.White;

            Graph.SetMethodsForDrawing(DrawVertex, DrawVertices);
            graph = new Graph();
        }

        private void ConfigureBitmapSize() {
            if (pictureBox.Width == 0 || pictureBox.Height == 0) return;
            bm = new Bitmap(pictureBox.Width, pictureBox.Height);
            g = Graphics.FromImage(bm);
            ClearPictureBox();
        }
        #endregion

        #region Панель управления
        private void buttonAddVert_Click(object sender, EventArgs e) {
            SelectAddVertexMode();
        }

        private void buttonDelVert_Click(object sender, EventArgs e) {
            SelectDelVertexMode();
        }

        private void buttonAddEdge_Click(object sender, EventArgs e) {
            AddEdgeForm edgeForm = new AddEdgeForm(AddEdge);
            edgeForm.Show();
        }

        private void buttonDelEdge_Click(object sender, EventArgs e) {
            RemoveEdgeForm edgeForm = new RemoveEdgeForm(RemoveEdge);
            edgeForm.Show();
        }

        private void buttonAddAdjMatrix_Click(object sender, EventArgs e) {
            AddAdjMatrixForm adjMatrixForm = new AddAdjMatrixForm(CreateAdjMatrixGraph);
            adjMatrixForm.Show();
            DrawPictureBox();
        }

        private void buttonSave_Click(object sender, EventArgs e) {
            SaveGraphForm saveGraphForm = new SaveGraphForm(SaveGraphToFile);
            saveGraphForm.Show();
        }

        private void buttonLoad_Click(object sender, EventArgs e) {
            LoadGraphFromFile();
        }

        private void buttonClear_Click(object sender, EventArgs e) {
            graph = new Graph();
            ClearPictureBox();
            SelectAddVertexMode();
            textBoxResults.Text = "";
        }

        private void SelectAddVertexMode() {
            selectedMode = SelectedMode.AddVertex;
            buttonAddVert.BackColor = Color.Gray;
            buttonAddVert.Focus();
            buttonDelVert.BackColor = Color.White;
        }

        private void SelectDelVertexMode() {
            selectedMode = SelectedMode.DeleteVertex;
            buttonDelVert.BackColor = Color.Gray;
            buttonDelVert.Focus();
            buttonAddVert.BackColor = Color.White;
        }

        private void CreateAdjMatrixGraph(int[,] matrix) {
            graph = new Graph(matrix);
            AddRandomVerteciesDrawing();
            DrawPictureBox();
        }

        private string GetValidName() {
            int i = 0;
            string name = Convert.ToChar(graph.Vertices.Count + 65 + i).ToString();
            while (graph.Vertices.TryGetValue(name, out Vertex _)) {
                i++;
                name = Convert.ToChar(graph.Vertices.Count + 65 + i).ToString();
            }
            return name;
        }

        private void AddVertex(int x, int y) {
            string name = GetValidName();

            VertexDrawing vertexDrawing = new VertexDrawing(x, y, name);
            Vertex newVertex = new Vertex(name);
            newVertex.VertexDrawing = vertexDrawing;
            graph.AddVertex(newVertex);
            DrawPictureBox();
        }

        private void DeleteVertex(int x, int y) {
            try {
                foreach (Vertex vertex in graph.Vertices.Values) {
                    VertexDrawing vertexDrawing = vertex.VertexDrawing;
                    Rectangle regionCapture = vertexDrawing.RegionCapture();
                    bool inWidthRange = x > regionCapture.X && x < regionCapture.X + regionCapture.Width;
                    bool inHeightRange = y > regionCapture.Y && y < regionCapture.Y + regionCapture.Height;
                    if (inWidthRange && inHeightRange) {
                        graph.RemoveVertex(vertex.Name);
                        DrawPictureBox();
                        return;
                    }
                }
            } catch (Exception e) {
                textBoxResults.Text = e.Message;
            }
        }

        private void AddEdge(string from, string to, int weight, bool bothWays) {
            try {
                if (bothWays)
                    graph.AddEdgeBothWays(from, to, weight);
                else
                    graph.AddEdge(from, to, weight);
                DrawPictureBox();
            } catch (Exception e) {
                textBoxResults.Text = e.Message;
            }
        }

        public void RemoveEdge(string name1, string name2) {
            try {
                graph.RemoveEdge(name1, name2);
                DrawPictureBox();
            } catch (Exception e) {
                textBoxResults.Text = e.Message;
            }
        }

        private void AddRandomVerteciesDrawing() {
            Random r = new Random();
            foreach (Vertex vertex in graph.Vertices.Values) {
                int x = (int)(pictureBox.Width * 0.2 + pictureBox.Width * r.NextDouble() * 0.6);
                int y = (int)(pictureBox.Height * 0.2 + pictureBox.Height * r.NextDouble() * 0.6);
                VertexDrawing vertexDrawing = new VertexDrawing(x, y, vertex.Name);
                vertex.VertexDrawing = vertexDrawing;
            }
        }

        private string MatrixToString(int[,] matrix) {
            string res = "";
            for (int i = 0; i < matrix.GetLength(0); i++) {
                for (int j = 0; j < matrix.GetLength(1); j++) {
                    res += $"{matrix[i, j]} ";
                }
                res += "\n";
            }
            return res;
        }

        private void SaveGraphToFile(string fileName) {
            string text = MatrixToString(graph.AdjMatrix);
            if (text.Length > 0) text += "-----\n";
            foreach (Vertex vertex in graph.Vertices.Values) {
                VertexDrawing vertexDrawing = vertex.VertexDrawing;
                float x = vertexDrawing.Center.X;
                float y = vertexDrawing.Center.Y;
                text += $"{x};{y}\n";
            }
            string path = GetRelativePath() + $"{fileName}.txt";
            using (StreamWriter sw = File.CreateText(path)) {
                sw.Write(text);
            }
            textBoxResults.Text = "Граф успешно сохранён.";
        }

        private (string adjMatrixText, string verticesDrawingText) GetFileGraphInfo(string fileName) {
            string fileText = RemoveEmptyLines(File.ReadAllText(fileName));
            string[] fileTextArr = fileText.Split('\n');
            string adjMatrixText = "";
            string verticesDrawingText = "";
            bool isAdjMatrix = true;
            for (int i = 0; i < fileTextArr.Length; i++) {
                if (fileTextArr[i] == "-----") {
                    isAdjMatrix = false;
                    continue;
                }

                if (isAdjMatrix) adjMatrixText += $"{fileTextArr[i]}\n";
                else verticesDrawingText += $"{fileTextArr[i]}\n";
            }
            adjMatrixText = RemoveEmptyLines(adjMatrixText);
            verticesDrawingText = RemoveEmptyLines(verticesDrawingText);
            return (adjMatrixText, verticesDrawingText);
        }

        private void GetGraphFromAdjMatrix(string adjMatrixText) {
            int N = adjMatrixText.Split('\n').Length;
            int[,] adjMatrix = new int[N, N];
            int i = 0, j = 0;
            foreach (string line in adjMatrixText.Split('\n')) {
                foreach (string el in line.Trim().Split(' ')) {
                    adjMatrix[i, j] = int.Parse(el);
                    j++;
                }
                j = 0;
                i++;
            }
            graph = new Graph(adjMatrix);
        }

        private void GetVerticesDrawingFromText(string verticesDrawingText) {
            int N = verticesDrawingText.Split('\n').Length;
            (int x, int y)[] verticesDrawing = new (int x, int y)[N];
            int i = 0;
            foreach (string line in verticesDrawingText.Split('\n')) {
                string[] el = line.Split(';');
                verticesDrawing[i] = (int.Parse(el[0]), int.Parse(el[1]));
                i++;
            }

            i = 0;
            foreach (Vertex vertex in graph.Vertices.Values) {
                VertexDrawing vertexDrawing = new VertexDrawing(verticesDrawing[i].x, verticesDrawing[i].y, vertex.Name);
                vertex.VertexDrawing = vertexDrawing;
                i++;
            }
        }

        private void LoadGraphFromFile() {
            string relativePath = GetRelativePath();
            OpenFileDialog openFileDialog = new OpenFileDialog() {
                Filter = "Text|*.txt|All|*.*",
                Title = "Выберите граф для загрузки",
                InitialDirectory = relativePath
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            string fileName = openFileDialog.FileName;
            (string adjMatrixText, string verticesDrawingText) = GetFileGraphInfo(fileName);
            if (adjMatrixText.Length == 0 || verticesDrawingText.Length == 0) return;

            GetGraphFromAdjMatrix(adjMatrixText);

            GetVerticesDrawingFromText(verticesDrawingText);

            DrawPictureBox();
        }

        private string GetRelativePath() {
            string[] separatingString = { "WindowsFormsGraphs" };
            string relativePath = Directory.GetCurrentDirectory().Split(separatingString, StringSplitOptions.None)[0];
            return relativePath;
        }

        private void pictureBox_MouseClick(object sender, MouseEventArgs e) {
            if (isAlgorithmRunning) return;
            if (selectedMode == SelectedMode.AddVertex) {
                AddVertex(e.X, e.Y);
            }
            if (selectedMode == SelectedMode.DeleteVertex) {
                DeleteVertex(e.X, e.Y);
            }
        }
        #endregion

        #region Алгоритмы
        private void buttonStart_Click(object sender, EventArgs e) {
            string selectedAlgo = comboBoxAlgos.SelectedItem.ToString();
            switch (selectedAlgo) {
                case "Матрица смежности": {
                        GetAdjMatrix();
                        break;
                    }
                case "BFS": {
                        BFS();
                        break;
                    }
                case "BFS Matrix": {
                        BFSMatrix();
                        break;
                    }
                case "DFS": {
                        DFS();
                        break;
                    }
                case "DFS Matrix": {
                        DFSMatrix();
                        break;
                    }
                case "Алгоритм Краскала": {
                        Kruskal();
                        break;
                    }
                case "Алгоритм Прима": {
                        Prima();
                        break;
                    }
                default: break;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            tokenSource.Cancel();
        }

        private void GetAdjMatrix() {
            textBoxResults.Text = graph.GetAdjMatrixString();
        }

        private async void BFS() {
            CancellationToken ct = StartAlgorithmProccess();
            await Task.Run(() => {
                try {
                    string name = textBoxFrom.Text;
                    string res = graph.BFS(name, ct);
                    textBoxResults.Invoke((MethodInvoker)(() => textBoxResults.Text = res));
                } catch (Exception err) {
                    textBoxResults.Invoke((MethodInvoker)(() => textBoxResults.Text = err.Message));
                }
            });
            FinnishAlgorithmProccess();
            DrawPictureBox();
        }

        private async void BFSMatrix() {
            CancellationToken ct = StartAlgorithmProccess();
            await Task.Run(() => {
                try {
                    string name = textBoxFrom.Text;
                    string res = graph.BFSMatrix(name, ct);
                    textBoxResults.Invoke((MethodInvoker)(() => textBoxResults.Text = res));
                } catch (Exception err) {
                    textBoxResults.Invoke((MethodInvoker)(() => textBoxResults.Text = err.Message));
                }
            });
            FinnishAlgorithmProccess();
            DrawPictureBox();
        }

        private async void DFS() {
            CancellationToken ct = StartAlgorithmProccess();
            await Task.Run(() => {
                try {
                    string name = textBoxFrom.Text;
                    string res = graph.DFS(name, ct);
                    textBoxResults.Invoke((MethodInvoker)(() => textBoxResults.Text = res));
                } catch (Exception err) {
                    textBoxResults.Invoke((MethodInvoker)(() => textBoxResults.Text = err.Message));
                }
            });
            FinnishAlgorithmProccess();
            DrawPictureBox();
        }

        private async void DFSMatrix() {
            CancellationToken ct = StartAlgorithmProccess();
            await Task.Run(() => {
                try {
                    string name = textBoxFrom.Text;
                    string res = graph.DFSMatrix(name, ct);
                    textBoxResults.Invoke((MethodInvoker)(() => textBoxResults.Text = res));
                } catch (Exception err) {
                    textBoxResults.Invoke((MethodInvoker)(() => textBoxResults.Text = err.Message));
                }
            });
            FinnishAlgorithmProccess();
            DrawPictureBox();
        }

        private void Kruskal() {
            try {
                Graph newGraph = graph.Kruskal();
                ShowNewGraph(newGraph);
            } catch {
                ShowNewGraph(new Graph());
            }
        }

        private void Prima() {
            try {
                Graph newGraph = graph.Prima();
                ShowNewGraph(newGraph);
            } catch {
                ShowNewGraph(new Graph());
            }
        }
        #endregion

        #region Методы рисования
        private void ClearPictureBox() {
            g.Clear(Color.Cornsilk);
            pictureBox.Image = bm;
        }

        private void DrawPictureBox() {
            // Очищаем канвас
            ClearPictureBox();

            // Рисуем вершины
            DrawVertices(Color.Black);

            // Рисуем рёбра
            DrawEdges();
        }

        private void DrawVertex(Vertex vertex, Color color, int millisecond = 0) {
            SolidBrush brushVertex = new SolidBrush(color);
            Pen penVertex = new Pen(brushVertex);
            VertexDrawing vertexDrawing = vertex.VertexDrawing;
            vertexDrawing.Show(g, penVertex, brushVertex);
            pictureBox.Invoke((MethodInvoker)(() => pictureBox.Refresh()));
            Thread.Sleep(millisecond);
        }

        private void DrawVertices(Color color) {
            foreach (Vertex vertex in graph.Vertices.Values) {
                DrawVertex(vertex, color);
            }
        }

        private void DrawEdges() {
            Pen penEdge = new Pen(Color.Gray);
            AdjustableArrowCap arrow = new AdjustableArrowCap(10, 10);
            Font font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular);
            penEdge.CustomEndCap = arrow;
            foreach (Vertex vertex in graph.Vertices.Values) {
                foreach (Vertex neighbour in vertex.Neighbours.Values) {
                    string edgeWeight = vertex.EdgesVal[neighbour.Name].ToString();
                    if (vertex == neighbour) {
                        g.DrawString(
                            $"Петля {edgeWeight}", font, new SolidBrush(Color.Black),
                            vertex.VertexDrawing.Center.X - VertexDrawing.Size / 2,
                            vertex.VertexDrawing.Center.Y - VertexDrawing.Size
                        );
                        continue;
                    }

                    int radius = VertexDrawing.Size / 2;
                    PointF vertex1Center = new PointF(vertex.VertexDrawing.Center.X, vertex.VertexDrawing.Center.Y);
                    PointF vertex2Center = new PointF(neighbour.VertexDrawing.Center.X, neighbour.VertexDrawing.Center.Y);

                    PointF vector = new PointF(vertex1Center.X - vertex2Center.X, vertex1Center.Y - vertex2Center.Y);
                    float m = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
                    vector.X /= m;
                    vector.X *= radius;
                    vector.Y /= m;
                    vector.Y *= radius;

                    vertex2Center.X += vector.X;
                    vertex2Center.Y += vector.Y;
                    vertex1Center.X -= vector.X;
                    vertex1Center.Y -= vector.Y;

                    g.DrawLine(penEdge, vertex1Center, vertex2Center);
                    float arrowCenterX = (vertex1Center.X + vertex2Center.X) / 2;
                    float arrowCenterY = (vertex1Center.Y + vertex2Center.Y) / 2;
                    g.DrawString(edgeWeight, font, new SolidBrush(Color.Black), arrowCenterX, arrowCenterY);
                }
            }
            pictureBox.Refresh();
        }
        #endregion

        #region Работа с потоками(Threads)
        private void SetAllButtons(TableLayoutPanel tableLayoutPanel, bool enabled) {
            foreach (TableLayoutPanel table in tableLayoutPanel.Controls.OfType<TableLayoutPanel>()) {
                foreach (Button button in table.Controls.OfType<Button>()) {
                    button.Enabled = enabled;
                }
                if (table.Controls.OfType<TableLayoutPanel>().Count() > 0) {
                    SetAllButtons(table, enabled);
                }
            }
        }

        private CancellationToken StartAlgorithmProccess() {
            isAlgorithmRunning = true;
            tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;
            SetAllButtons(tableLayoutPanel1, false);
            buttonCancel.Enabled = true;
            comboBoxAlgos.Enabled = false;
            return ct;
        }

        private void FinnishAlgorithmProccess() {
            isAlgorithmRunning = false;
            SetAllButtons(tableLayoutPanel1, true);
            buttonCancel.Enabled = false;
            comboBoxAlgos.Enabled = true;
            tokenSource.Dispose();
        }
        #endregion

        #region Вспомогательные методы
        private void ShowNewGraph(Graph newGraph) {
            ShowGraphForm showGraphForm = new ShowGraphForm(newGraph);
            showGraphForm.Show();
            Graph.SetMethodsForDrawing(DrawVertex, DrawVertices);
        }

        private string RemoveEmptyLines(string text) {
            return Regex.Replace(text, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline).TrimEnd();
        }

        #endregion
    }
}
