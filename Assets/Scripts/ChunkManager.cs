using System;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager {

    private readonly World world;
    private readonly Dictionary<Vector2Int, Chunk> chunks = new();
    private readonly List<Chunk> Draw1 = new(Data.CRange * Data.CRange);
    private readonly List<Chunk> Draw2 = new(Data.CRange * Data.CRange);
    private readonly List<Chunk> chunksToUpdate = new();
    private readonly List<Entity> entities = new();
    private readonly Queue<Queue<VoxelAndPos>> modifications = new();
    private readonly Queue<Entity> entityQueue = new();
    private readonly Material[] materials;


    public ChunkManager (World world) {
        this.world = world;
        materials = world.materials;
    }

    public void Awak () {

        LoadChunksAround(new(0, 0, 0));
        ModifyChunks();
        UpdateChunks();
        DrawChunks();
        AddEntity(new EntityPlayer(world));
    }

    public void AddEntity (Entity entity) {
        entityQueue.Enqueue(entity);
    }

    public void Late () {


        while (entityQueue.Count > 0) {
            entities.Add(entityQueue.Dequeue());
        }
        for (int i = entities.Count - 1; i >= 0; i--) {
            if (entities[i].IsAlive) {
                entities[i].Update();
            } else {
                entities.RemoveAt(i);
            }
        }
        ModifyChunks();
        UpdateChunks();
        DrawChunks();
    }
    public List<int> CollidingIDs (AABB aabb) {
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

    static float GetHeight (Vector2 pos) {
        float terrainHeight = 0;
        for (int i = 0; i < 4; i++) {
            terrainHeight += Noise.Get2DPerlin(pos, Data.terrainScale / Mathf.Pow(2, i));
        }
        return terrainHeight * Data.terrainHeight * (Mathf.Pow(2, Noise.Get2DPerlin(pos, 0.029F) * 4) + Mathf.Pow(2, Noise.Get2DPerlin(pos, 0.005F) * 4) / 2) / (Noise.Get2DPerlin(pos, 0.05f) / 5000 + 1) + Data.solidGroundHeight;
    }
    public byte GetVoxel (int x, int y, int z) {

        byte VoxelValue = 0;
        Vector2 ddd = new(x, z);

        if (y < 40) {
            VoxelValue = 4;
        }
        switch (y - Mathf.FloorToInt(GetHeight(ddd))) {
            case < -4:
                VoxelValue = 3;
                break;
            case < -1:
                VoxelValue = 2;
                break;
            case < 0:
                VoxelValue = 1;
                break;
            default:
                break;
        }

        if (VoxelValue == 1) {
            EETT(ddd, new(x, y, z));
        }
        return VoxelValue;
    }
    void EETT (Vector2 ddd, Vector3Int pos) {

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
    public List<AABB> GetCollidingBoundingBoxes (AABB aabb, int? self) {
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
        foreach (Entity entity in entities) {
            if (self == null || self != null && entity.ID != self && aabb.IntersectsWith(entity.BoundingBox)) {
                a.Add(entity.BoundingBox);
            }
        }
        return a;
    }
    public void AddMod (Queue<VoxelAndPos> aadd) {
        modifications.Enqueue(aadd);
    }
    public void DestroyBlock (Vector3 position) {
        ChunkVoxel pos = Data.Vector3ToChunkVoxel(position);
        Queue<VoxelAndPos> queue = new();
        queue.Enqueue(new(pos, 0));
        AddMod(queue);
    }
    public int GetVoxelID (Vector3 position) {
        ChunkVoxel pos = Data.Vector3ToChunkVoxel(position);
        return chunks.ContainsKey(pos.c) ? chunks[pos.c].GetVoxelIDChunk(pos.v) : 0;
    }
    public bool SetBlock (Vector3 position, Vector3 selectingPos, int itemID) {
        if (itemID != 0 && !Data.blockTypes[GetVoxelID(position)].hasCollision && Data.blockTypes[GetVoxelID(selectingPos)].hasCollision) {
            Queue<VoxelAndPos> queue = new();
            queue.Enqueue(new(Data.Vector3ToChunkVoxel(position), itemID));
            AddMod(queue);
            return true;
        }
        return false;
    }
    void DrawChunks () {
        for (int i = Draw1.Count - 1; i >= 0; i--) {
            if (Draw1[i].IsEditable) {
                Draw1[i].Draw1();
                Draw2.Add(Draw1[i]);
                Draw1.RemoveAt(i);
            }
        }
        for (int i = Draw2.Count - 1; i >= 0; i--) {
            if (Draw2[i].IsEditable) {
                Draw2[i].Draw2();
                Draw2.RemoveAt(i);
            }
        }
    }
    private void UpdateChunks () {
        for (int i = chunksToUpdate.Count - 1; i >= 0; i--) {
            if (chunksToUpdate[i].IsEditable) {
                chunksToUpdate[i].UpdateChunk();
                Draw1.Add(chunksToUpdate[i]);
                chunksToUpdate.RemoveAt(i);
            }
        }
    }

    private void ModifyChunks () {
        lock (modifications) {
            while (modifications.Count > 0) {
                Queue<VoxelAndPos> queue = modifications.Dequeue();
                while (queue.Count > 0) {
                    VoxelAndPos vmod = queue.Dequeue();
                    if (!chunks.ContainsKey(vmod.pos.c)) {
                        chunks.Add(vmod.pos.c, new(vmod.pos.c, this, materials));
                    }
                    chunks[vmod.pos.c].EnqueueVoxelMod(vmod);
                    if (!chunksToUpdate.Contains(chunks[vmod.pos.c])) {
                        chunksToUpdate.Add(chunks[vmod.pos.c]);
                    }
                }
            }
        }
    }
    public void LoadChunksAround (Vec3i posC) {
        foreach (Chunk c in chunks.Values) {
            c.SetActiveState(false);
        }
        for (int x = posC.x - Data.CRange; x < posC.x + Data.CRange; x++) {
            for (int z = posC.z - Data.CRange; z < posC.z + Data.CRange; z++) {
                Vector2Int pos = new(x, z);
                if (!chunks.ContainsKey(pos)) {
                    chunks.Add(pos, new(pos, this, materials));
                }
                if (!chunks[pos].IsTerrainMapGenerated) {
                    chunks[pos].GenerateTerrainData();
                    chunksToUpdate.Add(chunks[pos]);
                }
            }
        }
        for (int x = posC.x - Data.CRange - 12; x < posC.x + Data.CRange + 12; x++) {
            for (int z = posC.z - Data.CRange - 12; z < posC.z + Data.CRange + 12; z++) {
                Vector2Int pos = new(x, z);
                if (chunks.ContainsKey(pos)) {
                    chunks[pos].SetActiveState(true);
                }
            }
        }
    }
    public void QueueDraw1 (Vector2Int pos) {
        for (int i = 0; i < 4; i++) {
            Vector2Int fffpos = pos + Data.chunkCheck[i];
            if (chunks.ContainsKey(fffpos)) {
                Draw1.Add(chunks[fffpos]);
            }
        }
    }

}
