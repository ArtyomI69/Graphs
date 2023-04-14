using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace WindowsFormsGraphs {
    public delegate void DrawVertexDelegate(Vertex vertex, Color color, int mileSeconds = 0);
    public delegate void DrawVerticesDelegate(Color color);
    public delegate void DrawEdgeDelegate(Vertex v1, Vertex v2, Color color, int mileSeconds = 0);
    public class Graph {
        #region Поля
        // Статические поля
        private static bool HaveDrawingMethods = false;
        private static DrawVertexDelegate DrawVertex;
        private static DrawVerticesDelegate DrawVertices;
        private static DrawEdgeDelegate DrawEdge;

        // Поля
        public Dictionary<string, Vertex> Vertices = new Dictionary<string, Vertex>();
        public List<Edge> Edges = new List<Edge>();
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
                if (edgeVal == 0) return;

                if (name1 != "" && name2 == "") name2 = name1;
                if (name2 != "" && name1 == "") name1 = name2;
                Vertex v1 = Vertices[name1];
                Vertex v2 = Vertices[name2];
                v1.AddEdge(v2, edgeVal);

                Edge newEdge = new Edge(v1, v2, edgeVal);
                bool haveEdge = Edges.FindIndex(edge => edge.From == v2 && edge.To == v1) != -1;
                if (haveEdge) {
                    Edge edge = Edges.Find(e => e.From == v2 && e.To == v1);
                    edge.BothWays = true;
                } else
                    Edges.Add(newEdge);
            } catch {
                throw new Exception("Данных вершин не существует или ребро между вершинами уже существует");
            }
        }

        // Добавление ребра в графе в обе стороны
        public void AddEdgeBothWays(string name1, string name2, int edgeVal) {
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

        #region Матрица смежности и инцидентности
        // Получение матрицы смежности
        public int[,] GetAdjMatrix() {
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

        // Получение матрицы инцидентности
        public int[,] GetIncidenceMatrix() {
            List<Vertex> vertices = GetVertexArr();
            int[,] res = new int[N, Edges.Count];
            int j = 0;
            foreach (Edge edge in Edges) {
                int from = vertices.FindIndex(e => e == edge.From);
                int to = vertices.FindIndex(e => e == edge.To);

                res[from, j] = edge.BothWays ? 1 : -1; ;
                res[to, j] = 1;
                j++;
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

                List<Vertex> vertices = GetVertexArr();
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
            List<Vertex> vertices = GetVertexArr();
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
            List<Vertex> vertices = GetVertexArr();
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

            HashSet<string> U = new HashSet<string>() { GetVertexArr()[0].Name }; // множество соединённых вершин
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

        #region Алгоритм Дейкстры
        public int[] Dijkstra(string name) {
            int[] res = new int[N];
            for (int i = 0; i < N; i++) res[i] = int.MaxValue;

            List<Vertex> vertices = GetVertexArr();
            int v = vertices.IndexOf(Vertices[name]); // стартовая вершина
            HashSet<int> visited = new HashSet<int>() { v }; // просмотренные вершины
            res[v] = 0; // нулевой вес для стартовой вершины

            while (v != -1) {
                foreach (int j in GetLinkV(v)) {
                    if (!visited.Contains(j)) {
                        int curVertex = res[v] + AdjMatrix[v, j];
                        res[j] = Math.Min(curVertex, res[j]);
                    }
                }

                v = MinVertex(res, visited);
                if (v != -1) visited.Add(v);
            }

            return res;
        }

        private IEnumerable<int> GetLinkV(int v) {
            for (int j = 0; j < N; j++) {
                if (AdjMatrix[v, j] != 0) yield return j;
            }
        }

        private int MinVertex(int[] vertices, HashSet<int> visited) {
            int minVertex = -1;
            int min = vertices.Max();
            for (int i = 0; i < N; i++) {
                int cur = vertices[i];
                if (cur < min && !visited.Contains(i)) {
                    min = cur;
                    minVertex = i;
                }
            }

            return minVertex;
        }
        #endregion

        #region Алгоритм Флойда
        public (int[,] dist, int[,] next) Floyd() {
            int[,] dist = AdjMatrix.Clone() as int[,];
            int[,] next = new int[N, N]; // список предыдущих вершин для поиска кратчайшего маршрута
            // Инициализируем массивы
            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    if (dist[i, j] == 0 && i != j) dist[i, j] = int.MaxValue;
                    next[i, j] = j;
                }
            }
            for (int i = 0; i < N; i++) {
                next[i, i] = i;
            }

            // Алгоритм Флойда
            for (int k = 0; k < N; k++) {
                for (int i = 0; i < N; i++) {
                    for (int j = 0; j < N; j++) {
                        int d = dist[i, k] + dist[k, j];
                        if (dist[i, j] > d && d >= 0) {
                            dist[i, j] = d;
                            next[i, j] = next[i, k]; // номер промежуточной вершины при движении от i к j
                        }
                    }
                }
            }

            return (dist, next);
        }

        public string GetPathFloyd(string from, string to) {
            (int[,] _, int[,] next) = Floyd();
            List<Vertex> vertices = GetVertexArr();
            int u = vertices.IndexOf(Vertices[to]);
            int v = vertices.IndexOf(Vertices[from]);
            if (next[u, v] == int.MaxValue) throw new Exception();
            string path = vertices[u].Name;
            int prev = u;
            while (u != v) {
                u = next[u, v];
                if (HaveDrawingMethods) {
                    DrawEdge(vertices[u], vertices[prev], Color.Red);
                    if (vertices[prev].Neighbours.ContainsValue(vertices[u])) DrawEdge(vertices[prev], vertices[u], Color.Red);
                }
                prev = u;
                path += vertices[u].Name;
            }

            char[] charArray = path.ToCharArray();
            Array.Reverse(charArray);
            path = new string(charArray);
            return path;
        }
        #endregion

        #region Алгоритм Беллмана-Форда
        public int[] BellmanFord(string name) {
            List<Vertex> vertices = GetVertexArr();
            int start = vertices.IndexOf(Vertices[name]); // стартовая вершина

            int[] distance = new int[N];

            for (int j = 0; j < N; j++) {
                distance[j] = int.MaxValue;
            }
            distance[start] = 0;

            for (int i = 0; i < N; i++) {
                foreach (Edge edge in Edges) {
                    int from = vertices.IndexOf(Vertices[edge.From.Name]);
                    int to = vertices.IndexOf(Vertices[edge.To.Name]);
                    int weight = edge.Weight;
                    if (distance[from] + weight < distance[to]) {
                        distance[to] = distance[from] + weight;
                    }
                }
            }

            foreach (Edge edge in Edges) {
                int from = vertices.IndexOf(Vertices[edge.From.Name]);
                int to = vertices.IndexOf(Vertices[edge.To.Name]);
                int weight = edge.Weight;
                if (distance[from] + weight < distance[to]) throw new Exception("В графе присутствуют цикл с негативным значением");
            }
            distance[start] = 0;

            return distance;
        }
        #endregion

        #region Компоненты сильной связанности
        public List<List<Vertex>> SCC() {
            Stack<Vertex> stack = new Stack<Vertex>();
            HashSet<Vertex> visited = new HashSet<Vertex>();
            foreach (Vertex vertex in Vertices.Values)
                if (!visited.Contains(vertex))
                    DFSUtil(vertex, visited, stack);

            Graph gr = new Graph();
            foreach (Vertex vertex in Vertices.Values) {
                gr.AddVertex(vertex.Name);
            }

            foreach (Vertex vertex in Vertices.Values) {
                foreach (Vertex neighbour in vertex.Neighbours.Values) {
                    gr.AddEdge(neighbour.Name, vertex.Name, vertex.EdgesVal[neighbour.Name]);
                }
            }

            List<List<Vertex>> scc = new List<List<Vertex>>();
            visited = new HashSet<Vertex>();
            while (stack.Count > 0) {
                Vertex v = stack.Pop();
                if (!visited.Contains(v)) {
                    Stack<Vertex> comp = new Stack<Vertex>();
                    gr.DFSUtil(v, visited, comp);
                    scc.Add(comp.ToList());
                }
            }

            if (HaveDrawingMethods) {
                Random rnd = new Random();
                foreach (List<Vertex> list in scc) {
                    Color randomColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                    foreach (Vertex v in list) {
                        DrawVertex(v, randomColor);
                    }
                }
            }

            return scc;
        }

        private void DFSUtil(Vertex v, HashSet<Vertex> visited, Stack<Vertex> comp) {
            visited.Add(v);
            comp.Push(v);
            foreach (Vertex neighbour in v.Neighbours.Values)
                if (!visited.Contains(neighbour))
                    DFSUtil(neighbour, visited, comp);
        }
        #endregion

        #region Эйлеров цикл
        public List<Vertex> FindEulerCycle(CancellationToken ct) {
            int[,] adjMatrix = new int[N, N];
            Array.Copy(AdjMatrix, adjMatrix, N * N);
            List<Vertex> res = new List<Vertex>();
            EulerCycle(0, adjMatrix, res);
            Vertex prev = null;

            foreach (Vertex v in res) {
                if (ct.IsCancellationRequested) throw new OperationCanceledException();

                if (prev != null) {
                    DrawEdge(prev, v, Color.Red);
                    DrawEdge(v, prev, Color.Red, 1000);
                }
                prev = v;
            }

            return res;
        }

        private void EulerCycle(int v, int[,] adjMatrix, List<Vertex> res) {
            List<Vertex> vertices = GetVertexArr();
            for (int j = 0; j < N; j++) {
                if (adjMatrix[v, j] != 0) {
                    adjMatrix[v, j] = 0;
                    adjMatrix[j, v] = 0;
                    EulerCycle(j, adjMatrix, res);
                }
            }
            res.Add(vertices[v]);
        }
        #endregion

        #region Эйлеров цикл алгоритм Флёри
        public List<Vertex> EulerCycleFleury(CancellationToken ct) {
            List<Vertex> res = new List<Vertex>();
            List<int> cycle = new List<int>();

            int[,] tmpGraph = new int[N, N];
            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    tmpGraph[i, j] = AdjMatrix[i, j];
                }
            }

            // проверяем, что граф связный и каждая вершина имеет четную степень
            if (!IsConnected() || !HasEvenDegrees(tmpGraph)) {
                return res;
            }

            // выбираем начальную вершину
            int start = 0;
            for (int i = 0; i < N; i++) {
                if (GetDegree(tmpGraph, i) % 2 == 1) {
                    start = i;
                    break;
                }
            }

            // выполняем алгоритм Флери
            Stack<int> stack = new Stack<int>();
            stack.Push(start);
            while (stack.Count > 0) {
                int vertex = stack.Peek();
                int degree = GetDegree(tmpGraph, vertex);

                if (degree == 0) {
                    // добавляем вершину в цикл и удаляем из стека
                    cycle.Add(vertex);
                    stack.Pop();
                } else {
                    // ищем соседнюю вершину и удаляем ребро
                    for (int i = 0; i < N; i++) {
                        if (tmpGraph[vertex, i] == 1) {
                            tmpGraph[vertex, i] = 0;
                            tmpGraph[i, vertex] = 0;
                            stack.Push(i);
                            break;
                        }
                    }
                }
            }

            // переворачиваем цикл и возвращаем его
            cycle.Reverse();

            // рисуем цикл в интерфейсе
            List<Vertex> vertices = GetVertexArr();
            Vertex prev = null;
            foreach (int v in cycle) {
                if (ct.IsCancellationRequested) throw new OperationCanceledException();

                res.Add(vertices[v]);
                if (prev != null) {
                    DrawEdge(prev, vertices[v], Color.Red);
                    DrawEdge(vertices[v], prev, Color.Red, 1000);
                }
                prev = vertices[v];
            }
            return res;
        }

        // метод для проверки, что граф связный
        private bool IsConnected() {
            bool[] visited = new bool[N];
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(0);
            visited[0] = true;

            while (queue.Count > 0) {
                int vertex = queue.Dequeue();

                for (int i = 0; i < N; i++) {
                    if (AdjMatrix[vertex, i] == 1 && !visited[i]) {
                        queue.Enqueue(i);
                        visited[i] = true;
                    }
                }
            }

            return Array.TrueForAll(visited, v => v == true);
        }

        // метод для проверки, что каждая вершина имеет четную степень
        private bool HasEvenDegrees(int[,] matrix) {
            for (int i = 0; i < N; i++) {
                if (GetDegree(matrix, i) % 2 == 1) {
                    return false;
                }
            }

            return true;
        }

        // метод для нахождения степени вершины
        private int GetDegree(int[,] matrix, int vertex) {
            int degree = 0;

            for (int i = 0; i < N; i++) {
                degree += matrix[vertex, i];
            }

            return degree;
        }
        #endregion

        #region Гамильонов цикл
        public List<Vertex> FindHamiltonianCycle() {
            List<Vertex> res = new List<Vertex>();
            int[] path = new int[N + 1];
            path[0] = 0;
            bool[] visited = new bool[N];
            visited[0] = true;
            HamiltonCycle(1, path, visited);

            List<Vertex> vertices = GetVertexArr();
            Vertex prev = null;
            foreach (int v in path) {
                res.Add(vertices[v]);
                if (prev != null) {
                    DrawEdge(prev, vertices[v], Color.Red);
                    DrawEdge(vertices[v], prev, Color.Red);
                }
                prev = vertices[v];
            }
            return res;
        }

        private void HamiltonCycle(int k, int[] path, bool[] visited) {
            int v = path[k - 1];
            for (int j = 0; j < N; j++) {
                if (AdjMatrix[v, j] != 0) {
                    if (k == N + 1 && j == 0) return;
                    else {
                        if (!visited[j]) {
                            path[k] = j;
                            visited[j] = true;
                            HamiltonCycle(k + 1, path, visited);
                            visited[j] = false;
                        }
                    }
                }
            }
        }
        #endregion

        #region Гамильтонов цикл алгебраическим методом
        public string[] HamiltonAlgebra() {
            string[,] p = new string[N, N];
            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    p[i, j] = AdjMatrix[i, j].ToString();
                }
            }

            for (int i = 0; i < N - 2; i++) {
                p = HamiltonMultiplication(p);
                p = HamiltonAlgebraUtil(p);
            }
            p = HamiltonMultiplication(p);

            List<Vertex> vertices = GetVertexArr();
            string[] res = new string[N];
            for (int i = 0; i < N; i++) {
                foreach (string el in p[i, i].Trim().Split(' ')) {
                    if (el.Length == N - 1) res[i] += $"{vertices[i].Name}{el}{vertices[i].Name} ";
                }
            }
            return res;
        }

        private string[,] HamiltonMultiplication(string[,] p) {
            List<Vertex> vertices = GetVertexArr();
            string[,] res = new string[N, N];

            string[,] B = new string[N, N];
            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    if (AdjMatrix[i, j] == 0) B[i, j] = "0";
                    else B[i, j] = vertices[j].Name;
                }
            }

            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    for (int k = 0; k < N; k++) {
                        if (B[i, k] != "0" && p[k, j] != "0")
                            if (p[k, j] != null && p[k, j].Trim() != "0") {
                                if (p[k, j] == "1") res[i, j] += $"{B[i, k]} ";
                                else {
                                    string tmp = "";
                                    foreach (string el in p[k, j].Trim().Split(' ')) {
                                        tmp += $"{B[i, k]}{el} ";
                                    }
                                    res[i, j] += $"{tmp} ";
                                }
                            } else {
                                if (p[k, j] == "1") res[i, j] = $"{B[i, k]} ";
                                else {
                                    string tmp = "";
                                    foreach (string el in p[k, j].Trim().Split(' ')) {
                                        tmp += $"{B[i, k]}{el} ";
                                    }
                                    res[i, j] = $"{tmp} ";
                                }
                            }
                    }
                    if (res[i, j] != null) res[i, j] = Regex.Replace(res[i, j], @"\s+", " ");
                }
            }

            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    if (res[i, j] == null) res[i, j] = "0";
                }
            }

            return res;
        }

        private string[,] HamiltonAlgebraUtil(string[,] p) {
            List<Vertex> vertices = GetVertexArr();
            string[,] res = new string[N, N];
            Array.Copy(p, res, N * N);

            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    string tmp = "";
                    foreach (string el in p[i, j].Trim().Split(' ')) {
                        string newEl = el;
                        if (el.Contains(vertices[i].Name) || el.Contains(vertices[j].Name) || el.Contains("0")) newEl = "";
                        tmp += $"{newEl} ";
                    }
                    res[i, j] = tmp;
                    if (res[i, j].Trim().Length == 0) res[i, j] = "0";
                }
            }
            return res;
        }
        #endregion

        #region Гамильтон Робертс-Флорес
        public List<string> HamiltonRobertsFlores(string from) {
            List<string> res = new List<string>();

            List<Vertex> vertices = GetVertexArr();
            string[,] matrix = new string[N, N];
            for (int i = 0; i < N; i++) {
                int k = 0;
                for (int j = 0; j < N; j++) {
                    matrix[i, j] = "0";
                    if (Math.Abs(AdjMatrix[i, j]) != 0) {
                        matrix[i, k] = vertices[j].Name;
                        k++;
                    }
                }
            }

            int idx = vertices.FindIndex(v => v.Name == from);
            List<string> path = new List<string>() { vertices[idx].Name };
            int node = path.Count;
            Dictionary<string, int> depth = vertices.ToDictionary(x => x.Name, x => 0);

            while (node > 0) {
                string vertex = path.Last();
                string nextNode = "0";
                if (depth[vertex] < N) {
                    nextNode = matrix[vertices.FindIndex(v => v.Name == vertex), depth[vertex]];
                    depth[vertex]++;
                }
                if (nextNode != "0" && !path.Contains(nextNode)) {
                    path.Add(nextNode);
                    node++;
                    depth[nextNode] = 0;
                    continue;
                } else if (path.Contains(nextNode)) {
                    continue;
                } else {
                    if (path.Count == N) {
                        res.Add(String.Join("", path));
                    }
                    path.RemoveAt(path.Count - 1);
                    node--;
                }
            }
            return res;
        }
        #endregion

        #region Алгоритм Флойда-Уоршелла
        public long[,] FloydWarshall() {
            long[,] dist = new long[N, N];
            int i, j, k;

            long[,] tmpAdjMatrix = new long[N, N];
            for (i = 0; i < N; i++) {
                for (j = 0; j < N; j++) {
                    if (i == j) continue;
                    tmpAdjMatrix[i, j] = AdjMatrix[i, j] != 0 ? AdjMatrix[i, j] : int.MaxValue;
                }
            }

            for (i = 0; i < N; i++) {
                for (j = 0; j < N; j++) {
                    dist[i, j] = tmpAdjMatrix[i, j];
                }
            }

            for (k = 0; k < N; k++) {
                for (i = 0; i < N; i++) {
                    for (j = 0; j < N; j++) {
                        if (dist[i, k] + dist[k, j]
                            < dist[i, j]) {
                            dist[i, j]
                                = dist[i, k] + dist[k, j];
                        }
                    }
                }
            }

            return dist;
        }
        #endregion

        #region Вспомогательные методы
        // Получить массив вершин
        public List<Vertex> GetVertexArr() {
            List<Vertex> vertices = Vertices.Values.ToList();
            return vertices;
        }

        private List<(int edgeVal, string name1, string name2)> _GetEdgesArr() {
            List<(int edgeVal, string name1, string name2)> res = new List<(int edgeVal, string name1, string name2)>();
            HashSet<(string name1, string name2)> visited = new HashSet<(string name1, string name2)>();
            List<Vertex> vertices = GetVertexArr();
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