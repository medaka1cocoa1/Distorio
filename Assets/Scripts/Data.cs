using UnityEngine;

public static class Data {

    public static readonly int ChunkWidth = 16;
    public static readonly int ChunkHeight = 128;

    public static readonly int InventoryWidth = 8;
    public static readonly int InventoryHeight = 4;

    public static readonly float gravityScale = 25.0F;
    public static readonly float jumpScale = 1;
    public static readonly float playerSpeed = 13.0F / 3.0F;
    public static readonly float resistance = 14.0F;

    public static readonly int ChunkLoadRange = 6;
    public static readonly int TextureSize = 16;
    public static readonly float mouseSens = 0.60F;

    public static readonly int solidGroundHeight = 48;
    public static readonly int terrainHeight = 12;

    public static float terrainScale = 0.03F;
    public static float treeZoneScale = 0.02F;
    public static float treeZoneThreshold = 0.1F;
    public static float treePlacementScale = 1.03F;
    public static float treePlacementThreshold = 0.35F;



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

    public static readonly Vector3Int[] faceChecks = new Vector3Int[6] {

        new(0, 0, -1),
        new(0, 0, 1),
        new(0, 1, 0),
        new(0, -1, 0),
        new(-1, 0, 0),
        new(1, 0, 0)

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


    public static bool ABCheck (AABB ccc, AABB ddd) {

        return !(ddd.minX >= ccc.maxX || ccc.minX >= ddd.maxX || ddd.minY >= ccc.maxY || ccc.minY >= ddd.maxY || ddd.minZ >= ccc.maxZ || ccc.minZ >= ddd.maxZ);
    }



    public static Vector2 TexturePos (int ID) {

        return new(ID % TextureSize, TextureSize - 1 - ID / TextureSize);

    }

    public static ChunkVoxel Vector3ToChunkVoxel (Vector3 pos) {

        Vector3Int a = Vector3Int.FloorToInt(pos);

        int x = pos.x >= 0 ? a.x % ChunkWidth : (a.x + 1) % ChunkWidth - 1 + ChunkWidth;
        int y = pos.y >= 0 ? a.y % ChunkHeight : (a.y + 1) % ChunkHeight - 1 + ChunkHeight;
        int z = pos.z >= 0 ? a.z % ChunkWidth : (a.z + 1) % ChunkWidth - 1 + ChunkWidth;

        return new(new(Mathf.FloorToInt(pos.x / ChunkWidth), Mathf.FloorToInt(pos.z / ChunkWidth)), new(x, y, z));

    }

}
public struct VoxelAndPos {
    public ChunkVoxel pos;
    public int id;
    public VoxelAndPos (ChunkVoxel pos, int id) {
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
