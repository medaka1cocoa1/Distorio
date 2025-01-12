using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public static class Chunks {
    private static readonly Dictionary<Vector2Int, Chunk> chunks = new();
    private static readonly Queue<Queue<VoxelAndPos>> modifications = new();
    private static readonly List<Vector2Int> ChunksToDraw = new();
    private static readonly List<Vector2Int> chunksToUpdate = new();
    public static void Update () {
        ModifyChunks();
        UpdateChunks();
        DrawChunks();
    }
    public static void LoadChunksAround (Vector2Int posC) {
        foreach (Chunk c in chunks.Values) {
            c.SetActiveState(false);
        }
        for (int x = posC.x - Data.CRange; x < posC.x + Data.CRange; x++) {
            for (int z = posC.y - Data.CRange; z < posC.y + Data.CRange; z++) {
                Vector2Int pos = new(x, z);
                if (!chunks.ContainsKey(pos)) {
                    chunks.Add(pos, new(pos));
                }
                chunks[pos].SetActiveState(true);
                if (!chunks[pos].IsTerrainMapGenerated) {
                    chunks[pos].GenerateTerrainData();
                    chunksToUpdate.Add(pos);
                }
            }
        }
    }
    public static void DestroyBlock (Vector3 position) {
        ChunkVoxel pos = Data.Vector3ToChunkVoxel(position);
        Queue<VoxelAndPos> queue = new();
        queue.Enqueue(new(pos, 0));
        modifications.Enqueue(queue);
    }
    public static void PlantTrees (Vector3Int pos) {
        Vector2 ddd = new(pos.x, pos.z);
        lock (modifications) {
            if (Noise.Get2DPerlin(ddd, 0.0158f) > 0) {
                if (new System.Random().Next(0, Mathf.FloorToInt(Mathf.Max(0, Noise.Get2DPerlin(ddd, 0.052f) + 0.5f) * 32 + 2)) == 0) {
                    Queue<VoxelAndPos> a = new();
                    a.Enqueue(new(Data.Vector3ToChunkVoxel(pos + Vector3Int.up), 4));
                    modifications.Enqueue(a);
                }
            }
            if (Noise.Get2DPerlin(ddd, Data.treeZoneScale) > Data.treeZoneThreshold) {
                if (Noise.Get2DPerlin(ddd, Data.treePlacementScale) > Data.treePlacementThreshold) {
                    modifications.Enqueue(Structure.MakeTree(pos + Vector3Int.up));
                }
            }
        }
    }
    public static List<int> CollidingIDs (AABB aabb) {
        List<int> a = new();
        for (int x = (int)Math.Floor(aabb.minX); x < (int)Math.Ceiling(aabb.maxX); x++) {
            for (int y = (int)Math.Floor(aabb.minY); y < (int)Math.Ceiling(aabb.maxY); y++) {
                for (int z = (int)Math.Floor(aabb.minZ); z < (int)Math.Ceiling(aabb.maxZ); z++) {
                    a.Add(GetVoxelID(new(x, y, z)));
                }
            }
        }
        return a;
    }
    public static List<AABB> GetCollidingBoundingBoxes (AABB aabb) {
        List<AABB> a = new();
        for (int x = (int)Math.Floor(aabb.minX); x < (int)Math.Ceiling(aabb.maxX); x++) {
            for (int y = (int)Math.Floor(aabb.minY); y < (int)Math.Ceiling(aabb.maxY); y++) {
                for (int z = (int)Math.Floor(aabb.minZ); z < (int)Math.Ceiling(aabb.maxZ); z++) {
                    if (Data.blockTypes[GetVoxelID(new(x, y, z))].hasCollision) {
                        a.Add(new(x, y, z, x + 1, y + 1, z + 1));
                    }
                }
            }
        }
        return a;
    }
    public static int GetVoxelID (Vector3 position) {
        ChunkVoxel pos = Data.Vector3ToChunkVoxel(position);
        return chunks.ContainsKey(pos.c) ? chunks[pos.c].GetVoxelIDChunk(pos.v) : 0;
    }
    public static bool SetBlock (Vector3 position, Vector3 selectingPos, byte itemID) {
        if (itemID != 0 && !Data.blockTypes[GetVoxelID(position)].hasCollision && Data.blockTypes[GetVoxelID(selectingPos)].hasCollision) {
            Queue<VoxelAndPos> queue = new();
            queue.Enqueue(new(Data.Vector3ToChunkVoxel(position), itemID));
            modifications.Enqueue(queue);
            return true;
        }
        return false;
    }
    private static void ModifyChunks () {
        lock (modifications) {
            while (modifications.Any()) {
                Queue<VoxelAndPos> queue = modifications.Dequeue();
                while (queue.Any()) {
                    VoxelAndPos vmod = queue.Dequeue();
                    if (!chunks.ContainsKey(vmod.pos.c)) {
                        chunks.Add(vmod.pos.c, new(vmod.pos.c));
                    }
                    chunks[vmod.pos.c].EnqueueVoxelMod(vmod);
                    if (!chunksToUpdate.Contains(vmod.pos.c)) {
                        chunksToUpdate.Add(vmod.pos.c);
                    }
                }
            }
        }
    }
    private static void UpdateChunks () {
        for (int i = chunksToUpdate.Count - 1; i >= 0; i--) {
            if (chunks[chunksToUpdate[i]].IsEditable) {
                chunks[chunksToUpdate[i]].UpdateChunk();
                ChunksToDraw.Add(chunksToUpdate[i]);
                for (int p = 0; p < 4; p++) {
                    Vector2Int fffpos = chunksToUpdate[i] + Data.chunkCheck[p];
                    if (chunks.ContainsKey(fffpos)) {
                        ChunksToDraw.Add(fffpos);
                    }
                }
                chunksToUpdate.RemoveAt(i);
            }
        }
    }
    private static void DrawChunks () {
        for (int i = ChunksToDraw.Count - 1; i >= 0; i--) {
            if (chunks[ChunksToDraw[i]].IsEditable) {
                chunks[ChunksToDraw[i]].GenerateMesh();
                ChunksToDraw.RemoveAt(i);
            }
        }
    }
}