using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsGraphs {
    public class Edge {
        public Vertex From;
        public Vertex To;
        public int Weight;
        public bool BothWays;

        public Edge(Vertex from, Vertex to, int weight, bool bothWays = false) {
            From = from;
            To = to;
            Weight = weight;
            BothWays = bothWays;
        }
    }
}
