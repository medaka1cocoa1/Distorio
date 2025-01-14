using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
public class Chunk {
    //Chunksからのみ利用される
    public bool IsEditable => IsTerrainMapGenerated && !threadLocked;
    public bool IsTerrainMapGenerated { get; private set; }
    public readonly Vector3Int chunkPos;
    private static readonly int ChunkWidth = 16;
    private static readonly int ChunkHeight = 128;
    private bool threadLocked;
    private bool ActiveState = true;
    private readonly MeshFilter meshFilter;
    private readonly GameObject chunkObject = new();
    private readonly Queue<VoxelAndPos> modifications = new();
    private readonly byte[,,] voxelMap = new byte[ChunkWidth, ChunkHeight, ChunkWidth];
    private readonly List<Vector3> vertices = new();
    private readonly List<Vector2> uvs = new();
    private readonly List<int> triangles = new();
    private readonly List<int> waterTriangles = new();
    private int vertexIndex = 0;
    public Chunk (Vector3Int posIn) {
        chunkPos = ChunkWidth * posIn;
        chunkObject.transform.position = chunkPos;
        chunkObject.AddComponent<MeshRenderer>().materials = MyResources.materials;
        meshFilter = chunkObject.AddComponent<MeshFilter>();
    }
    public void GenerateTerrainData () {
        threadLocked = true;
        new Thread(new ThreadStart(GenerateTerrainData)).Start();
        void GenerateTerrainData () {
            for (int x = 0; x < ChunkWidth; x++) {
                for (int y = 0; y < ChunkHeight; y++) {
                    for (int z = 0; z < ChunkWidth; z++) {
                        voxelMap[x, y, z] = Terrain.GetTerrain(x + chunkPos.x, y + chunkPos.y, z + chunkPos.z);
                        if (voxelMap[x, y, z] == 1) {
                            Chunks.PlantTrees(new(x + chunkPos.x, y + chunkPos.y, z + chunkPos.z));
                        }
                    }
                }
            }
            IsTerrainMapGenerated = true;
            threadLocked = false;
        }
    }
    public int GetVoxelIDChunk (Vector3Int pos) {
        return pos.x >= 0 && pos.x < ChunkWidth && pos.y >= 0 && pos.y < ChunkHeight && pos.z >= 0 && pos.z < ChunkWidth ? voxelMap[pos.x, pos.y, pos.z] : 0;
    }
    public void EnqueueVoxelMod (VoxelAndPos voxelMod) {
        modifications.Enqueue(voxelMod);
    }
    public void SetActiveState (bool value) {
        if (ActiveState != value) {
            ActiveState = value;
            chunkObject.SetActive(value);
        }
    }
    public void UpdateChunk () {
        lock (modifications) {
            while (modifications.Any()) {
                VoxelAndPos vmod = modifications.Dequeue();
                voxelMap[vmod.pos.v.x, vmod.pos.v.y, vmod.pos.v.z] = vmod.id;
            }
        }
    }
    public async void GenerateMesh () {
        threadLocked = true;
        await Task.Run(() => {
            vertexIndex = 0;
            vertices.Clear();
            triangles.Clear();
            waterTriangles.Clear();
            uvs.Clear();
            for (int x = 0; x < ChunkWidth; x++) {
                for (int y = 0; y < ChunkHeight; y++) {
                    for (int z = 0; z < ChunkWidth; z++) {
                        CreateMeshAt(x, y, z);
                    }
                }
            }
        });
        Mesh mesh = new() {
            subMeshCount = 2,
            vertices = vertices.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(waterTriangles.ToArray(), 1);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        threadLocked = false;
    }
    bool IsOutOfChunk (Vector3Int pos) {
        return pos.x < 0 || pos.x >= ChunkWidth || pos.y < 0 || pos.y >= ChunkHeight || pos.z < 0 || pos.z >= ChunkWidth;
    }
    private void CreateMeshAt (int x, int y, int z) {
        byte ttt = voxelMap[x, y, z];
        if (ttt != 0) {
            Vector3Int position = new(x, y, z);
            switch (MyResources.blockTypes[ttt].blockRenderType) {
                case BlockRenderType.standard:
                    for (int p = 0; p < 6; p++) {
                        Vector3Int a = faceChecks[p] + position;
                        int faceCheck = IsOutOfChunk(a) ? Chunks.GetVoxelID(a + chunkPos) : GetVoxelIDChunk(a);
                        if (ttt == 4) {
                            //ここを&&で結ぶとおかしくなる
                            if (faceCheck != 4) {
                                AAA();
                                for (int i = 0; i < 6; i++) {
                                    waterTriangles.Add(MyResources.order[i] + vertexIndex);
                                }
                                vertexIndex += 4;
                            }
                        } else if (!MyResources.blockTypes[faceCheck].isSolid) {
                            AAA();
                            for (int i = 0; i < 6; i++) {
                                triangles.Add(MyResources.order[i] + vertexIndex);
                            }
                            vertexIndex += 4;
                        }
                        void AAA () {
                            for (int i = 0; i < 4; i++) {
                                vertices.Add(MyResources.voxelVerts[MyResources.blockMesh[p, i]] + position);
                                uvs.Add((MyResources.voxelUVs[i] + MyResources.TexturePos(MyResources.blockTypes[ttt].backFaceTexture)) / MyResources.TextureSize);
                            }
                        }
                    }
                    break;
                case BlockRenderType.liquid:
                    for (int p = 0; p < MyResources.grassMesh.Length >> 2; p++) {
                        for (int i = 0; i < 4; i++) {
                            vertices.Add(MyResources.voxelVerts[MyResources.grassMesh[p, i]] + position);
                            uvs.Add((MyResources.voxelUVs[i] + MyResources.TexturePos(MyResources.blockTypes[ttt].backFaceTexture)) / MyResources.TextureSize);
                        }
                        for (int i = 0; i < 6; i++) {
                            triangles.Add(MyResources.order[i] + vertexIndex);
                        }
                        vertexIndex += 4;
                    }
                    break;
            }
        }
    }
    private static readonly Vector3Int[] faceChecks = new Vector3Int[6] {
        new(0, 0, -1),
        new(0, 0, 1),
        new(0, 1, 0),
        new(0, -1, 0),
        new(-1, 0, 0),
        new(1, 0, 0)
    };
}