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
        public float XAngle;
        public float YAngle;
        public Vector3 Position;

        public Matrix4 Transformation;
        public Matrix4 PerspectiveProjection;
    }
}
