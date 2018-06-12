using System;
using System.Diagnostics;
using System.Threading.Tasks;
using OpenTK;

namespace vissatellite
{
    public class GameData
    {
        public bool Quit;
        public float SatelliteSizeScale = 0.3f;

        public ImageAssetData EarthColorTexture;
        public ImageAssetData SatelliteTexture;
        public ImageAssetData SatelliteTextureSelected;
        public ImageAssetData EmptyNormalTexture;
        public MeshAssetData SphereMeshAsset;
        public MeshAssetData SatelliteMeshAsset;
        public CameraData CameraData;

        public BlinnShaderAsset BlinnShader;
        public Vector3 AmbientLightDirection;
        public SimData SimulationData;
        public KeyboardInput KeyboardInput;
        public Task ConsoleTask;
    }

    public class ObjectVertexData
    {
        public int[]     Indices;
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] UVs;
        public Vector3[] Tangents;
        public Vector3[] BiTangents;

        public float xMax;
        public float xMin;
        public float yMin;
        public float yMax;
        public float zMin;
        public float zMax;
    }

    public class ImageAssetData
    {
        public string AssetName;
        public bool IsLoaded;
        public int OpenGLHandle;
        public bool IsDisplacement;
    }

    public class MeshAssetData
    {
        public string AssetName;
        public bool IsLoaded;

        public int IndicesCount;
        public int VertexBufferHandle;
        public int IndicesBufferHandle;
        public int VertexArrayObjectHandle;

        public float xMin;
        public float xMax;
        public float yMin;
        public float yMax;
        public float OverallMaximum;
    }

    public class BasicShaderAssetData
    {
        public bool IsLoaded;
        public string FragmentShaderName;
        public string VertexShaderName;

        public int VertexObjectHandle;
        public int FragmentObjectHandle;
        public int ProgramHandle;
        public int ModelviewProjectionMatrixLocation;
    }

    public class BlinnShaderAsset
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

    public class CameraData
    {
        // perspective projection
        public float AspectRatio;
        public int ViewportWidth;
        public int ViewportHeight;
        public float zNear;
        public float zFar;

        public Matrix4 PerspectiveProjection;

        // moving with mouse and cursor
        public Vector3 Eye;
        public Vector3 Up;
        public Vector3 Direction;

        // calculated from previous values
        public Matrix4 Transformation;
    }

    public class SimData
    {
        public double ElapsedSeconds;
        public float RealEarthPeriode = 24 * 60 * 60;
        public float RealEarthDiameter = 12742;
	    public double SimulationSizeScalar;

        public double SimulationSpeed;

        public float CurrentEarthRotation;
        public SatelliteSimData[] Satellites;
    }

    public class SatelliteSimData
    {
        public Vector3 Position;

        public bool IsVisible;

        public string Name;
        public string Users;
        public string ClassOfOrbit;


        public float Apogee;
        public float Perigee;
        public float LongitudeOfGeo;

        public float Eccentricity; //e
        public float SemiMajorAxis; //a

        public float Inclenation; //i
        public float LongitudeOfAscendingNode; //Ω
        public float ArgumentOfPeriapsis; //ω
        public float Periode;
        public bool IsSelected;
    }

    public class KeyboardInput
    {
        public bool W;
        public bool A;
        public bool S;
        public bool D;

        public bool LeftArrow;
        public bool UpArrow;
        public bool DownArrow;
        public bool RightArrow;
    }

    public static class VertexAttribIndex
    {
        public const int Vertex = 0;
        public const int Normal = 1;
        public const int Uv = 2;
        public const int Tangent = 3;
        public const int Bitangent = 4;
    }
}
