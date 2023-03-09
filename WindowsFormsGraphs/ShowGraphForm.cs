﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Drawing.Drawing2D;

namespace WindowsFormsGraphs {
    public partial class ShowGraphForm : Form {
        Graph graph;

        Bitmap bm;
        Graphics g;

        public ShowGraphForm(Graph graph) {
            this.graph = graph;
            InitializeComponent();
            InitializaeState();
            DrawPictureBox();
        }

        private void ShowGraphForm_Resize(object sender, EventArgs e) {
            InitializaeState();
            DrawPictureBox();
        }

        private void InitializaeState() {
            ConfigureBitmapSize();

            Graph.SetMethodsForDrawing(DrawVertex, DrawVertices);
        }

        private void ConfigureBitmapSize() {
            if (pictureBox.Width == 0 || pictureBox.Height == 0) return;
            bm = new Bitmap(pictureBox.Width, pictureBox.Height);
            g = Graphics.FromImage(bm);
            ClearPictureBox();
        }

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
            pictureBox.Refresh();
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
    }
}