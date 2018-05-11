using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace vissatellite.shared
{
	public struct Vector3i
    {
        public int X;
        public int Y;
    }

    public class ObjectVertexData
    {
        public int[] Indices;
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] UVs;
        public Vector3[] Tangents;
        public Vector3[] BiTangents;
    }

    public static class VertexAttribIndex
    {
        public const int Vertex = 0;
        public const int Normal = 1;
        public const int Uv = 2;
        public const int Tangent = 3;
        public const int Bitangent = 4;
    }

    public struct ImageAssetData
    {
        public string AssetName;
        public bool IsLoaded;
        public int OpenGLHandle;
        public bool IsDisplacement;
    }

    public struct MeshAssetData
    {
        public string AssetName;
        public bool IsLoaded;

        public int IndicesCount;
        public int VertexBufferHandle;
        public int IndicesBufferHandle;
        public int VertexArrayObjectHandle;
    }

    public struct BasicShaderAssetData
    {
        public bool IsLoaded;
        public string FragmentShaderName;
        public string VertexShaderName;

        public int VertexObjectHandle;
        public int FragmentObjectHandle;
        public int ProgramHandle;
        public int ModelviewProjectionMatrixLocation;
    }

    public struct BlinnShaderAsset
    {
        public BasicShaderAssetData BasicShader;
        public int ModelMatrixLocation;
        public int ColorTextureLocation;
        public int NormalTextureLocation;
        public int MaterialShininessLocation;
        public int LightDirectionLocation;
        public int LightAmbientColorLocation;
        public int LightDiffuseColorLocation;
        public int LightSpecularColorLocation;
        public int CameraPositionLocation;
    }

    public struct CameraData
    {
        public float XAngle;
        public float YAngle;
        public Vector3 Position;

        public Matrix4 Transformation;
        public Matrix4 PerspectiveProjection;
    }

	public class SatelliteUniverse
    {   
        private MeshAssetData sphereMeshAsset;

		public void Load()
		{            
            this.sphereMeshAsset = new MeshAssetData() {
				AssetName = "vissatellite.mac.meshes.icosphere.obj"
            };
            this.LoadMeshData(ref this.sphereMeshAsset);
		}

        private void LoadMeshData(ref MeshAssetData meshAsset)
        {
            if (meshAsset.IsLoaded)
                return;

            var planeMesh = Wavefront.Load(meshAsset.AssetName);
            //PushMeshToGPUBuffer(planeMesh, ref meshAsset);
            meshAsset.IndicesCount = planeMesh.Indices.Length;
            meshAsset.IsLoaded = true;
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
