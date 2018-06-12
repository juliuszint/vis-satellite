using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace vissatellite
{
	public class SatelliteUniverse : GameWindow
	{
        private bool quit;
        private double elapsedSeconds;
	    private double simulationSizeScalar;
        private float satelliteSizeScale = 0.3f;

        private ImageAssetData earthColorTexture;
        private ImageAssetData satelliteTexture;
        private ImageAssetData satelliteTextureSelected;
        private ImageAssetData emptyNormalTexture;
        private MeshAssetData sphereMeshAsset;
        private MeshAssetData satelliteMeshAsset;
        private CameraData cameraData;

        private BlinnShaderAsset blinnShader;
        private Vector3 ambientLightDirection;
        private SimData simulationData;
        private KeyboardInput keyboardInput;
        private Task consoleTask;

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
            this.consoleTask = Task.Run((Action)this.ProcessConsoleInput);
            Console.WriteLine($"OpenGL Version: {openGlVersion}");
            Console.WriteLine($"OpenGL Shader Language Version: {openGlShaderLanguageVersion}");

            this.simulationSizeScalar = 0.001f;

            this.sphereMeshAsset = new MeshAssetData();
            this.sphereMeshAsset.AssetName = "vissatellite.meshes.sphere.obj";
            this.LoadMeshAsset(ref this.sphereMeshAsset);

            this.satelliteMeshAsset = new MeshAssetData();
            this.satelliteMeshAsset.AssetName = "vissatellite.meshes.satellite.obj";
            this.LoadMeshAsset(ref this.satelliteMeshAsset);

            this.earthColorTexture = new ImageAssetData();
            this.earthColorTexture.AssetName = "vissatellite.textures.earth.jpg";
            this.LoadImageAsset(ref this.earthColorTexture);

            this.satelliteTexture = new ImageAssetData();
            this.satelliteTexture.AssetName = "vissatellite.textures.satellite_texture.jpg";
            this.LoadImageAsset(ref this.satelliteTexture);

            this.satelliteTextureSelected = new ImageAssetData();
            this.satelliteTextureSelected.AssetName = "vissatellite.textures.satellite_texture_selected.jpg";
            this.LoadImageAsset(ref this.satelliteTextureSelected);

            this.emptyNormalTexture = new ImageAssetData();
            this.emptyNormalTexture.AssetName = "vissatellite.textures.empty_normal.jpg";
            this.LoadImageAsset(ref this.emptyNormalTexture);

            this.blinnShader = new BlinnShaderAsset();
            this.blinnShader.BasicShader.VertexShaderName = "vissatellite.shader.Blinn_VS.glsl";
            this.blinnShader.BasicShader.FragmentShaderName = "vissatellite.shader.Blinn_FS.glsl";
            this.LoadBlinnShaderAsset(ref this.blinnShader);

            this.cameraData.Eye = new Vector3(0, 0, 20);
            this.cameraData.Up = new Vector3(0, 1, 0);
            this.cameraData.Direction = new Vector3(0, 0, -1);
            this.cameraData.zNear = 0.1f;
            this.cameraData.zFar = 500.0f;
            UpdateCameraTransformation(ref this.cameraData);

            this.ambientLightDirection = new Vector3(-1, 0, 0);
            this.ambientLightDirection.Normalize();
            this.InitSimulationData();

            this.keyboardInput = new KeyboardInput();
            Console.WriteLine();
            Console.WriteLine("Befehle");
            Console.WriteLine("simtime time      setzen der simulationsgeschwindigkeit");
            Console.WriteLine("filter all        zeigt alle Satelliten an");
            Console.WriteLine("       none       zeigt keinen Satelliten an");
            Console.WriteLine("       iridium    zeigt Iridium Satelliten an");
            Console.WriteLine("       civ        zeigt zivile Satelliten an");
            Console.WriteLine("       com        zeigt kommerzielle Satelliten an");
            Console.WriteLine("       mil        zeigt Milit�r-Satelliten an");
            Console.WriteLine("       gov        zeigt Regierungs-Satelliten an");

            Console.WriteLine();
            Console.Write("> ");
            //Console.WriteLine("camera axis       setzen der camera ansicht");

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
                    for (int y = 0; y < img.Height; y++) {
                        for (int x = 0; x < img.Width; x++) {
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
            meshAsset.xMin = planeMesh.xMin;
            meshAsset.xMax = planeMesh.xMax;
            meshAsset.yMin = planeMesh.yMin;
            meshAsset.yMax = planeMesh.yMax;
            var xMax = Math.Max(Math.Abs(meshAsset.xMin), Math.Abs(meshAsset.xMax));
            var yMax = Math.Max(Math.Abs(meshAsset.yMin), Math.Abs(meshAsset.yMax));
            meshAsset.OverallMaximum = Math.Max(xMax, yMax);

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

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (this.quit) {
                this.Exit();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            this.elapsedSeconds += e.Time;
            this.MoveCamera(ref this.cameraData, (float)e.Time, 6.0f, 0.3f);
            this.DoSimulation(e.Time);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
#if JULIUS
            var fullRotationTime = 6;
            var rotationAngleRad = (float)(((this.elapsedSeconds % fullRotationTime) / fullRotationTime) * 2 * Math.PI);

            var satelliteMatrix =
                Matrix4.Identity *
                Matrix4.CreateScale(this.satelliteSizeScale) *
                Matrix4.CreateTranslation(this.earthPosition); // *
                //Matrix4.CreateRotationY(rotationAngleRad);
            RenderWithBlinn(
                ref this.satelliteMeshAsset,
                ref this.satelliteTexture,
                ref this.emptyNormalTexture,
                satelliteMatrix);

            /*
            var earthMatrix =
                Matrix4.Identity *
                Matrix4.CreateRotationY(rotationAngleRad);

            RenderWithBlinn(
                ref this.sphereMeshAsset,
                ref this.earthColorTexture,
                ref this.emptyNormalTexture,
                earthMatrix);
                */
#else


            var earthMatrix =
                Matrix4.Identity *
                Matrix4.CreateRotationY(simulationData.CurrentEarthRotation) *
                Matrix4.CreateScale((float)simulationData.RealEarthDiameter * (float)simulationSizeScalar);

            RenderWithBlinn(
                ref this.sphereMeshAsset,
                ref this.earthColorTexture,
                ref this.emptyNormalTexture,
                earthMatrix);

            for(int i = 0; i < this.simulationData.Satellites.Length; i++) {
                var satellite = this.simulationData.Satellites[i];
                if (satellite.IsVisible) {
                    var selectedScale = satellite.IsSelected ? 5.0f : 1.0f;
                    var satelliteMatrix =
                        Matrix4.Identity *
                        Matrix4.CreateScale(this.satelliteSizeScale) *
                        Matrix4.CreateTranslation(satellite.Position);

                    var texture = satellite.IsSelected 
                        ? this.satelliteTextureSelected
                        : this.satelliteTexture;

                    RenderWithBlinn(
                        ref this.satelliteMeshAsset,
                        ref texture,
                        ref this.emptyNormalTexture,
                        satelliteMatrix);
                }
            }
#endif
            this.SwapBuffers();
        }

        private void RenderWithBlinn(
            ref MeshAssetData mesh,
            ref ImageAssetData colorTexture,
            ref ImageAssetData normalTexture,
            Matrix4 modelMatrix)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, colorTexture.OpenGLHandle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, normalTexture.OpenGLHandle);

            GL.BindVertexArray(mesh.VertexArrayObjectHandle);
            GL.UseProgram(blinnShader.BasicShader.ProgramHandle);

            var modelViewProjection =
                modelMatrix *
                this.cameraData.Transformation *
                this.cameraData.PerspectiveProjection;

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
            GL.Uniform4(blinnShader.CameraPositionLocation, new Vector4(this.cameraData.Eye, 1));

            GL.DrawElements(
                PrimitiveType.Triangles,
                mesh.IndicesCount,
                DrawElementsType.UnsignedInt,
                IntPtr.Zero);

            GL.BindVertexArray(0);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var mouseX = ((e.X / (float)this.cameraData.ViewportWidth) * 2.0f) - 1.0f;
            var mouseY = 1.0f - ((e.Y / (float)this.cameraData.ViewportHeight) * 2.0f);
            var mouseVector = new Vector4(mouseX, mouseY, 0, 1.0f);

            var inverseTransformation = Matrix4.Invert(this.cameraData.Transformation);
            var inverseProjection = Matrix4.Invert(this.cameraData.PerspectiveProjection);
            mouseVector = mouseVector * inverseProjection;
            mouseVector = mouseVector * inverseTransformation;

            if (mouseVector.W > float.Epsilon || mouseVector.W < float.Epsilon) {
                mouseVector.X /= mouseVector.W;
                mouseVector.Y /= mouseVector.W;
                mouseVector.Z /= mouseVector.W;
            }

            var mouseRay = mouseVector.Xyz - this.cameraData.Eye;
            mouseRay.Normalize();

#if true
            var satelliteRadius = this.satelliteMeshAsset.OverallMaximum * this.satelliteSizeScale;
            var minHitDistance = float.PositiveInfinity;
            var minHitIndex = -1;
            var hitCounter = 0;
            // Note: performance optimierungen möglich
            // für debugzwecke wird der hittest erstmal für jeden satelliten ausgeführt
            for(int i = 0; i < this.simulationData.Satellites.Length; i++) {
                var satellite = this.simulationData.Satellites[i];
                satellite.IsSelected = false;
                var sPosition = satellite.Position;
                var distance = CalculateDistancePointLine(
                    this.cameraData.Eye,
                    mouseRay,
                    sPosition);

                if(minHitDistance > distance) {
                    minHitDistance = distance;
                    hitCounter++;
                    minHitIndex = i;
                }
            }
//            Console.WriteLine($"SatelliteBoundingRadius: {satelliteRadius}, distance: {minHitDistance}");
//            Console.WriteLine($"Hitcounter: {hitCounter}");
            if(minHitIndex > 0) {
                var selectedSatellite = this.simulationData.Satellites[minHitIndex];
                selectedSatellite.IsSelected = true;
                Console.WriteLine($"Informationen zum ausgewählten Satelliten:");
                Console.WriteLine($"Name: {selectedSatellite.Name}");
            }

#else
            var satelliteRadius = this.satelliteMeshAsset.OverallMaximum * this.satelliteSizeScale;
            var earthPosition = new Vector3(0, 0, 0);
            var earthRadius = 0.5 * this.simulationData.RealEarthDiameter * this.simulationSizeScalar;

            var distance = CalculateDistancePointLine(
                this.cameraData.Eye,
                mouseRay,
                this.earthPosition);

            Console.WriteLine($"MouseX    : {e.X}");
            Console.WriteLine($"MouseY    : {e.Y}");
            Console.WriteLine($"RelMouseX : {mouseX}");
            Console.WriteLine($"RelMouseY : {mouseY}");
            Console.WriteLine($"\rsatellite: {this.earthPosition.X:F3}, {this.earthPosition.Y:F3}, {this.earthPosition.Z:F3}");
            Console.WriteLine($"ray      : {mouseRay.X:F3}, {mouseRay.Y:F3}, {mouseRay.Z:F3}");
            Console.WriteLine($"eye      : {this.cameraData.Eye.X:F3}, {this.cameraData.Eye.Y:F3}, {this.cameraData.Eye.Z:F3}");
            Console.WriteLine($"earth    : {satelliteRadius:F3}");
            Console.WriteLine($"distance : {distance:F3}");
            if(distance < satelliteRadius) {
                Console.WriteLine("EARTH SELECTED !!!!!!!!!!!!!!!!!!!!!!!!!11");
            }
#endif
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.W)
                this.keyboardInput.W = false;
            else if(e.Key == Key.A)
                this.keyboardInput.A = false;
            else if(e.Key == Key.S)
                this.keyboardInput.S = false;
            else if(e.Key == Key.D)
                this.keyboardInput.D = false;
            else if(e.Key == Key.Up)
                this.keyboardInput.UpArrow = false;
            else if(e.Key == Key.Down)
                this.keyboardInput.DownArrow = false;
            else if(e.Key == Key.Left)
                this.keyboardInput.LeftArrow = false;
            else if(e.Key == Key.Right)
                this.keyboardInput.RightArrow = false;

            if(e.Key == Key.C && e.Modifiers.HasFlag(KeyModifiers.Control))
                this.quit = true;
            if(e.Key == Key.Escape)
                this.quit = true;

            base.OnKeyDown(e);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.W)
                this.keyboardInput.W = true;
            else if(e.Key == Key.A)
                this.keyboardInput.A = true;
            else if(e.Key == Key.S)
                this.keyboardInput.S = true;
            else if(e.Key == Key.D)
                this.keyboardInput.D = true;
            else if(e.Key == Key.Up)
                this.keyboardInput.UpArrow = true;
            else if(e.Key == Key.Down)
                this.keyboardInput.DownArrow = true;
            else if(e.Key == Key.Left)
                this.keyboardInput.LeftArrow = true;
            else if(e.Key == Key.Right)
                this.keyboardInput.RightArrow = true;
            base.OnKeyDown(e);
        }

        private void MoveCamera(
                ref CameraData cameraData,
                float fTimeDelta,
                float translationSens,
                float rotationSens)
        {
            var directionOrtho = Vector3.Cross(cameraData.Up, cameraData.Direction);
            directionOrtho.Normalize();

            // translation
            if(this.keyboardInput.W) {
                cameraData.Eye += cameraData.Direction * translationSens * fTimeDelta;
            }
            if(this.keyboardInput.S) {
                cameraData.Eye += -cameraData.Direction * translationSens * fTimeDelta;
            }

            // rotation
            float angle = (float)(Math.PI * fTimeDelta * rotationSens);
            if(this.keyboardInput.UpArrow) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(directionOrtho, -angle);
                var newDirection4 = new Vector4(cameraData.Direction) * rotationMatrix;
                cameraData.Direction = newDirection4.Xyz;
            }
            if(this.keyboardInput.DownArrow) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(directionOrtho, angle);
                var newDirection4 = new Vector4(cameraData.Direction) * rotationMatrix;
                cameraData.Direction = newDirection4.Xyz;
            }
            if(this.keyboardInput.LeftArrow) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(cameraData.Up, angle);
                var newDirection4 = new Vector4(cameraData.Direction) * rotationMatrix;
                cameraData.Direction = newDirection4.Xyz;
            }
            if(this.keyboardInput.RightArrow) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(cameraData.Up, -angle);
                var newDirection4 = new Vector4(cameraData.Direction) * rotationMatrix;
                cameraData.Direction = newDirection4.Xyz;
            }
            if(this.keyboardInput.A) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(cameraData.Direction, -angle);
                var newUp4 = new Vector4(cameraData.Up) * rotationMatrix;
                cameraData.Up = newUp4.Xyz;
            }
            if(this.keyboardInput.D) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(cameraData.Direction, angle);
                var newUp4 = new Vector4(cameraData.Up) * rotationMatrix;
                cameraData.Up = newUp4.Xyz;
            }
            this.UpdateCameraTransformation(ref this.cameraData);
        }

        protected override void OnUnload(EventArgs e)
        {
            this.consoleTask.Wait(0);
            this.UnloadImageAsset(this.earthColorTexture);
            this.UnloadImageAsset(this.emptyNormalTexture);
            this.UnloadImageAsset(this.satelliteTexture);
            this.UnloadMeshData(this.sphereMeshAsset);
            this.UnloadMeshData(this.satelliteMeshAsset);
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
            var aspectRatio = Width / (float)Height;
            this.cameraData.ViewportWidth = Width;
            this.cameraData.ViewportHeight = Height;
            var yFieldOfView = (float)(fov * Math.PI / 180.0f);
            this.cameraData.PerspectiveProjection =
                Matrix4.CreatePerspectiveFieldOfView(yFieldOfView, aspectRatio, this.cameraData.zNear, this.cameraData.zFar);
        }

        private void UpdateCameraTransformation(ref CameraData cameraData)
        {
            cameraData.Transformation = Matrix4.LookAt(
                    cameraData.Eye,
                    cameraData.Eye + cameraData.Direction,
                    cameraData.Up);
        }

        // line direction needs to be normalized
        private float CalculateDistancePointLine(Vector3 linePoint, Vector3 lineDirection, Vector3 point)
        {
            float result = float.PositiveInfinity;
            var a = linePoint;
            var r = lineDirection;
            var p = point;
            float lambda =
                r.X * p.X - r.X * a.X + 
                r.Y * p.Y - r.Y * a.Y + 
                r.Z * p.Z - r.Z * a.Z;

            if(lambda > 0) {
                var pointOnPlane = linePoint + lineDirection * lambda;
                result = Vector3.Distance(pointOnPlane, point);
            }
            return result;
        }

        private void ProcessConsoleInput()
        {
            while(true) {
                var line = Console.ReadLine();
                if(line.StartsWith("simtime")) {
                    var time = line.Substring(7);
                    var newScalar = double.Parse(time.Trim());
                    this.simulationData.SimulationSpeed = newScalar;
                }
                else if (line.StartsWith("filter"))
                {
                    var filter = line.Substring(7);

                    foreach(var sat in simulationData.Satellites)
                    {
                        switch (filter)
                        {
                            case "all":
                                sat.IsVisible = true;
                                break;
                            case "none":
                                sat.IsVisible = false;
                                break;
                            case "iridium":
                                sat.IsVisible = sat.Name.Contains("Iridium");
                                break;
                            case "civ":
                                sat.IsVisible = sat.Users.Contains("Civil");
                                break;
                            case "com":
                                sat.IsVisible = sat.Users.Contains("Commercial");
                                break;
                            case "mil":
                                sat.IsVisible = sat.Users.Contains("Military");
                                break;
                            case "gov":
                                sat.IsVisible = sat.Users.Contains("Government");
                                break;
                            case "geo":
                                sat.IsVisible = sat.ClassOfOrbit.Equals("GEO");
                                break;
                            case "meo":
                                sat.IsVisible = sat.ClassOfOrbit.Equals("MEO");
                                break;
                            case "leo":
                                sat.IsVisible = sat.ClassOfOrbit.Equals("LEO");
                                break;
                            case "elp":
                                sat.IsVisible = sat.ClassOfOrbit.Equals("Elliptical");
                                break;
                            case "dbg":
                                sat.IsVisible = sat.Name.Contains("Beidou IGSO-3");
                                break;

                        }
                    }
                }
                else {
                    Console.WriteLine($"I am a teapot");
                }
                Console.Write("> ");
            }
        }

        //
        // simulation stuff
        //

        private void InitSimulationData()
        {
            this.simulationData = new SimData();

            this.simulationData.CurrentEarthRotation = 0;
            this.simulationData.SimulationSpeed = 1000.0f;


            var satelites = new List<SatelliteSimData>();
            var satDataStream = Utils.OpenEmbeddedResource("vissatellite.simdata.UCS_Satellite_Database_9-1-2017.txt");

            var streamReader = new System.IO.StreamReader(satDataStream);
            streamReader.ReadLine();
            string line;
            var rand = new Random();
            while ((line = streamReader.ReadLine()) != null)
            {

                string[] elements = line.Split('\t');

                var satelite = new SatelliteSimData();
                satelite.IsVisible = true;
                satelite.Name = elements[0];
                satelite.Users = elements[4];
                satelite.ClassOfOrbit = elements[7];

                //Read all the data we have
                if (!elements[9].Equals(""))
                    satelite.LongitudeOfGeo = float.Parse(elements[9], CultureInfo.InvariantCulture);

                satelite.Perigee = float.Parse(elements[10].Replace("\"", "").Replace(",", ""));
                satelite.Apogee = float.Parse(elements[11].Replace("\"", "").Replace(",", ""));
                satelite.Eccentricity = float.Parse(elements[12], CultureInfo.InvariantCulture);
                satelite.Inclenation = float.Parse(elements[13], CultureInfo.InvariantCulture) * (float) Math.PI/180;
                satelite.Periode = float.Parse(elements[14].Replace("\"", ""), CultureInfo.InvariantCulture) * 60;
                satelite.Position = new Vector3(satelites.Count, 0, 0);
                satelites.Add(satelite);

                //Calculated what we need
                satelite.SemiMajorAxis = (float)(satelite.Apogee + satelite.Perigee + simulationData.RealEarthDiameter) / 2;

                if (satelite.ClassOfOrbit == "GEO")
                {
                    Console.WriteLine(elements[13] + "\t" +satelite.Name);
                }

                //Generate random values for needed object elements that are not in the dataset
                satelite.LongitudeOfAscendingNode = (float)(rand.NextDouble() * Math.PI * 2);
                satelite.ArgumentOfPeriapsis = (float) (rand.NextDouble() * Math.PI * 2);
                //satelite.ArgumentOfPeriapsis = 0;
                //TODO: remove temp hack for not loading all satelites
                //if (satelites.Count >50)
                //    break;
            }

            this.simulationData.Satellites = satelites.ToArray();
        }

        private void DoSimulation(double fTimeDelta)
        {

            double time = elapsedSeconds * simulationData.SimulationSpeed;

            simulationData.CurrentEarthRotation = (float)((time % this.simulationData.RealEarthPeriode / this.simulationData.RealEarthPeriode) * 2 * Math.PI);


            for (int i = 0; i < this.simulationData.Satellites.Length; i++)
            {

                var satellite = this.simulationData.Satellites[i];

                if (satellite.IsVisible)
                {
                    double meanAnomaly = satellite.ArgumentOfPeriapsis;
                    meanAnomaly += (2 * Math.PI) * time / satellite.Periode;

                    //Calc aproximated true anomaly
                    double trueAnomaly = meanAnomaly;
                    trueAnomaly += 2 * satellite.Eccentricity * Math.Sin(meanAnomaly);
                    trueAnomaly += 5.0f / 4.0f *
                                   (satellite.Eccentricity * satellite.Eccentricity * Math.Sin(2 * meanAnomaly));


                    double distance = satellite.SemiMajorAxis;
                    distance *= (1 - satellite.Eccentricity * satellite.Eccentricity) /
                                (1 + satellite.Eccentricity * Math.Cos(trueAnomaly));


                    double polarAngle = Math.PI / 2 -
                                        (trueAnomaly - Math.PI+ satellite.LongitudeOfAscendingNode) * satellite.Inclenation;


                    //Convert spherical coordinates to cartesian coordinates
                    double posX = distance * Math.Sin(trueAnomaly) * Math.Sin(polarAngle);
                    double posZ = distance * Math.Cos(trueAnomaly) * Math.Sin(polarAngle);
                    double posY = distance * Math.Cos(polarAngle);


                    satellite.Position.X = (float) posX * (float) simulationSizeScalar;
                    satellite.Position.Y = (float) posY * (float) simulationSizeScalar;
                    satellite.Position.Z = (float) posZ * (float) simulationSizeScalar;
                }
            }
        }
    }
}
