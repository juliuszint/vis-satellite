using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace vissatellite
{
	public class SatelliteUniverse : GameWindow
	{
        private double elapsedSeconds;
        private ImageAssetData colorTexture;
        private ImageAssetData normalTexture;
        private MeshAssetData sphereMeshAsset;
        private CameraData cameraData;

        private BasicShaderAssetData basicShaderAsset;
        private BlinnShaderAsset blinnShader;
        private Vector3 ambientLightDirection;
        private SimData simulationData;


		public SatelliteUniverse(int width, int height) : base(
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
            this.sphereMeshAsset.AssetName = "vissatellite.meshes.sphere.obj";
            this.LoadMeshAsset(ref this.sphereMeshAsset);

            this.colorTexture = new ImageAssetData();
            this.colorTexture.AssetName = "vissatellite.textures.earth.jpg";
            this.LoadImageAsset(ref this.colorTexture);

            this.normalTexture = new ImageAssetData();
            this.normalTexture.AssetName = "vissatellite.textures.empty_normal.jpg";
            this.LoadImageAsset(ref this.normalTexture);

            this.basicShaderAsset = new BasicShaderAssetData();
            this.basicShaderAsset.VertexShaderName = "vissatellite.shader.Simple_VS.glsl";
            this.basicShaderAsset.FragmentShaderName = "vissatellite.shader.Simple_FS.glsl";
            this.LoadShaderAsset(ref this.basicShaderAsset);

            this.blinnShader = new BlinnShaderAsset();
            this.blinnShader.BasicShader.VertexShaderName = "vissatellite.shader.Blinn_VS.glsl";
            this.blinnShader.BasicShader.FragmentShaderName = "vissatellite.shader.Blinn_FS.glsl";
            this.LoadBlinnShaderAsset(ref this.blinnShader);

            var cameraPosition = new Vector3(0, 0, 20);
            this.cameraData.Transformation = Matrix4.LookAt(
                    cameraPosition,
                    new Vector3(0, 0, 0),
                    new Vector3(0, 1, 0));
            this.cameraData.Position = cameraPosition;

            this.ambientLightDirection = new Vector3(0, -1, 0);
            this.ambientLightDirection.Normalize();
            this.InitSimulationData();

            GL.Enable(EnableCap.DepthTest);
        }

        private void LoadImageAsset(ref ImageAssetData asset)
        {
            if (asset.IsLoaded) return;
            int textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);

            var width = 0;
            var height = 0;
            byte[] imageData = null;
            var currentIndex = 0;
            using(var stream = Utils.OpenEmbeddedResource(asset.AssetName)){
                using(var img = SixLabors.ImageSharp.Image.Load(stream)) {
                    imageData = new byte[img.Width * img.Height * 4];
                    width = img.Width;
                    height = img.Height;
                    for (int x = 0; x < img.Width; x++) {
                        for (int y = 0; y < img.Height; y++) {
                            var pixelValue = img[x, y];
                            imageData[currentIndex++] = pixelValue.B;
                            imageData[currentIndex++] = pixelValue.G;
                            imageData[currentIndex++] = pixelValue.R;
                            imageData[currentIndex++] = pixelValue.A;
                        }
                    }
                }
            }
            
            GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    width,
                    height,
                    0,
                    PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    imageData);

            GL.TexParameter(
                    TextureTarget.Texture2D,
                    TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.Nearest);
            GL.TexParameter(
                    TextureTarget.Texture2D,
                    TextureParameterName.TextureMagFilter,
                    (int)TextureMinFilter.Nearest);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            asset.OpenGLHandle = textureHandle;
            asset.IsLoaded = true;
        }

        private void LoadBlinnShaderAsset(ref BlinnShaderAsset shader)
        {
            this.LoadShaderAsset(ref shader.BasicShader);
            GL.BindAttribLocation(shader.BasicShader.ProgramHandle, VertexAttribIndex.Tangent, "in_tangent");
            GL.BindAttribLocation(shader.BasicShader.ProgramHandle, VertexAttribIndex.Bitangent, "in_bitangent");

            shader.ModelMatrixLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "model_matrix");
            shader.MaterialShininessLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "specular_shininess");
            shader.LightDirectionLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_direction");
            shader.LightAmbientColorLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_ambient_color");
            shader.LightDiffuseColorLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_diffuse_color");
            shader.LightSpecularColorLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "light_specular_color");
            shader.CameraPositionLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "camera_position");
            shader.ColorTextureLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "color_texture");
            shader.NormalTextureLocation = GL.GetUniformLocation(shader.BasicShader.ProgramHandle, "normalmap_texture");
        }

        private void UnloadBlinnShaderAsset(BlinnShaderAsset shader)
        {
            this.UnloadShaderAsset(shader.BasicShader);
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
            GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    interleaved.Length  * sizeof(float),
                    interleaved,
                    BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            meshAsset.VertexBufferHandle = vertexBufferHandle;

            int indexBufferHandle;
            GL.GenBuffers(1, out indexBufferHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
            GL.BufferData(
                    BufferTarget.ElementArrayBuffer,
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
            GL.VertexAttribPointer(
                    VertexAttribIndex.Vertex,
                    3,
                    VertexAttribPointerType.Float,
                    true,
                    stride,
                    0);

            GL.VertexAttribPointer(
                    VertexAttribIndex.Normal,
                    3,
                    VertexAttribPointerType.Float,
                    true,
                    stride,
                    Vector3.SizeInBytes);
            GL.VertexAttribPointer(
                    VertexAttribIndex.Uv,
                    2,
                    VertexAttribPointerType.Float,
                    true,
                    stride,
                    2 * Vector3.SizeInBytes);
            GL.VertexAttribPointer(
                    VertexAttribIndex.Tangent,
                    3,
                    VertexAttribPointerType.Float,
                    true,
                    stride,
                    2 * Vector3.SizeInBytes + Vector2.SizeInBytes);
            GL.VertexAttribPointer(
                    VertexAttribIndex.Bitangent,
                    3,
                    VertexAttribPointerType.Float,
                    true,
                    stride,
                    3 * Vector3.SizeInBytes + Vector2.SizeInBytes);
            GL.BindVertexArray(0);
            meshAsset.VertexArrayObjectHandle = vertexArrayObjectHandle;
            meshAsset.IsLoaded = true;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            this.elapsedSeconds += e.Time;
            this.DoSimulation(e.Time);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
#if JULIUS

            var fullRotationTime = 6;
            var modelMatrix = Matrix4.Identity;
            modelMatrix *= Matrix4.CreateScale(6.0f);
            var rotation = (float)(((this.elapsedSeconds % fullRotationTime) / fullRotationTime) * 2 * Math.PI);
            modelMatrix *= Matrix4.CreateRotationX(rotation);
            RenderWithBasicShader(ref this.sphereMeshAsset, ref this.colorTexture, modelMatrix);
            
//                RenderWithBlinn(
//                    ref this.sphereMeshAsset,
//                    ref this.colorTexture,
//                    ref this.normalTexture,
//                    new Vector3 (i * 2.5f, 0, 0));

#else
            for(int i = 0; i < this.simulationData.Satellites.Length; i++) {
                var satellite = this.simulationData.Satellites[i];
                var modelMatrix = Matrix4.Identity * Matrix4.CreateTranslation(satellite.Position);
                RenderWithBasicShader(ref this.sphereMeshAsset, ref this.colorTexture, modelMatrix);
            }
#endif
            this.SwapBuffers();
        }

        private void RenderWithBlinn(
            ref MeshAssetData mesh,
            ref ImageAssetData colorTexture,
            ref ImageAssetData normalTexture,
            Vector3 location)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, colorTexture.OpenGLHandle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, normalTexture.OpenGLHandle);

            GL.BindVertexArray(mesh.VertexArrayObjectHandle);
            GL.UseProgram(blinnShader.BasicShader.ProgramHandle);

            Matrix4 modelMatrix = Matrix4.Identity * Matrix4.CreateTranslation(location);
            var modelViewProjection = modelMatrix * this.cameraData.Transformation * this.cameraData.PerspectiveProjection;
            GL.UniformMatrix4(
                blinnShader.BasicShader.ModelviewProjectionMatrixLocation,
                false,
                ref modelViewProjection);

            GL.UniformMatrix4(blinnShader.ModelMatrixLocation, false, ref modelMatrix);
            GL.Uniform1(blinnShader.ColorTextureLocation, 0);
            GL.Uniform1(blinnShader.NormalTextureLocation, 1);
            GL.Uniform1(blinnShader.MaterialShininessLocation, 2.0f);
            GL.Uniform3(blinnShader.LightDirectionLocation, this.ambientLightDirection);

            GL.Uniform4(blinnShader.LightAmbientColorLocation, new Vector4(0.6f, 0.6f, 0.6f, 0));
            GL.Uniform4(blinnShader.LightDiffuseColorLocation, new Vector4(0.8f, 0.8f, 0.8f, 0));
            GL.Uniform4(blinnShader.LightSpecularColorLocation, new Vector4(0.0f, 0.0f, 0.0f, 0));
            GL.Uniform4(blinnShader.CameraPositionLocation, new Vector4(this.cameraData.Position, 1));

            GL.DrawElements(PrimitiveType.Triangles,
                            mesh.IndicesCount,
                            DrawElementsType.UnsignedInt,
                            IntPtr.Zero);
            GL.BindVertexArray(0);
            GL.ActiveTexture(TextureUnit.Texture0);

        }

        private void RenderWithBasicShader(
            ref MeshAssetData mesh,
            ref ImageAssetData texture,
            Matrix4 modelMatrix)
        {
            GL.BindTexture(TextureTarget.Texture2D, texture.OpenGLHandle);
            GL.BindVertexArray(mesh.VertexArrayObjectHandle);
            GL.UseProgram(basicShaderAsset.ProgramHandle);

            var modelViewProjection =
                modelMatrix * 
                this.cameraData.Transformation * 
                this.cameraData.PerspectiveProjection;

            GL.UniformMatrix4(
                basicShaderAsset.ModelviewProjectionMatrixLocation,
                false,
                ref modelViewProjection);

            GL.DrawElements(
                    PrimitiveType.Triangles,
                    mesh.IndicesCount,
                    DrawElementsType.UnsignedInt,
                    IntPtr.Zero);

            GL.BindVertexArray(0);
        }

        protected override void OnUnload(EventArgs e)
        {
            this.UnloadImageAsset(this.colorTexture);
            this.UnloadShaderAsset(this.basicShaderAsset);
            this.UnloadMeshData(this.sphereMeshAsset);
        }

        private void UnloadShaderAsset(BasicShaderAssetData shaderAsset)
        {
            if(shaderAsset.IsLoaded) {
                GL.DeleteProgram(shaderAsset.ProgramHandle);
                GL.DeleteShader(shaderAsset.FragmentObjectHandle);
                GL.DeleteShader(shaderAsset.VertexObjectHandle);
            }
            shaderAsset.IsLoaded = false;
        }

        private void UnloadImageAsset(ImageAssetData asset)
        {
            if(asset.IsLoaded) {
                GL.DeleteTexture(asset.OpenGLHandle);
            }
        }

        private void UnloadMeshData(MeshAssetData meshAsset)
        {
            if(meshAsset.IsLoaded) {
                GL.DeleteVertexArray(meshAsset.VertexArrayObjectHandle);
                GL.DeleteBuffer(meshAsset.VertexBufferHandle);
                GL.DeleteBuffer(meshAsset.IndicesBufferHandle);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            var fov = 60;
            GL.Viewport(0, 0, Width, Height);
            float aspectRatio = Width / (float)Height;
            Matrix4.CreatePerspectiveFieldOfView(
                    (float)(fov * Math.PI / 180.0f),
                    aspectRatio,
                    1,
                    100,
                    out this.cameraData.PerspectiveProjection);
        }

        //
        // simulation stuff
        //

        private void InitSimulationData() {
            this.simulationData = new SimData();
            this.simulationData.TotalSimulationTime = 0;
            this.simulationData.SimulationSpeed = 1.0f;
            this.simulationData.Satellites = new SatelliteSimData[10];
            for(int i = 0; i < 10; i++) {
                this.simulationData.Satellites[i] = new SatelliteSimData();
                this.simulationData.Satellites[i].Position = new Vector3(i, 0, 0);
            }
        }

        private void DoSimulation(double fTimeDelta) 
        {
            for(int i = 0; i < this.simulationData.Satellites.Length; i++) {
                var satellite = this.simulationData.Satellites[i];
            }
        }
	}
}
