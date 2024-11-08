using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class World : MonoBehaviour {


    public GameObject particle;
    public PathRenderer pathrend;
    public Slider hpBar;
    public Text hpText;
    public AudioClip gunSound;
    public BiomeAttributes biome;
    public Material material;
    public List<BlockType> blockTypes = new();
    public List<ItemType> itemTypes = new();
    public List<Entity> entities = new();
    public Transform backGround;
    public Transform cam;
    public Hand hand;
    public GameObject miningProgresBarObj;
    public UserInterface userInter;
    public GameObject blockHighlight;
    public GameObject miningEffect; public Slider miningProgresBar;
    private Vector2 offset;
    private readonly Dictionary<Vector2Int, Chunk> chunks = new();
    private readonly Queue<Queue<VoxelAndPos>> modifications = new();
    private readonly List<Chunk> chunksToUpdate = new();
    private readonly Queue<Chunk> chunksToDraw = new();

    private void Awake () {

        Item.RegisterItems();
        InitNoise();

        GenerateWorld();
        ModifyChunks();
        UpdateChunks();


    }

    private void Start () {

        userInter.inventory = new();

        entities.Add(new EntityPlayer(this, cam, GetSpawnPoint()));
        entities.Add(new EntityEnemy(this));

    }
    private void LateUpdate () {

        foreach (Entity entity in entities) {
            entity.UpdateIfNotDead();
        }

        ModifyChunks();
        UpdateChunks();
        if (chunksToDraw.Count > 0 && chunksToDraw.Peek().IsEditable) {
            chunksToDraw.Dequeue().GenerateMesh();
        }
    }

    public void CheckViewDistance (Vector2Int playerPos) {
        List<Vector2Int> previouslyActiveChunks = chunks.Keys.ToList();
        for (int x = playerPos.x - Data.ChunkLoadRange; x < playerPos.x + Data.ChunkLoadRange; x++) {
            for (int y = playerPos.y - Data.ChunkLoadRange; y < playerPos.y + Data.ChunkLoadRange; y++) {
                Vector2Int pos = new(x, y);
                if (!chunks.ContainsKey(pos)) {
                    chunks.Add(pos, new(pos, this));
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



    public byte GetVoxel (Vector3Int pos) {

        byte VoxelValue = 0;
        int terrainHeight = Mathf.FloorToInt(GetHeight(new(pos.x, pos.z)));

        switch (pos.y - terrainHeight) {
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

        if (pos.y == terrainHeight) {
            lock (modifications) {
                if (Noise.Get2DPerlin(new(pos.x, pos.z), 0.0158f) > 0) {

                    float www = Mathf.Max(0, Noise.Get2DPerlin(new(pos.x, pos.z), 0.052f) + 0.5f);

                    if (new System.Random().Next(0, Mathf.FloorToInt(www * 32) + 2) == 0) {
                        Queue<VoxelAndPos> a = new();
                        a.Enqueue(new(Data.Vector3ToChunkVoxel(pos), 15));
                        modifications.Enqueue(a);
                    }
                }
                if (Noise.Get2DPerlin(new(pos.x, pos.z), biome.treeZoneScale) > biome.treeZoneThreshold) {
                    if (Noise.Get2DPerlin(new(pos.x + 50, pos.z + 50), biome.treePlacementScale) > biome.treePlacementThreshold) {
                        modifications.Enqueue(Structure.MakeTree(pos));
                    }
                }
            }
        }

        //superFlat
        //VoxelValue = pos.y > 4 ? (byte)0 : (byte)1;

        return VoxelValue;
    }
    public bool SetBlock (Vector3 position, Vector3 selectingPos) {

        if (userInter.selectedBlockIndex != 0 && !blockTypes[GetVoxelID(position)].hasCollision && blockTypes[GetVoxelID(selectingPos)].hasCollision) {

            Queue<VoxelAndPos> queue = new();
            queue.Enqueue(new(Data.Vector3ToChunkVoxel(position), userInter.selectedBlockIndex));
            AddMod(queue);

            hand.placeEase = 0;
            return true;
        }
        return false;
    }
    public bool IsEditable (Vector2Int pos) {
        return chunks.ContainsKey(pos) && chunks[pos].IsEditable;
    }
    public int GetVoxelID (Vector3 position) {

        ChunkVoxel pos = Data.Vector3ToChunkVoxel(position);

        if (chunks.ContainsKey(pos.c) && chunks[pos.c].IsEditable)
            return chunks[pos.c].GetVoxelIDChunk(pos.v);
        else
            return 0;
    }
    public bool GetInventory (Vector3 position) {


        ChunkVoxel pos = Data.Vector3ToChunkVoxel(position);
        return chunks.ContainsKey(pos.c) && chunks[pos.c].IsEditable && chunks[pos.c].GetInventory(pos.v);

    }
    public List<AABB> Ajj (AABB aaa) {
        List<AABB> a = new();
        Vector3 ttt = new((float)aaa.minX, (float)aaa.minY, (float)aaa.minZ);
        for (int __X = -2; __X < 2; __X++) {
            for (int __Y = -2; __Y < 3; __Y++) {
                for (int __Z = -2; __Z < 2; __Z++) {
                    Vector3Int pos = Vector3Int.FloorToInt(ttt + new Vector3Int(__X, __Y, __Z));
                    if (blockTypes[GetVoxelID(pos)].hasCollision) {
                        a.Add(new(pos.x, pos.y, pos.z, pos.x + 1, pos.y + 1, pos.z + 1));
                    }
                }
            }
        }
        return a;
    }
    public Vector3 GetSpawnPoint () {
        if (chunks.ContainsKey(Vector2Int.zero)) {
            for (int y = 0; y < Data.ChunkHeight; y++) {
                if (!blockTypes[GetVoxelID(new(0, y, 0))].hasCollision) {

                    return new Vector3Int(0, y, 0) + new Vector3(0.5f, 0, 0.5f);

                }
            }
        }
        return Vector3.zero;
    }
    public void AddMod (Queue<VoxelAndPos> aadd) {
        modifications.Enqueue(aadd);
    }

    private float GetTemp (Vector3 pos) {
        return (Noise.Get2DPerlin(new(pos.x, pos.z), 0.002f) + 0.5f) * 60 - 20;
    }
    private float GetHeight (Vector2Int pos) {

        float terrainHeight = 0;
        for (int i = 0; i < 4; i++) {
            terrainHeight += Noise.Get2DPerlin(pos, biome.terrainScale / Mathf.Pow(2, i));
        }
        return terrainHeight * biome.terrainHeight * (Mathf.Pow(2, Noise.Get2DPerlin(pos, 0.029f) * 4) + Mathf.Pow(2, Noise.Get2DPerlin(pos, 0.005f) * 4) / 2) / (Noise.Get2DPerlin(pos, 0.05f) / 5000 + 1) + biome.solidGroundHeight;
    }
    private void InitNoise () {
        Random.InitState((int)System.DateTime.Now.Ticks);
        offset = new(Random.Range(-66666.6f, 66666.6f), Random.Range(-66666.6f, 66666.6f));
        Noise.SetOffset(offset);
    }
    private void GenerateWorld () {
        for (int x = -Data.ChunkLoadRange; x < Data.ChunkLoadRange; x++) {
            for (int y = -Data.ChunkLoadRange; y < Data.ChunkLoadRange; y++) {
                Vector2Int pos = new(x, y);
                chunks.Add(pos, new(pos, this));
                chunksToUpdate.Add(chunks[pos]);
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
}
[System.Serializable]
public class BlockType {
    public string blockName;
    public bool isSolid;
    public bool hasCollision;
    public bool hasInventory;
    public float hardness;
    public Sprite sprite;
    public byte meshTypes;
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;
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