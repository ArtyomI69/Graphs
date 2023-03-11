using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace WindowsFormsGraphs {
    public delegate void DrawVertexDelegate(Vertex vertex, Color color, int mileSeconds = 0);
    public delegate void DrawVerticesDelegate(Color color);
    public delegate void DrawEdgeDelegate(Vertex v1, Vertex v2, Color color);
    public class Graph {
        #region Поля
        // Статические поля
        private static bool HaveDrawingMethods = false;
        private static DrawVertexDelegate DrawVertex;
        private static DrawVerticesDelegate DrawVertices;
        private static DrawEdgeDelegate DrawEdge;

        // Поля
        public Dictionary<string, Vertex> Vertices = new Dictionary<string, Vertex>();
        public int[,] AdjMatrix => GetAdjMatrix();
        public int N => Vertices.Count;
        #endregion

        #region Статические поля и конструкторы
        // Добавляет графические методы для визуализации графа
        public static void SetMethodsForDrawing(DrawVertexDelegate drawVertex, DrawVerticesDelegate drawVertices, DrawEdgeDelegate drawEdge) {
            HaveDrawingMethods = true;
            DrawVertex = drawVertex;
            DrawVertices = drawVertices;
            DrawEdge = drawEdge;
        }

        // Конструктор графа
        public Graph() {
        }

        // Создание графа через матрицу смежности
        public Graph(int[,] matrix) {
            try {
                if (matrix.GetLength(0) != matrix.GetLength(1)) throw new Exception();

                int N = matrix.GetLength(0);
                for (int i = 0; i < N; i++) {
                    string name = Convert.ToChar(i + 65).ToString();
                    AddVertex(name);
                }

                for (int j = 0; j < N; j++) {
                    for (int i = 0; i < N; i++) {
                        string name = Convert.ToChar(j + 65).ToString();
                        string name2 = Convert.ToChar(i + 65).ToString();
                        AddEdge(name, name2, matrix[j, i]);
                    }
                }
            } catch {
                throw new Exception("Кол-во столбоцов и рядов в матрице должны быть одинкаовы");
            }
        }
        #endregion

        #region Операции над графом(добавление/удание)
        // Добавление вершины в графе
        public void AddVertex(string name) {
            try {
                if (name == "") return;
                Vertex newVertex = new Vertex(name);
                Vertices.Add(name, newVertex);
            } catch {
                throw new Exception("Вершина с данным именем уже существует");
            }
        }

        // Добавление вершины в графе
        public void AddVertex(Vertex newVertex) {
            try {
                Vertices.Add(newVertex.Name, newVertex);
            } catch {
                throw new Exception("Вершина с данным именем уже существует");
            }
        }

        // Удаление вершины в графе
        public void RemoveVertex(string name) {
            try {
                Vertex vertex = Vertices[name];
                Vertices.Remove(name);
                // Удаляем рёбра
                foreach (Vertex v in Vertices.Values) {
                    v.Neighbours.Remove(name);
                    v.EdgesVal.Remove(name);
                }
            } catch {
                throw new Exception("Вершина с данным именем не существует");
            }
        }

        // Добавление ребра в графе в одну сторону
        public void AddEdge(string name1, string name2, int edgeVal) {
            try {
                if (edgeVal <= 0) return;

                if (name1 != "" && name2 == "") name2 = name1;
                if (name2 != "" && name1 == "") name1 = name2;
                Vertex v1 = Vertices[name1];
                Vertex v2 = Vertices[name2];
                v1.AddEdge(v2, edgeVal);
            } catch {
                throw new Exception("Данных вершин не существует или ребро между вершинами уже существует");
            }
        }

        // Добавление ребра в графе в обе стороны
        public void AddEdgeBothWays(string name1, string name2, int edgeVal) {
            if (name1 != "" && name2 == "") name2 = name1;
            if (name2 != "" && name1 == "") name1 = name2;
            AddEdge(name1, name2, edgeVal);
            if (name1 != name2) AddEdge(name2, name1, edgeVal);
        }

        // Удаление ребра в графе
        public void RemoveEdge(string name1, string name2) {
            try {
                Vertex v1 = Vertices[name1];
                Vertex v2 = Vertices[name2];
                v1.Neighbours.Remove(name2);
                v1.EdgesVal.Remove(name2);
                v2.Neighbours.Remove(name1);
                v2.EdgesVal.Remove(name1);
            } catch {
                throw new Exception("Выбранной вершины не существует");
            }
        }
        #endregion

        #region Матрица смежности
        // Получение матрицы смежности
        public int[,] GetAdjMatrix() {
            int N = Vertices.Count;
            int[,] res = new int[N, N];
            int i = 0, j = 0;
            foreach (Vertex vertex in Vertices.Values) {
                foreach (string anotherVertexName in Vertices.Keys) {
                    if (vertex.Neighbours.ContainsKey(anotherVertexName)) {
                        res[i, j] = vertex.EdgesVal[anotherVertexName];
                    } else {
                        res[i, j] = 0;
                    }
                    j++;
                };
                j = 0;
                i++;
            }
            return res;
        }

        // Получение матрицы в виде строки
        public string GetAdjMatrixString() {
            string res = "";
            // Выводим в первый ряд все вершины
            res += "  ";
            foreach (string vertexName in Vertices.Keys) res += $"{vertexName} ";
            res += "\n";

            // Составляем матрицу смежности
            foreach (Vertex vertex in Vertices.Values) {
                res += $"{vertex.Name} ";
                foreach (string anotherVertexName in Vertices.Keys) {
                    if (vertex.Neighbours.ContainsKey(anotherVertexName)) {
                        res += $"{vertex.EdgesVal[anotherVertexName]} ";
                    } else {
                        res += "0 ";
                    }
                };
                res += "\n";
            }
            return res;
        }
        #endregion 

        #region DFS и BFS
        // Поиск в глубину в графе от какой либо вершины
        public string DFS(string name, CancellationToken ct = new CancellationToken()) {
            try {
                if (HaveDrawingMethods) DrawVertices(Color.White);
                return _DFS(Vertices[name], Vertices[name].Name.ToString(), new HashSet<Vertex>(), ct);
            } catch (OperationCanceledException) {
                return "";
            } catch {
                throw new Exception("Данной вершины не существует");
            }
        }

        // Поиск в глубину матрица
        public string DFSMatrix(string name, CancellationToken ct = new CancellationToken()) {
            try {
                if (HaveDrawingMethods) DrawVertices(Color.White);

                List<Vertex> vertices = _GetVertexArr();
                int start = vertices.FindIndex(vertex => vertex.Name == name);
                bool[] visited = new bool[vertices.Count];
                string res = "";
                return _DFSMatrix(start, res, visited, ct);
            } catch (OperationCanceledException) {
                return "";
            } catch {
                throw new Exception("Данной вершины не существует");
            }
        }

        // Поиск в ширину в графе от какой либо вершины
        public string BFS(string name, CancellationToken ct = new CancellationToken()) {
            try {
                if (HaveDrawingMethods) DrawVertices(Color.White);
                return _BFS(name, ct);
            } catch (OperationCanceledException) {
                return "";
            } catch {
                throw new Exception("Данной вершины не существует");
            }
        }

        // Поиск в ширину в графе от какой либо вершины, с помощью матрицы смежности
        public string BFSMatrix(string name, CancellationToken ct = new CancellationToken()) {
            try {
                if (HaveDrawingMethods) DrawVertices(Color.White);

                return _BFSMatrix(name, ct);
            } catch (OperationCanceledException) {
                return "";
            } catch {
                throw new Exception("Данной вершины не существует");
            }
        }

        // Рекурсивный поиск в глубину
        public string _DFS(Vertex v, string res, HashSet<Vertex> visited, CancellationToken ct = new CancellationToken()) {
            if (visited.Contains(v)) return res;
            visited.Add(v);

            if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested();


            if (HaveDrawingMethods) DrawVertex(v, Color.Gray, 1000);
            if (HaveDrawingMethods) DrawVertex(v, Color.Black);
            var neighbours = v.Neighbours;
            foreach (Vertex neighbor in neighbours.Values) {
                if (visited.Contains(neighbor)) continue;
                res += neighbor.Name;
                res = _DFS(neighbor, res, visited, ct);
            }

            return res;
        }

        // Поиск в глубину с помощью матрицы
        public string _DFSMatrix(int start, string res, bool[] visited, CancellationToken ct = new CancellationToken()) {
            List<Vertex> vertices = _GetVertexArr();
            res += vertices[start].Name;
            if (HaveDrawingMethods) DrawVertex(vertices[start], Color.Gray, 1000);
            if (HaveDrawingMethods) DrawVertex(vertices[start], Color.Black);
            visited[start] = true;

            if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested();

            for (int i = 0; i < AdjMatrix.GetLength(0); i++) {
                if (AdjMatrix[start, i] > 0 && (!visited[i])) {
                    res = _DFSMatrix(i, res, visited, ct);
                }
            }
            return res;
        }

        // Поиск в ширину
        private string _BFS(string name, CancellationToken ct = new CancellationToken()) {
            string res = "";
            HashSet<Vertex> visited = new HashSet<Vertex>();
            Vertex v = Vertices[name];

            Queue<Vertex> queue = new Queue<Vertex>();
            queue.Enqueue(v);
            while (queue.Count > 0) {
                Vertex vertex = queue.Dequeue();
                visited.Add(vertex);
                if (HaveDrawingMethods) DrawVertex(vertex, Color.Gray, 1000);
                if (HaveDrawingMethods) DrawVertex(vertex, Color.Black);
                res += vertex.Name;
                var neighbours = vertex.Neighbours;
                foreach (Vertex neighbor in neighbours.Values) {
                    if (!visited.Contains(neighbor) && !queue.Contains(neighbor)) queue.Enqueue(neighbor);
                }

                if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested();

            }
            return res;
        }

        // Поиск в ширину с помощью матрицы
        private string _BFSMatrix(string name, CancellationToken ct = new CancellationToken()) {
            string res = name;
            List<Vertex> vertices = _GetVertexArr();
            int start = vertices.FindIndex(vertex => vertex.Name == name);
            if (start == -1) throw new Exception();

            bool[] visited = new bool[vertices.Count];
            List<int> q = new List<int>();
            q.Add(start);
            visited[start] = true;

            int vis;
            while (q.Count != 0) {
                vis = q[0];
                q.Remove(q[0]);
                Vertex vertex = vertices[vis];
                if (HaveDrawingMethods) DrawVertex(vertex, Color.Gray, 1000);
                if (HaveDrawingMethods) DrawVertex(vertex, Color.Black);
                for (int i = 0; i < vertices.Count; i++) {
                    if (AdjMatrix[vis, i] > 0 && (!visited[i])) {
                        q.Add(i);

                        res += vertices[i].Name;
                        visited[i] = true;
                    }
                }
                if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested();
            }
            return res;
        }
        #endregion

        #region Алгоритм Краскала и Прима
        // Алгоритм Краскала
        public Graph Kruskal() {
            List<(int edgeVal, string name1, string name2)> edges = _GetEdgesArr(); // список рёбер острова

            Comparison<(int edgeVal, string name1, string name2)> comparison = (edge1, edge2) => edge1.edgeVal.CompareTo(edge2.edgeVal);
            edges.Sort(comparison);
            HashSet<string> U = new HashSet<string>(); // список СОЕДИНЁННЫХ вершин
            Dictionary<string, List<string>> D = new Dictionary<string, List<string>>(); // словарь списка изолированных групп вершин
            List<(int edgeVal, string name1, string name2)> T = new List<(int edgeVal, string name1, string name2)>(); // список рёбер острова

            // проходим по рёбрам и соеденяем ИЗОЛИРОВАННЫЕ вершины
            foreach ((int edgeVal, string name1, string name2) in edges) {
                if (!U.Contains(name1) || !U.Contains(name2)) {
                    if (!U.Contains(name1) && !U.Contains(name2)) {
                        D[name1] = new List<string>() { name1, name2 };
                        D[name2] = D[name1];
                    } else {
                        if (!D.ContainsKey(name1)) {
                            D[name2].Add(name1);
                            D[name1] = D[name2];
                        } else {
                            D[name1].Add(name2);
                            D[name2] = D[name1];
                        }
                    }

                    T.Add((edgeVal, name1, name2));
                    U.Add(name1);
                    U.Add(name2);
                }
            }

            // проходим по рёбрам второй раз и объеденяем разрозненные группы вершин
            foreach ((int edgeVal, string name1, string name2) in edges) {
                if (!D[name1].Contains(name2)) {
                    T.Add((edgeVal, name1, name2));
                    List<string> tmp = new List<string>(D[name1]);
                    D[name1].AddRange(D[name2]);
                    D[name2].AddRange(tmp);
                }
            }

            // Создаём граф
            Graph res = CreateGraphFromEdgesArr(T);
            return res;
        }

        // Алгоритм Прима
        public Graph Prima() {
            List<(int edgeVal, string name1, string name2)> edges = _GetEdgesArr();
            edges.Insert(0, (int.MaxValue, "", ""));

            HashSet<string> U = new HashSet<string>() { _GetVertexArr()[0].Name }; // множество соединённых вершин
            List<(int edgeVal, string name1, string name2)> T = new List<(int edgeVal, string name1, string name2)>(); // список рёбер острова

            while (U.Count < N) {
                (int edgeVal, string name1, string name2) minEdge = GetMinEdgePrima(edges, U);
                if (minEdge.edgeVal == int.MaxValue) break;

                T.Add(minEdge);
                U.Add(minEdge.name1);
                U.Add(minEdge.name2);
            }

            // Создаём граф
            Graph res = CreateGraphFromEdgesArr(T);
            return res;
        }

        // Получить ребро с наименьшим весом
        private (int edgeVal, string name1, string name2) GetMinEdgePrima(List<(int edgeVal, string name1, string name2)> edges, HashSet<string> U) {
            (int edgeVal, string name1, string name2) minEdge = (int.MaxValue, "", "");
            foreach (string name in U) {
                var curEdge = edges[0];
                foreach ((int edgeVal, string name1, string name2) in edges) {
                    if ((name == name1 || name == name2) && (!U.Contains(name1) || !U.Contains(name2))) {
                        if (curEdge.edgeVal > edgeVal) curEdge = (edgeVal, name1, name2);
                    }
                }
                if (minEdge.edgeVal > curEdge.edgeVal) minEdge = curEdge;
            }
            return minEdge;
        }

        private Graph CreateGraphFromEdgesArr(List<(int edgeVal, string name1, string name2)> edgesArr) {
            Graph res = new Graph();
            foreach ((int edgeVal, string name1, string name2) in edgesArr) {
                if (!res.Vertices.ContainsKey(name1)) {
                    res.AddVertex(name1);
                    res.Vertices[name1].VertexDrawing = Vertices[name1].VertexDrawing;
                }
                if (!res.Vertices.ContainsKey(name2)) {
                    res.AddVertex(name2);
                    res.Vertices[name2].VertexDrawing = Vertices[name2].VertexDrawing;
                }
                if (HaveDrawingMethods) {
                    DrawEdge(Vertices[name1], Vertices[name2], Color.Red);
                    DrawEdge(Vertices[name2], Vertices[name1], Color.Red);
                }
                res.AddEdgeBothWays(name1, name2, edgeVal);
            }

            return res;
        }
        #endregion

        #region Вспомогательные методы
        // Получить массив вершин
        private List<Vertex> _GetVertexArr() {
            List<Vertex> vertices = Vertices.Values.ToList();
            return vertices;
        }

        private List<(int edgeVal, string name1, string name2)> _GetEdgesArr() {
            List<(int edgeVal, string name1, string name2)> res = new List<(int edgeVal, string name1, string name2)>();
            HashSet<(string name1, string name2)> visited = new HashSet<(string name1, string name2)>();
            List<Vertex> vertices = _GetVertexArr();
            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    string name1 = vertices[i].Name;
                    string name2 = vertices[j].Name;
                    int edgeVal = AdjMatrix[i, j];
                    if (edgeVal == 0) continue;
                    if (visited.Contains((name1, name2))) continue;

                    visited.Add((name2, name1));
                    res.Add((edgeVal, name1, name2));
                }
            }
            return res;
        }
        #endregion
    }
}