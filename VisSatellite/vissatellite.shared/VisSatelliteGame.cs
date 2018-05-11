using System.Drawing;
using OpenTK.Graphics.OpenGL;

namespace vissatellite.shared
{
    public class VisSatteliteGame
    {
		public void Load()
		{
			
		}

		public void Render()
		{
			// Setup buffer
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.MatrixMode(MatrixMode.Projection);

            // Draw a simple triangle
            GL.LoadIdentity();
            GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
            GL.Begin(BeginMode.Triangles);
            GL.Color3(Color.MidnightBlue);
            GL.Vertex2(-1.0f, 1.0f);
            GL.Color3(Color.SpringGreen);
            GL.Vertex2(0.0f, -1.0f);
            GL.Color3(Color.Ivory);
            GL.Vertex2(1.0f, 1.0f);
            GL.End();
		}
    }
}
