using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsGraphs {
    public class VertexDrawing {
        public static int Size = 45;
        public PointF Center;
        public string Name;

        public VertexDrawing(int x, int y, string vertexName) {
            Center = new PointF(x, y);
            Name = vertexName;
        }

        public void Move(int a, int b) {
            Center.X += a;
            Center.Y += b;
        }

        public void Show(Graphics g, Pen pen, SolidBrush brush) {
            Rectangle rectangle = new Rectangle((int)Center.X - Size / 2, (int)Center.Y - Size / 2, Size, Size);
            g.DrawEllipse(pen, rectangle);
            Font font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold);
            g.FillEllipse(brush, rectangle);
            if (brush.Color == Color.Cornsilk) return;
            if (brush.Color == Color.White)
                g.DrawString(Name, font, new SolidBrush(Color.Black), Center.X - Size / 6, Center.Y - Size / 6);
            else
                g.DrawString(Name, font, new SolidBrush(Color.White), Center.X - Size / 6, Center.Y - Size / 6);

        }

        public Rectangle RegionCapture() {
            Rectangle rectangle = new Rectangle((int)Center.X - Size / 2, (int)Center.Y - Size / 2, Size, Size);
            return rectangle;
        }
    }
}
