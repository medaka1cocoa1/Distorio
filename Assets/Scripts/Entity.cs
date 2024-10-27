using System.Collections.Generic;
using UnityEngine;

public abstract class Entity {

    private readonly MeshFilter meshFilter;
    private float width;
    private float height;
    private bool isCollidedVertically;
    private AABB boundingBox;

    private bool isDead;
    private protected bool isGrounded;
    private protected double gravityVelocity;
    private protected Vector3 velocity;
    private protected readonly GameObject playerObject;
    private protected readonly World world;
    private protected double posX;
    private protected double posY;
    private protected double posZ;

    public Entity (World world) {

        this.world = world;
        playerObject = new();
        playerObject.AddComponent<MeshRenderer>().material = world.material;
        meshFilter = playerObject.AddComponent<MeshFilter>();

        width = 0.6F;
        height = 1.8F;
        gravityVelocity = 0;
        velocity = Vector3.zero;
        SetPosition(0.0D, 0.0D, 0.0D);

        GenerateMesh(10);
    }

    public void UpdateIfNotDead () {
        if (!isDead) {
            Update();
        }
    }

    private void AddPosition (double x, double y, double z) {
        SetPosition(posX + x, posY + y, posZ + z);
    }
    private void MoveEntity (double _x, double _y, double _z) {
        double y = _y;
        List<AABB> AABBs = world.Ajj(boundingBox.AddCoord(_x, _y, _z));
        foreach (AABB value in AABBs) {
            _y = value.CalculateYOffset(boundingBox, _y);
        }
        AddPosition(0.0D, _y, 0.0D);
        foreach (AABB value in AABBs) {
            _x = value.CalculateXOffset(boundingBox, _x);
        }
        AddPosition(_x, 0.0D, 0.0D);
        foreach (AABB value in AABBs) {
            _z = value.CalculateZOffset(boundingBox, _z);
        }
        AddPosition(0.0D, 0.0D, _z);
        isCollidedVertically = y != _y;
        isGrounded = isCollidedVertically && y < 0.0D;
    }
    private void GenerateMesh (byte skin) {
        int vertexIndex = 0;
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();
        for (int p = 0; p < 6; p++) {
            for (int i = 0; i < 4; i++) {
                Vector3 a = Data.voxelVerts[Data.blockMesh[p, i]];
                a.x *= width;
                a.y *= height;
                a.z *= width;
                a -= new Vector3(width, 0, width) / 2;
                vertices.Add(a);
                uvs.Add((Data.voxelUVs[i] + Data.TexturePos(world.blockTypes[skin].GetTextureID(p))) / Data.TextureSize);
            }
            for (int i = 0; i < 6; i++)
                triangles.Add(Data.order[i] + vertexIndex);
            vertexIndex += 4;
        }
        Mesh mesh = new() {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;
    }
    private protected bool IsCollide (Vector3Int selectedPos) {
        return Data.ABCheck(new WWWEe(new((float)posX, (float)posY, (float)posZ), new(width, height, width), velocity, isGrounded), selectedPos + Vector3.one * 0.5f);
    }
    private protected void AddVelocity (Vector3 vel) {
        velocity += Data.playerSpeed * Time.deltaTime * vel;
    }
    private protected void Die () {
        isDead = true;
        playerObject.SetActive(false);
    }
    private protected void SetSize (float width, float height) {
        if (width != this.width || height != this.height) {
            this.width = width;
            this.height = height;

            float f = width / 2.0F;
            boundingBox = new(f, 0, f, f, height, f);
            GenerateMesh(10);
        }
    }
    private protected void SetPosition (double x, double y, double z) {
        posX = x;
        posY = y;
        posZ = z;
        float f = width / 2.0F;
        boundingBox = new(posX - f, posY, posZ - f, posX + f, posY + height, posZ + f);
        playerObject.transform.position = new((float)posX, (float)posY, (float)posZ);
    }
    private protected virtual void Update () {
        gravityVelocity += Data.gravityScale * Time.deltaTime;
        velocity += (float)gravityVelocity * Time.deltaTime * Vector3.down;
        MoveEntity(velocity.x, velocity.y, velocity.z);
        if (isGrounded) {
            OnLanded();
        }
        if (isCollidedVertically) {
            gravityVelocity = 0;
        }
        velocity = Vector3.zero;
    }
    private protected virtual void OnLanded () {
    }
}