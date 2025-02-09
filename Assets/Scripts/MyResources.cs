using System.Collections.Generic;
using UnityEngine;
public class BlockType {
    public BlockType (string blockName, bool hasCollision, float hardness, RenderType RenderType, int backFaceTexture) {
        this.blockName = blockName;
        this.hasCollision = hasCollision;
        this.hardness = hardness;
        this.RenderType = RenderType;
        this.backFaceTexture = backFaceTexture;
    }
    public string blockName;
    public bool hasCollision;
    public float hardness;
    public RenderType RenderType;
    public int backFaceTexture;
}
public static class MyResources {
    public static Material[] materials = new Material[2] { (Material)Resources.Load("Block"), (Material)Resources.Load("Water") };
    public static Camera camera;
    public static Transform cameraTransform;
    public static readonly int TextureSize = 16;
    public static readonly float mouseSens = 0.60F;
    public static readonly int solidGroundHeight = 48;
    public static readonly int terrainHeight = 12;
    public static float terrainScale = 0.03F;
    public static float treeZoneScale = 0.02F;
    public static float treeZoneThreshold = -0.3F;
    public static float treePlacementScale = 1.03F;
    public static float treePlacementThreshold = 0.35F;
    public static readonly BlockType[] blockTypes = new BlockType[7] {
        new ("Air", false, 0, RenderType.none, 0),
        new ("Grass", true, 1, RenderType.standard, 0),
        new ("Dirt", true, 1, RenderType.standard, 2),
        new ("Stone", true, 1, RenderType.standard, 1),
        new ("Glass", false, 1, RenderType.liquid, 49),
        new ("Glass", false, 1,  RenderType.notSolid, 49),
        new ("IronOre", true, 1, RenderType.standard, 47)
    };
    public static readonly Vector3[] voxelVerts = new Vector3[8] {
        new(0.0F, 0.0F, 0.0F),
        new(1.0F, 0.0F, 0.0F),
        new(1.0F, 1.0F, 0.0F),
        new(0.0F, 1.0F, 0.0F),
        new(0.0F, 0.0F, 1.0F),
        new(1.0F, 0.0F, 1.0F),
        new(1.0F, 1.0F, 1.0F),
        new(0.0F, 1.0F, 1.0F)
    };
    public static readonly Vector2[] voxelUVs = new Vector2[4] {
        new(0.0F, 0.0F),
        new(0.0F, 1.0F),
        new(1.0F, 0.0F),
        new(1.0F, 1.0F)
    };
    public static readonly Vector2Int[] chunkCheck = new Vector2Int[4] {
        new(0,  -1),
        new(0,  1),
        new(-1,  0),
        new(1,  0)
    };
    public static readonly int[] order = new int[6] { 0, 1, 2, 2, 1, 3 };
    public static readonly Vector3[] halfVoxelVerts = new Vector3[8] {
        new(0.0f, 0.0f, 0.0f),
        new(1.0f, 0.0f, 0.0f),
        new(1.0f, 0.5f, 0.0f),
        new(0.0f, 0.5f, 0.0f),
        new(0.0f, 0.0f, 1.0f),
        new(1.0f, 0.0f, 1.0f),
        new(1.0f, 0.5f, 1.0f),
        new(0.0f, 0.5f, 1.0f)
    };
    public static readonly byte[,,] trees = new byte[5, 7, 5] {
        {{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 },{0, 5, 5, 5, 0 },{0, 5, 5, 5, 0 },{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 } },
        {{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 },{5, 5, 5, 5, 5 },{5, 5, 5, 5, 5 },{0, 5, 5, 5, 0 },{0, 0, 5, 0, 0 } },
        {{0, 0, 4, 0, 0 },{0, 0, 4, 0, 0 },{0, 0, 4, 0, 0 },{5, 5, 4, 5, 5 },{5, 5, 4, 5, 5 },{0, 5, 4, 5, 0 },{0, 5, 5, 5, 0 } },
        {{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 },{5, 5, 5, 5, 5 },{5, 5, 5, 5, 5 },{0, 5, 5, 5, 0 },{0, 0, 5, 0, 0 } },
        {{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 },{0, 5, 5, 5, 0 },{0, 5, 5, 5, 0 },{0, 0, 0, 0, 0 },{0, 0, 0, 0, 0 } }
    };
    public static readonly int[,] grassMesh = new int[4, 4] {
        {0, 3, 5, 6}, // Back Face
        {5, 6, 0, 3}, // Front Face
        {4, 7, 1, 2}, // Left Face
        {1, 2, 4, 7}  // Right Face
    };
    public static readonly int[,] blockMesh = new int[6, 4] {
        {0, 3, 1, 2}, // Back Face
        {5, 6, 4, 7}, // Front Face
        {3, 7, 2, 6}, // Top Face
        {1, 5, 0, 4}, // Bottom Face
        {4, 7, 0, 3}, // Left Face
        {1, 2, 5, 6}  // Right Face
    };
    public static Vector2 TexturePos (int ID, int i) {
        return (voxelUVs[i] + new Vector2(ID % TextureSize, TextureSize - 1 - ID / TextureSize)) / TextureSize;
    }
    public static ChunkVoxel Vector3ToChunkVoxel (Vector3 pos) {
        Vector3Int p = Vector3Int.FloorToInt(pos);
        return new(new(p.x >> 4, p.z >> 4), new(p.x & 15, p.y & 127, p.z & 15));
    }
    public static ChunkVoxel Vector3ToChunkCoord (Vector3 pos) {
        Vector3Int p = Vector3Int.FloorToInt(pos);
        return new(new(p.x >> 4, p.z >> 4), new(p.x & 15, p.y & 127, p.z & 15));
    }
    public static Mesh MakeMesh (List<Vector3> vertices, List<int> triangles, List<Vector2> uvs) {
        Mesh mesh = new() {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.RecalculateNormals();
        return mesh;
    }
}
public struct VoxelAndPos {
    public ChunkVoxel pos;
    public byte id;
    public VoxelAndPos (ChunkVoxel pos, byte id) {
        this.pos = pos;
        this.id = id;
    }
}
[System.Serializable]
public struct ItemType {
    public string itemName;
    public Sprite sprite;
    public float mineSpeed;
}
public struct ChunkVoxel {
    public Vector2Int c;
    public Vector3Int v;
    public ChunkVoxel (Vector2Int c, Vector3Int v) {
        this.c = c;
        this.v = v;
    }
}
