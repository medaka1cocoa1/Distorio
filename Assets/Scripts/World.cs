using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class World : MonoBehaviour {
    public AudioClip audioClip;

    public BiomeAttributes biome;
    public Material material;
    public List<BlockType> blockTypes = new();
    public List<ItemType> itemTypes = new();
    public Sprite slot;
    public List<Entity> entities = new();

    private Vector2 offset;
    private readonly Dictionary<Vector2Int, Chunk> chunks = new();
    private readonly Queue<Queue<VoxelAndPos>> modifications = new();
    private readonly List<Chunk> chunksToUpdate = new();
    private readonly Queue<Chunk> chunksToDraw = new();

    private void Awake () {

        offset = new(Random.Range(-66666.6f, 66666.6f), Random.Range(-66666.6f, 66666.6f));
        Random.InitState((int)System.DateTime.Now.Ticks);

        for (int x = -8; x < 8; x++) {
            for (int y = -8; y < 8; y++) {

                Vector2Int pos = new(x, y);

                chunks.Add(pos, new(pos, this));
                if (!chunksToUpdate.Contains(chunks[pos]))
                    chunksToUpdate.Add(chunks[pos]);


            }
        }
    }
    private void Start () {

        for(int i = 0; i < 10; i++) {
            entities.Add(new(Vector3.one * 0.9f, this, 50000f));
            entities[i].AddRotation(Random.Range(-180, 180) );
        }
    }
    private void LateUpdate () {

        for (int i = 0; i < 10; i++) {
            entities[i].AddRotation(90 * Time.deltaTime);
            entities[i].AddVel(Vector3.forward, true);
            entities[i].Apply();
        }

        ModifyChunks();
        UpdateChunks();
        DrawChunks();
    }

    private float Get2DPerlin (Vector2 position, float scale) {
        return Mathf.PerlinNoise(position.x * scale + offset.x, position.y * scale + offset.y) - 0.5f;
    }
    public void CheckViewDistance (Vector2Int playerPos) {
        List<Vector2Int> previouslyActiveChunks = chunks.Keys.ToList();
        for (int x = playerPos.x - Data.ChunkLoadRange; x < playerPos.x + Data.ChunkLoadRange; x++) {
            for (int y = playerPos.y - Data.ChunkLoadRange; y < playerPos.y + Data.ChunkLoadRange; y++) {
                Vector2Int pos = new(x, y);
                if (!chunks.ContainsKey(pos)) {
                    chunks.Add(pos, new(pos, this));
                    if (!chunksToUpdate.Contains(chunks[pos]))
                        chunksToUpdate.Add(chunks[pos]);
                } else {
                    chunks[pos].SetActiveState(true);
                }
                for (int i = 0; i < previouslyActiveChunks.Count; i++) {
                    if (previouslyActiveChunks[i] == pos) {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }
        foreach (Vector2Int c in previouslyActiveChunks) {
            chunks[c].SetActiveState(false);
        }
        previouslyActiveChunks.Clear();
    }
    private float GetHeight (Vector3Int pos) {
        float terrainHeight = 0;
        for (int i = 0; i < 4; i++) {
            float a = Get2DPerlin(new(pos.x, pos.z), 0.05f) / 5000 + 1;
            terrainHeight += biome.terrainHeight * Get2DPerlin(new(pos.x, pos.z), biome.terrainScale / Mathf.Pow(2, i) / a);
        }
        float sec = Mathf.Pow(2, Get2DPerlin(new(pos.x, pos.z), 0.029f) * 4) + Mathf.Pow(2, Get2DPerlin(new(pos.x, pos.z), 0.005f) * 4);
        terrainHeight *= sec / 2;
        terrainHeight = Mathf.FloorToInt(terrainHeight + biome.solidGroundHeight);
        return terrainHeight;
    }
    public byte GetVoxel (Vector3Int pos) {

        float terrainHeight = GetHeight(pos);
        float temp = GetTemp(pos);
        byte VoxelValue = 0;

        if (pos.y < terrainHeight) {

            if (pos.y == terrainHeight - 1) {
                if (temp < -5f) {
                    VoxelValue = 12;
                } else if (temp < 20f) {
                    VoxelValue = 1;
                } else {
                    VoxelValue = 6;
                }
            } else if (pos.y < terrainHeight - 1 && pos.y >= terrainHeight - 4) {
                VoxelValue = 2;
            } else {
                VoxelValue = 3;
            }
        }

        if (pos.y == terrainHeight) {
            lock (modifications) {
                int add = 0;
                byte type = 0;
                if (temp < -5f) {
                } else if (temp < 20f) {
                    add = 32;
                    type = 15;
                } else {
                    add = 128;
                    type = 14;
                }
                float www = Mathf.Max(0, Get2DPerlin(new(pos.x, pos.z), 0.052f) + 0.5f) * add;


                if (Get2DPerlin(new(pos.x, pos.z), 0.0158f) > 0) {
                    if (new System.Random().Next(0, Mathf.FloorToInt(www) + 2) == 0) {
                        Queue<VoxelAndPos> a = new();
                        a.Enqueue(new(Data.Vector3ToChunkVoxel(pos + Vector3.one * 0.5f), type));
                        modifications.Enqueue(a);
                    }
                }

            }
            if (Get2DPerlin(new(pos.x, pos.z), biome.treeZoneScale) > biome.treeZoneThreshold) {
                if (Get2DPerlin(new(pos.x + 50, pos.z + 50), biome.treePlacementScale) > biome.treePlacementThreshold) {
                    lock (modifications) {

                        if (temp < -5f) {
                        } else if (temp < 20f) {
                            modifications.Enqueue(Structure.MakeTree(pos));
                        } else {
                            modifications.Enqueue(Structure.MakeCactus(pos));
                        }
                    }
                }
            }
        }

        return VoxelValue;

    }
    public bool IsEditable (Vector2Int pos) {
        return chunks.ContainsKey(pos) && chunks[pos].IsEditable;
    }
    public byte GetVoxelID (ChunkVoxel pos) {
        if (chunks.ContainsKey(pos.c) && chunks[pos.c].IsEditable)
            return chunks[pos.c].GetVoxelIDChunk(pos.v);
        else
            return 0;
    }
    public float GetTemp (Vector3 pos) {
        return (Get2DPerlin(new(pos.x, pos.z), 0.002f) + 0.5f) * 60 - 20;
    }
    public Vector3Int GetSpawnPoint () {
        Vector3Int pos = Vector3Int.zero;
        if (chunks.ContainsKey(Vector2Int.zero)) {
            for (int y = 0; y < Data.ChunkHeight; y++) {
                if (!blockTypes[GetVoxelID(new(Vector2Int.zero, new(0, y, 0)))].hasCollision) {
                    pos = new Vector3Int(0, y, 0);
                    break;
                }
            }
        }
        return pos;
    }
    public void AddMod (Queue<VoxelAndPos> aadd) {
        modifications.Enqueue(aadd);
    }

    private void ModifyChunks () {
        lock (modifications) {
            while (modifications.Count > 0) {
                Queue<VoxelAndPos> queue = modifications.Dequeue();
                while (queue.Count > 0) {
                    VoxelAndPos vmod = queue.Dequeue();
                    if (!chunks.ContainsKey(vmod.pos.c)) {
                        chunks.Add(vmod.pos.c, new(vmod.pos.c, this));
                    }
                    chunks[vmod.pos.c].EnqueueVoxelMod(vmod);
                    if (!chunksToUpdate.Contains(chunks[vmod.pos.c]))
                        chunksToUpdate.Add(chunks[vmod.pos.c]);
                }
            }
        }
    }
    private void UpdateChunks () {
        if (chunksToUpdate.Count > 0) {
            for (int i = chunksToUpdate.Count - 1; i >= 0; i--) {

                if (chunksToUpdate[i].IsEditable) {
                    chunksToUpdate[i].UpdateChunk();
                    chunksToDraw.Enqueue(chunksToUpdate[i]);
                    chunksToUpdate.RemoveAt(i);

                }
            }
        }
    }
    private void DrawChunks () {
        if (chunksToDraw.Count > 0 && chunksToDraw.Peek().IsEditable)
            chunksToDraw.Dequeue().GenerateMesh();
    }
}
[System.Serializable]
public class BlockType {
    public string blockName;
    public bool isSolid;
    public bool hasCollision;
    public float hardness;
    public Sprite sprite;
    public byte meshTypes;
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    public Vector3Int size;

    public int GetTextureID (int faceIndex) {
        return faceIndex switch {
            0 => backFaceTexture,
            1 => frontFaceTexture,
            2 => topFaceTexture,
            3 => bottomFaceTexture,
            4 => leftFaceTexture,
            5 => rightFaceTexture,
            _ => 0,
        };
    }
}