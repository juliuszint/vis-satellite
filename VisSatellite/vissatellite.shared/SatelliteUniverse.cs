using System;
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
        private BasicShaderAssetData basicShaderAsset;

		public void Load()
		{            
            var shadingLanguageVersion = GL.GetString(StringName.ShadingLanguageVersion);
            var openGlVersion = GL.GetString(StringName.Version);

            this.sphereMeshAsset = new MeshAssetData() {
				AssetName = "vissatellite.mac.meshes.icosphere.obj"
            };
            this.LoadMeshData(ref this.sphereMeshAsset);

            this.basicShaderAsset = new BasicShaderAssetData {
                VertexShaderName = "vissatellite.mac.shader.Simple_VS.glsl",
                FragmentShaderName = "vissatellite.mac.shader.Simple_FS.glsl"
            };
            this.LoadShaderAsset(ref this.basicShaderAsset);
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

        private void LoadBlinnShaderAsset(ref BlinnShaderAsset shader)
        {
            this.LoadShaderAsset(ref shader.BasicShader);

            GL.BindAttribLocation(
                    shader.BasicShader.ProgramHandle,
                    VertexAttribIndex.Tangent,
                    "in_tangent");
            GL.BindAttribLocation(
                    shader.BasicShader.ProgramHandle,
                    VertexAttribIndex.Bitangent,
                    "in_bitangent");

            shader.ModelMatrixLocation = GL.GetUniformLocation(
                    shader.BasicShader.ProgramHandle,
                    "model_matrix");
            shader.MaterialShininessLocation = GL.GetUniformLocation(
                    shader.BasicShader.ProgramHandle,
                    "specular_shininess");
            shader.LightDirectionLocation = GL.GetUniformLocation(
                    shader.BasicShader.ProgramHandle,
                    "light_direction");
            shader.LightAmbientColorLocation = GL.GetUniformLocation(
                    shader.BasicShader.ProgramHandle,
                    "light_ambient_color");
            shader.LightDiffuseColorLocation = GL.GetUniformLocation(
                    shader.BasicShader.ProgramHandle,
                    "light_diffuse_color");
            shader.LightSpecularColorLocation = GL.GetUniformLocation(
                    shader.BasicShader.ProgramHandle,
                    "light_specular_color");
            shader.CameraPositionLocation = GL.GetUniformLocation(
                    shader.BasicShader.ProgramHandle,
                    "camera_position");
            shader.ColorTextureLocation = GL.GetUniformLocation(
                    shader.BasicShader.ProgramHandle, "color_texture");
            shader.NormalTextureLocation = GL.GetUniformLocation(
                    shader.BasicShader.ProgramHandle,
                    "normalmap_texture");
        }

        private void LoadShaderAsset(ref BasicShaderAssetData shaderAsset)
        {
            if (shaderAsset.IsLoaded)
                return;

            string vs = Utils.LoadFromEmbeddedResource(shaderAsset.VertexShaderName);
            string fs = Utils.LoadFromEmbeddedResource(shaderAsset.FragmentShaderName);

            int status_code;
            string info;

            int vertexObject = GL.CreateShader(ShaderType.VertexShader);
            int fragmentObject = GL.CreateShader(ShaderType.FragmentShader);
            shaderAsset.VertexObjectHandle = vertexObject;
            shaderAsset.FragmentObjectHandle = fragmentObject;

            GL.ShaderSource(vertexObject, vs);
            GL.CompileShader(vertexObject);
            GL.GetShaderInfoLog(vertexObject, out info);
            GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            GL.ShaderSource(fragmentObject, fs);
            GL.CompileShader(fragmentObject);
            GL.GetShaderInfoLog(fragmentObject, out info);
            GL.GetShader(fragmentObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            int program = GL.CreateProgram();
            GL.AttachShader(program, fragmentObject);
            GL.AttachShader(program, vertexObject);
            shaderAsset.ProgramHandle = program;

            GL.BindAttribLocation(program, VertexAttribIndex.Vertex, "in_position");
            GL.BindAttribLocation(program, VertexAttribIndex.Normal, "in_normal");
            GL.BindAttribLocation(program, VertexAttribIndex.Uv, "in_uv");

            GL.LinkProgram(program);

            shaderAsset.ModelviewProjectionMatrixLocation = GL.GetUniformLocation(
                program,
                "modelview_projection_matrix");
            Console.WriteLine($"modelview_projection_matrix: {shaderAsset.ModelviewProjectionMatrixLocation}");
            shaderAsset.IsLoaded = true;
        }

        private void UnloadBlinnShaderAsset(BlinnShaderAsset shader)
        {
            this.UnloadShaderAsset(shader.BasicShader);
        }

        private void UnloadShaderAsset(BasicShaderAssetData shaderAsset)
        {
            if(shaderAsset.IsLoaded) {
                //GL.DeleteProgram(shaderAsset.ProgramHandle);
                GL.DeleteShader(shaderAsset.FragmentObjectHandle);
                GL.DeleteShader(shaderAsset.VertexObjectHandle);
            }
            shaderAsset.IsLoaded = false;
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
            //GL.Color3(Color.MidnightBlue);
            //GL.Vertex2(-1.0f, 1.0f);
            //GL.Color3(Color.SpringGreen);
            //GL.Vertex2(0.0f, -1.0f);
            //GL.Color3(Color.Ivory);
            //GL.Vertex2(1.0f, 1.0f);
            GL.End();
		}
    }
}
