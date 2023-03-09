using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsGraphs {
    public class Vertex {
        // Имя вершины
        public string Name;
        // Соседи вершины
        public Dictionary<string, Vertex> Neighbours = new Dictionary<string, Vertex>();
        // Величины рёбер с соседями
        public Dictionary<string, int> EdgesVal = new Dictionary<string, int>();
        // Вершина на канвасе
        public VertexDrawing VertexDrawing;

        // Конструктор
        public Vertex(string name) {
            Name = name;
        }

        // Добавить ребро
        public void AddEdge(Vertex v, int edgeVal) {
            try {
                edgeVal = Math.Abs(edgeVal);

                Neighbours.Add(v.Name, v);
                EdgesVal.Add(v.Name, edgeVal);
            } catch {
                throw new Exception();
            }
        }
    }
}
