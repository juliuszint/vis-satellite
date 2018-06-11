using System;
using OpenTK;

namespace vissatellite
{
    public struct Vector3i
    {
        public int X;
        public int Y;
    }

    public class ObjectVertexData
    {
        public int[]     Indices;
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
        public float TotalSimulationTime;
        public float SimulationSpeed;

        public SatelliteSimData[] Satellites;
    }

    public class SatelliteSimData
    {
        public Vector3 Position;

        public string Name;
        public float Apogee;
        public float Perigee;
        public float LongitudeOfGeo;

        public float Eccentricity; //e
        public float SemiMajorAxis; //a

        public float Inclenation; //i
        public float LongitudeOfAscendingNode; //Ω
        public float ArgumentOfPeriapsis; //ω
        public float Periode;
    }

    public struct KeyboardInput
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
}
