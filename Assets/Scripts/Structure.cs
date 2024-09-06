using System.Collections.Generic;
using UnityEngine;

public class Structure : MonoBehaviour {

    public static Queue<VoxelMod> MakeCactus (Vector3Int position) {

        Queue<VoxelMod> queue = new();

        for (int _y = 0; _y < 4; _y++) {
            queue.Enqueue(new VoxelMod(Data.Vector3ToChunkVoxel(new Vector3Int(0, _y, 0) + position + Vector3.one * 0.5f), 11));
        }

        return queue;

    }
    public static Queue<VoxelMod> MakeTree (Vector3Int position) {
        Queue<VoxelMod> queue = new();
        for (int _x = 0; _x < 5; _x++) {
            for (int _y = 0; _y < 7; _y++) {
                for (int _z = 0; _z < 5; _z++) {
                    if (Data.trees[_x, _y, _z] != 0) {
                        Vector3Int pos = new Vector3Int(_x, _y, _z) + position;
                        queue.Enqueue(new VoxelMod(Data.Vector3ToChunkVoxel(pos + Vector3.one * 0.5f + new Vector3Int(-2, 0, -2)), Data.trees[_x, _y, _z]));
                    }
                }
            }
        }
        return queue;
    }
    public static Queue<VoxelMod> MakeTreeTwo (Vector3Int position) {
        Queue<VoxelMod> queue = new();
        for (int _x = 0; _x < 5; _x++) {
            for (int _y = 0; _y < 9; _y++) {
                for (int _z = 0; _z < 5; _z++) {
                    if (Data.treesTwo[_x, _y, _z] != 0) {
                        Vector3Int pos = new Vector3Int(_x, _y, _z) + position;
                        queue.Enqueue(new VoxelMod(Data.Vector3ToChunkVoxel(pos + Vector3.one * 0.5f + new Vector3Int(-2, 0, -2)), Data.treesTwo[_x, _y, _z]));
                    }
                }
            }
        }
        return queue;
    }
}

public struct VoxelMod {

    public ChunkVoxel pos;
    public byte id;

    public VoxelMod (ChunkVoxel _position, byte _id) {

        pos = _position;
        id = _id;

    }

}