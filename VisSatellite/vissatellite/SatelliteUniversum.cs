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
        private GameData gameData;

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

            this.gameData = new GameData();
            this.gameData.ConsoleTask = Task.Run((Action)this.ProcessConsoleInput);

            this.gameData.SphereMeshAsset = new MeshAssetData();
            this.gameData.SphereMeshAsset.AssetName = "vissatellite.meshes.sphere.obj";
            this.LoadMeshAsset(this.gameData.SphereMeshAsset);

            this.gameData.SatelliteMeshAsset = new MeshAssetData();
            this.gameData.SatelliteMeshAsset.AssetName = "vissatellite.meshes.satellite.obj";
            this.LoadMeshAsset(this.gameData.SatelliteMeshAsset);

            this.gameData.EarthColorTexture = new ImageAssetData();
            this.gameData.EarthColorTexture.AssetName = "vissatellite.textures.earth.jpg";
            this.LoadImageAsset(this.gameData.EarthColorTexture);

            this.gameData.SatelliteTexture = new ImageAssetData();
            this.gameData.SatelliteTexture.AssetName = "vissatellite.textures.satellite_texture.jpg";
            this.LoadImageAsset(this.gameData.SatelliteTexture);

            this.gameData.SatelliteTextureSelected = new ImageAssetData();
            this.gameData.SatelliteTextureSelected.AssetName = "vissatellite.textures.satellite_texture_selected.jpg";
            this.LoadImageAsset(this.gameData.SatelliteTextureSelected);

            this.gameData.EmptyNormalTexture = new ImageAssetData();
            this.gameData.EmptyNormalTexture.AssetName = "vissatellite.textures.empty_normal.jpg";
            this.LoadImageAsset(this.gameData.EmptyNormalTexture);

            this.gameData.BlinnShader = new BlinnShaderAsset();
            this.gameData.BlinnShader.BasicShader = new BasicShaderAssetData();
            this.gameData.BlinnShader.BasicShader.VertexShaderName = "vissatellite.shader.Blinn_VS.glsl";
            this.gameData.BlinnShader.BasicShader.FragmentShaderName = "vissatellite.shader.Blinn_FS.glsl";
            this.LoadBlinnShaderAsset(this.gameData.BlinnShader);

            this.gameData.CameraData = new CameraData();
            this.gameData.CameraData.Eye = new Vector3(0, 0, 20);
            this.gameData.CameraData.Up = new Vector3(0, 1, 0);
            this.gameData.CameraData.Direction = new Vector3(0, 0, -1);
            this.gameData.CameraData.zNear = 0.1f;
            this.gameData.CameraData.zFar = 5000.0f;
            UpdateCameraTransformation(this.gameData.CameraData);

            this.gameData.AmbientLightDirection = new Vector3(-1, 0, 0);
            this.gameData.AmbientLightDirection.Normalize();
            this.gameData.SimulationData = new SimData();
            this.InitSimulationData(this.gameData.SimulationData);

            this.gameData.KeyboardInput = new KeyboardInput();

            Console.WriteLine();
            Console.WriteLine("Befehle");
            Console.WriteLine("simtime time    setzen der simulationsgeschwindigkeit");
            Console.WriteLine("show all        zeigt alle Satelliten an");
            Console.WriteLine("     none       zeigt keinen Satelliten an");
            Console.WriteLine("     iridium    zeigt Iridium Satelliten an");
            Console.WriteLine("     civ        zeigt zivile Satelliten an");
            Console.WriteLine("     com        zeigt kommerzielle Satelliten an");
            Console.WriteLine("     mil        zeigt Milit?r-Satelliten an");
            Console.WriteLine("     gov        zeigt Regierungs-Satelliten an");
            Console.WriteLine("     geo        zeigt Satelliten im GEO an");
            Console.WriteLine("     meo        zeigt Satelliten im MEO an");
            Console.WriteLine("     leo        zeigt Satelliten im LEO an");
            Console.WriteLine("     elp        zeigt Satelliten in eliptischen Umlaufbahnen an");

            Console.WriteLine();
            Console.Write("> ");

            GL.Enable(EnableCap.DepthTest);
        }

        private void LoadImageAsset(ImageAssetData asset)
        {
            if (asset.IsLoaded)
                return;

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

        private void LoadBlinnShaderAsset(BlinnShaderAsset shader)
        {
            this.LoadShaderAsset(shader.BasicShader);
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

        private void LoadShaderAsset(BasicShaderAssetData shaderAsset)
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

        private void LoadMeshAsset(MeshAssetData meshAsset)
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
            if (this.gameData.Quit) {
                this.Exit();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            this.MoveCamera(this.gameData.CameraData, (float)e.Time, 6.0f, 0.3f);
            this.DoSimulation(this.gameData.SimulationData, e.Time);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var earthMatrix =
                Matrix4.Identity *
                Matrix4.CreateRotationY(this.gameData.SimulationData.CurrentEarthRotation) *
                Matrix4.CreateScale((float)this.gameData.SimulationData.RealEarthDiameter * (float)this.gameData.SimulationData.SimulationSizeScalar);

            RenderWithBlinn(
                this.gameData.SphereMeshAsset,
                this.gameData.EarthColorTexture,
                this.gameData.EmptyNormalTexture,
                earthMatrix);

            var satelliteSimData = this.gameData.SimulationData.Satellites;
            for(int i = 0; i < satelliteSimData.Length; i++) {
                var satellite = satelliteSimData[i];
                if (satellite.IsVisible) {
                    var selectedScale = satellite.IsSelected ? 5.0f : 1.0f;
                    var satelliteMatrix =
                        Matrix4.Identity *
                        Matrix4.CreateScale(this.gameData.SatelliteSizeScale) *
                        Matrix4.CreateTranslation(satellite.Position);

                    var texture = satellite.IsSelected
                        ? this.gameData.SatelliteTextureSelected
                        : this.gameData.SatelliteTexture;

                    RenderWithBlinn(
                        this.gameData.SatelliteMeshAsset,
                        texture,
                        this.gameData.EmptyNormalTexture,
                        satelliteMatrix);
                }
            }
            this.SwapBuffers();
        }

        private void RenderWithBlinn(
            MeshAssetData mesh,
            ImageAssetData colorTexture,
            ImageAssetData normalTexture,
            Matrix4 modelMatrix)
        {
            var modelViewProjection =
                modelMatrix *
                this.gameData.CameraData.Transformation *
                this.gameData.CameraData.PerspectiveProjection;

            var shader = this.gameData.BlinnShader;

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, colorTexture.OpenGLHandle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, normalTexture.OpenGLHandle);

            GL.BindVertexArray(mesh.VertexArrayObjectHandle);

            GL.UseProgram(shader.BasicShader.ProgramHandle);
            GL.UniformMatrix4(
                shader.BasicShader.ModelviewProjectionMatrixLocation,
                false,
                ref modelViewProjection);

            GL.UniformMatrix4(shader.ModelMatrixLocation, false, ref modelMatrix);
            GL.Uniform1(shader.ColorTextureLocation, 0);
            GL.Uniform1(shader.NormalTextureLocation, 1);
            GL.Uniform1(shader.MaterialShininessLocation, 2.0f);
            GL.Uniform3(shader.LightDirectionLocation, this.gameData.AmbientLightDirection);

            GL.Uniform4(shader.LightAmbientColorLocation, new Vector4(0.6f, 0.6f, 0.6f, 0));
            GL.Uniform4(shader.LightDiffuseColorLocation, new Vector4(0.8f, 0.8f, 0.8f, 0));
            GL.Uniform4(shader.LightSpecularColorLocation, new Vector4(0.0f, 0.0f, 0.0f, 0));
            GL.Uniform4(shader.CameraPositionLocation, new Vector4(this.gameData.CameraData.Eye, 1));

            GL.DrawElements(
                PrimitiveType.Triangles,
                mesh.IndicesCount,
                DrawElementsType.UnsignedInt,
                IntPtr.Zero);

            GL.BindVertexArray(0);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        //
        // input processing
        //
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var mouseRay = this.CalculateMouseRay(e.X, e.Y);

            var satelliteRadius = this.gameData.SatelliteMeshAsset.OverallMaximum * this.gameData.SatelliteSizeScale;
            var minHitDistance = float.PositiveInfinity;
            var minHitIndex = -1;

            for(int i = 0; i < this.gameData.SimulationData.Satellites.Length; i++) {
                var satellite = this.gameData.SimulationData.Satellites[i];
                satellite.IsSelected = false;
                var sPosition = satellite.Position;
                var distance = CalculateDistancePointLine(
                    this.gameData.CameraData.Eye,
                    mouseRay,
                    sPosition);

                if(minHitDistance > distance) {
                    minHitDistance = distance;
                    minHitIndex = i;
                }
            }

            if(minHitIndex > 0) {
                var selectedSatellite = this.gameData.SimulationData.Satellites[minHitIndex];

                if (selectedSatellite.IsVisible)
                {
                    selectedSatellite.IsSelected = true;
                    Console.WriteLine($"\rInformationen zum ausgewählten Satelliten:");
                    Console.WriteLine($"Name: {selectedSatellite.Name}");
                    Console.WriteLine($"Verwender: {selectedSatellite.Users}");

                    Console.WriteLine($"Orbit Typ: {selectedSatellite.ClassOfOrbit}");
                    Console.WriteLine($"Apoapsis: {selectedSatellite.Apogee} km");
                    Console.WriteLine($"Periapsis: {selectedSatellite.Perigee} km");
                    Console.WriteLine($"Exzentrizität: {selectedSatellite.Eccentricity}");
                    Console.WriteLine($"Bahnneigung: {selectedSatellite.Inclenation * 180 / Math.PI}°");

                    Console.Write("> ");
                }

            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            var keyboardInput = this.gameData.KeyboardInput;
            if (e.Key == Key.W)
                keyboardInput.W = false;
            else if(e.Key == Key.A)
                keyboardInput.A = false;
            else if(e.Key == Key.S)
                keyboardInput.S = false;
            else if(e.Key == Key.D)
                keyboardInput.D = false;
            else if(e.Key == Key.Up)
                keyboardInput.UpArrow = false;
            else if(e.Key == Key.Down)
                keyboardInput.DownArrow = false;
            else if(e.Key == Key.Left)
                keyboardInput.LeftArrow = false;
            else if(e.Key == Key.Right)
                keyboardInput.RightArrow = false;

            if(e.Key == Key.C && e.Modifiers.HasFlag(KeyModifiers.Control))
                this.gameData.Quit = true;
            if(e.Key == Key.Escape)
                this.gameData.Quit = true;

            base.OnKeyDown(e);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            var keyboardInput = this.gameData.KeyboardInput;
            if (e.Key == Key.W)
                keyboardInput.W = true;
            else if(e.Key == Key.A)
                keyboardInput.A = true;
            else if(e.Key == Key.S)
                keyboardInput.S = true;
            else if(e.Key == Key.D)
                keyboardInput.D = true;
            else if(e.Key == Key.Up)
                keyboardInput.UpArrow = true;
            else if(e.Key == Key.Down)
                keyboardInput.DownArrow = true;
            else if(e.Key == Key.Left)
                keyboardInput.LeftArrow = true;
            else if(e.Key == Key.Right)
                keyboardInput.RightArrow = true;
            base.OnKeyDown(e);
        }

        private void MoveCamera(
                CameraData camera,
                float fTimeDelta,
                float translationSens,
                float rotationSens)
        {
            var keyboardInput = this.gameData.KeyboardInput;
            var directionOrtho = Vector3.Cross(camera.Up, camera.Direction);
            directionOrtho.Normalize();

            // translation
            if(keyboardInput.W) {
                camera.Eye += camera.Direction * translationSens * fTimeDelta;
            }
            if(keyboardInput.S) {
                camera.Eye += -camera.Direction * translationSens * fTimeDelta;
            }

            // rotation
            float angle = (float)(Math.PI * fTimeDelta * rotationSens);
            if(keyboardInput.UpArrow) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(directionOrtho, -angle);
                var newDirection4 = new Vector4(camera.Direction) * rotationMatrix;
                camera.Direction = newDirection4.Xyz;
            }
            if(keyboardInput.DownArrow) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(directionOrtho, angle);
                var newDirection4 = new Vector4(camera.Direction) * rotationMatrix;
                camera.Direction = newDirection4.Xyz;
            }
            if(keyboardInput.LeftArrow) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(camera.Up, angle);
                var newDirection4 = new Vector4(camera.Direction) * rotationMatrix;
                camera.Direction = newDirection4.Xyz;
            }
            if(keyboardInput.RightArrow) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(camera.Up, -angle);
                var newDirection4 = new Vector4(camera.Direction) * rotationMatrix;
                camera.Direction = newDirection4.Xyz;
            }
            if(keyboardInput.A) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(camera.Direction, -angle);
                var newUp4 = new Vector4(camera.Up) * rotationMatrix;
                camera.Up = newUp4.Xyz;
            }
            if(keyboardInput.D) {
                var rotationMatrix = Matrix4.CreateFromAxisAngle(camera.Direction, angle);
                var newUp4 = new Vector4(camera.Up) * rotationMatrix;
                camera.Up = newUp4.Xyz;
            }
            UpdateCameraTransformation(camera);
        }

        private void ProcessConsoleInput()
        {
            while(true) {
                var line = Console.ReadLine();
                if(line.StartsWith("simtime")) {
                    var time = line.Substring(7);
                    var newScalar = double.Parse(time.Trim());
                    this.gameData.SimulationData.SimulationSpeed = newScalar;
                }
                else if (line.StartsWith("show"))
                {
                    var filter = line.Substring(5);

                    foreach(var sat in gameData.SimulationData.Satellites)
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
        // unloading
        //

        protected override void OnUnload(EventArgs e)
        {
            this.gameData.ConsoleTask.Wait(0);
            this.UnloadImageAsset(this.gameData.EarthColorTexture);
            this.UnloadImageAsset(this.gameData.EmptyNormalTexture);
            this.UnloadImageAsset(this.gameData.SatelliteTexture);
            this.UnloadMeshData(this.gameData.SphereMeshAsset);
            this.UnloadMeshData(this.gameData.SatelliteMeshAsset);
        }

        private void UnloadBlinnShaderAsset(BlinnShaderAsset shader)
        {
            this.UnloadShaderAsset(shader.BasicShader);
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

        //
        // utility
        //

        protected override void OnResize(EventArgs e)
        {
            var fov = 60;
            GL.Viewport(0, 0, Width, Height);
            var aspectRatio = Width / (float)Height;
            this.gameData.CameraData.ViewportWidth = Width;
            this.gameData.CameraData.ViewportHeight = Height;
            var yFieldOfView = (float)(fov * Math.PI / 180.0f);
            this.gameData.CameraData.PerspectiveProjection = Matrix4.CreatePerspectiveFieldOfView(
                yFieldOfView,
                aspectRatio,
                this.gameData.CameraData.zNear,
                this.gameData.CameraData.zFar);
        }

        private void UpdateCameraTransformation(CameraData cameraData)
        {
            cameraData.Transformation = Matrix4.LookAt(
                    cameraData.Eye,
                    cameraData.Eye + cameraData.Direction,
                    cameraData.Up);
        }

        private Vector3 CalculateMouseRay(int x, int y)
        {
            var mouseX = ((x / (float)this.gameData.CameraData.ViewportWidth) * 2.0f) - 1.0f;
            var mouseY = 1.0f - ((y / (float)this.gameData.CameraData.ViewportHeight) * 2.0f);
            var mouseVector = new Vector4(mouseX, mouseY, 0, 1.0f);

            var inverseTransformation = Matrix4.Invert(this.gameData.CameraData.Transformation);
            var inverseProjection = Matrix4.Invert(this.gameData.CameraData.PerspectiveProjection);
            mouseVector = mouseVector * inverseProjection;
            mouseVector = mouseVector * inverseTransformation;

            if (mouseVector.W > float.Epsilon || mouseVector.W < float.Epsilon) {
                mouseVector.X /= mouseVector.W;
                mouseVector.Y /= mouseVector.W;
                mouseVector.Z /= mouseVector.W;
            }

            var mouseRay = mouseVector.Xyz - this.gameData.CameraData.Eye;
            mouseRay.Normalize();
            return mouseRay;
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

        //
        // simulation stuff
        //

        private void InitSimulationData(SimData simData)
        {
            simData.CurrentEarthRotation = 0;
            simData.SimulationSpeed = 1000.0f;
            simData.SimulationSizeScalar = 0.001f;


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
                satelite.IsSelected = false;

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

                //Calculated what we need
                satelite.SemiMajorAxis = (float)(satelite.Apogee + satelite.Perigee + simData.RealEarthDiameter) / 2;

                //Generate random values for needed object elements that are not in the dataset
                satelite.LongitudeOfAscendingNode = (float)(rand.NextDouble() * Math.PI * 2);
                satelite.ArgumentOfPeriapsis = (float) (rand.NextDouble() * Math.PI * 2);


                satelite.Position = new Vector3(0, 0, 0);
                satelites.Add(satelite);


            }
            simData.Satellites = satelites.ToArray();
        }

        private void DoSimulation(SimData simData, double fTimeDelta)
        {
            simData.ElapsedSeconds += fTimeDelta;
            double time = simData.ElapsedSeconds * simData.SimulationSpeed;

            simData.CurrentEarthRotation = (float)(time % simData.RealEarthPeriode / simData.RealEarthPeriode * 2 * Math.PI);


            for (int i = 0; i < simData.Satellites.Length; i++)
            {

                var satellite = simData.Satellites[i];

                if (satellite.IsVisible)
                {
                    double meanAnomaly = satellite.ArgumentOfPeriapsis;
                    meanAnomaly += (2 * Math.PI) * time / satellite.Periode;

                    meanAnomaly %= Math.PI * 2;

                    //Calc aproximated true anomaly
                    double trueAnomaly = meanAnomaly;
                    trueAnomaly += 2 * satellite.Eccentricity * Math.Sin(meanAnomaly);
                    trueAnomaly += 5.0f / 4.0f *
                                   (satellite.Eccentricity * satellite.Eccentricity * Math.Sin(2 * meanAnomaly));


                    double distance = satellite.SemiMajorAxis;
                    distance *= (1 - satellite.Eccentricity * satellite.Eccentricity) /
                                (1 + satellite.Eccentricity * Math.Cos(trueAnomaly));


                    double polarAngle = Math.Sin(trueAnomaly + satellite.LongitudeOfAscendingNode) * satellite.Inclenation;

                    //Convert spherical coordinates to cartesian coordinates
                    double posX = distance * Math.Sin(trueAnomaly) * Math.Cos(polarAngle);
                    double posZ = distance * Math.Cos(trueAnomaly) * Math.Cos(polarAngle);
                    double posY = distance * Math.Sin(polarAngle);


                    satellite.Position.X = (float) posX * (float) simData.SimulationSizeScalar;
                    satellite.Position.Y = (float) posY * (float) simData.SimulationSizeScalar;
                    satellite.Position.Z = (float) posZ * (float) simData.SimulationSizeScalar;

                }
            }
        }
    }
}
