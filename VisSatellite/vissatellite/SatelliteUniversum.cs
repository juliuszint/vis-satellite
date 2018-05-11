using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace vissatellite
{
	public class SatelliteUniversum : GameWindow
	{
        private MeshAssetData sphereMeshAsset;
        private BasicShaderAssetData basicShaderAsset;

		public SatelliteUniversum(int width, int height) : base(
                width,
                height,
                GraphicsMode.Default,
                "Satellite Universum",
                0,
                DisplayDevice.Default, 3, 3, GraphicsContextFlags.ForwardCompatible)
		{ }

        protected override void OnLoad(EventArgs e)
        {
            var openGlVersion = GL.GetString(StringName.Version);
            var openGlShaderLanguageVersion = GL.GetString(StringName.ShadingLanguageVersion);
            Console.WriteLine($"OpenGL Version: {openGlVersion}");
            Console.WriteLine($"OpenGL Shader Language Version: {openGlShaderLanguageVersion}");

            this.sphereMeshAsset = new MeshAssetData();
            this.sphereMeshAsset.AssetName = "vissatellite.meshes.icosphere.obj";
            this.LoadMeshAsset(ref this.sphereMeshAsset);

            this.basicShaderAsset = new BasicShaderAssetData();
            this.basicShaderAsset.VertexShaderName = "vissatellite.shader.Simple_FS.glsl";
            this.basicShaderAsset.FragmentShaderName = "vissatellite.shader.Simple_VS.glsl";
            this.LoadShaderAsset(ref this.basicShaderAsset);
        }

        private void LoadShaderAsset(ref BasicShaderAssetData shaderAsset)
        {
            if (shaderAsset.IsLoaded)
                return;

            string vs = Utils.LoadEmbeddedResourceAsString(shaderAsset.VertexShaderName);
            string fs = Utils.LoadEmbeddedResourceAsString(shaderAsset.FragmentShaderName);

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

        private void LoadMeshAsset(ref MeshAssetData meshAsset)
        {
            if (meshAsset.IsLoaded)
                return;

            var planeMesh = Wavefront.Load(meshAsset.AssetName);
            meshAsset.IndicesCount = planeMesh.Indices.Length;

            // load geometry into gpu memory
            int strideCount = 14;
            var interleaved = new float[strideCount * planeMesh.Vertices.Length];
            var interleavedIndex = 0;
            for (int i = 0; i < planeMesh.Vertices.Length; i++) {
                interleavedIndex = i * strideCount;
                interleaved[interleavedIndex++] = planeMesh.Vertices[i].X;
                interleaved[interleavedIndex++] = planeMesh.Vertices[i].Y;
                interleaved[interleavedIndex++] = planeMesh.Vertices[i].Z;

                interleaved[interleavedIndex++] = planeMesh.Normals[i].X;
                interleaved[interleavedIndex++] = planeMesh.Normals[i].Y;
                interleaved[interleavedIndex++] = planeMesh.Normals[i].Z;

                interleaved[interleavedIndex++] = planeMesh.UVs[i].X;
                interleaved[interleavedIndex++] = planeMesh.UVs[i].Y;

                interleaved[interleavedIndex++] = planeMesh.Tangents[i].X;
                interleaved[interleavedIndex++] = planeMesh.Tangents[i].Y;
                interleaved[interleavedIndex++] = planeMesh.Tangents[i].Z;

                interleaved[interleavedIndex++] = planeMesh.BiTangents[i].X;
                interleaved[interleavedIndex++] = planeMesh.BiTangents[i].Y;
                interleaved[interleavedIndex++] = planeMesh.BiTangents[i].Z;
            }

            int vertexBufferHandle;
            GL.GenBuffers(1, out vertexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          interleaved.Length  * sizeof(float),
                          interleaved,
                          BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            meshAsset.VertexBufferHandle = vertexBufferHandle;

            int indexBufferHandle;
            GL.GenBuffers(1, out indexBufferHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                          sizeof(uint) * planeMesh.Indices.Length,
                          planeMesh.Indices,
                          BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            meshAsset.IndicesBufferHandle = indexBufferHandle;

            int vertexArrayObjectHandle;
            GL.GenVertexArrays(1, out vertexArrayObjectHandle);
            GL.BindVertexArray(vertexArrayObjectHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);
            GL.EnableVertexAttribArray(4);

            var stride = (4 * Vector3.SizeInBytes) + Vector2.SizeInBytes;
            GL.VertexAttribPointer(VertexAttribIndex.Vertex,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   0);

            GL.VertexAttribPointer(VertexAttribIndex.Normal,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   Vector3.SizeInBytes);
            GL.VertexAttribPointer(VertexAttribIndex.Uv,
                                   2,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   2 * Vector3.SizeInBytes);
            GL.VertexAttribPointer(VertexAttribIndex.Tangent,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   2 * Vector3.SizeInBytes + Vector2.SizeInBytes);
            GL.VertexAttribPointer(VertexAttribIndex.Bitangent,
                                   3,
                                   VertexAttribPointerType.Float,
                                   true,
                                   stride,
                                   3 * Vector3.SizeInBytes + Vector2.SizeInBytes);
            GL.BindVertexArray(0);
            meshAsset.VertexArrayObjectHandle = vertexArrayObjectHandle;
            meshAsset.IsLoaded = true;
        }

        protected override void OnUnload(EventArgs e)
        {
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            this.SwapBuffers();
        }
	}
}
