using System.Collections.Generic;
using UnityEngine;

public class EntityPlayer : EntityLiving {
    private Vector2Int chunkCoord;
    private Vector2Int lastChunkCoord;
    private Vector3Int tryPlacingPos;
    private Vector3Int lastTryPlacingPos = Vector3Int.zero;
    private Vector3Int SelectingPos;
    private Vector3Int miningPos;
    private readonly Transform camTransform;
    private readonly Camera cam;
    private float miningProgress;
    private readonly float mineSpeed = 2.5f;
    private float coolDown = 0;
    private bool nextFramePlaced;
    public EntityPlayer (World world) : base(world) {
        Vector3 pos = world.GetSpawnPoint();
        SetPosition(pos.x, pos.y, pos.z);
        cam = world.camObj;
        camTransform = world.cam;
    }
    private protected override void Update () {
        Vector3 pos = world.GetSpawnPoint();
        if (Input.GetKeyDown(KeyCode.R)) {
            SetPosition(pos.x, pos.y, pos.z);
            SetVelocity(0.0D, 0.0D, 0.0D);
        }
        ApplyFieldOfView();
        ApplyRotation();
        Vector3 iii = PlayerVel() * Time.deltaTime;
        AddVelocity(iii.x, iii.y, iii.z);
        if (Input.GetKey(KeyCode.Space) && isGrounded) {
            AddVelocity(0, GetJumpPower(), 0);
        }
        base.Update();
        camTransform.SetPositionAndRotation(new((float)posX, (float)posY + GetEyeHeight(), (float)posZ), Quaternion.Euler(rotationPitch, rotationYaw, 0));
        chunkCoord = Data.Vector3ToChunkVoxel(new((float)posX, (float)posY, (float)posZ)).c;
        if (chunkCoord != lastChunkCoord) {
            lastChunkCoord = chunkCoord;
            world.CheckViewDistance(chunkCoord);
        }
        if (IsTouching(15)) {
            AddHealth(3 * Time.deltaTime);
        }
        CalculateSelectingPos();
        bool isMining = Input.GetMouseButton(0) && world.blockTypes[world.GetVoxelID(SelectingPos)].hasCollision;
        if (isMining) {
            if (miningPos != SelectingPos) {
                miningPos = SelectingPos;
                miningProgress = 0;
            }
            miningProgress += Time.deltaTime * mineSpeed;
            if (miningProgress >= world.blockTypes[world.GetVoxelID(SelectingPos)].hardness) {
                DestroyBlock(SelectingPos);
            }
            world.miningProgresBar.value = miningProgress / world.blockTypes[world.GetVoxelID(SelectingPos)].hardness;
        }
        if (nextFramePlaced) {
            lastTryPlacingPos = tryPlacingPos;
            nextFramePlaced = false;
        }
        coolDown += Time.deltaTime;
        if (!IsCollide(tryPlacingPos) && (Input.GetMouseButton(1) && coolDown >= 0.3f || Input.GetMouseButtonDown(1) || Input.GetMouseButton(1) && tryPlacingPos != lastTryPlacingPos)) {
            if (world.SetBlock(tryPlacingPos, SelectingPos)) {
                coolDown = 0;
                lastTryPlacingPos = tryPlacingPos;
                nextFramePlaced = true;
            }
        }
        world.blockHighlight.transform.position = SelectingPos + Vector3.one * 0.5f;
        world.miningEffect.transform.position = SelectingPos + Vector3.one * 0.5f;
        //world.miningEffect.SetActive(isMining);
        world.miningProgresBarObj.SetActive(isMining);
        world.blockHighlight.SetActive(world.blockTypes[world.GetVoxelID(SelectingPos)].hasCollision);
        world.hpBar.value = health / maxHealth;
        void DestroyBlock (Vector3 position) {
            ChunkVoxel pos = Data.Vector3ToChunkVoxel(position);
            Queue<VoxelAndPos> queue = new();
            queue.Enqueue(new(pos, 0));
            world.AddMod(queue);
        }
        float GetJumpPower () {
            return Mathf.Sqrt(2 * Data.gravityScale * (Data.jumpScale + 0.4F));
        }
        void ApplyRotation () {
            rotationPitch -= Data.mouseSens * Input.GetAxisRaw("Mouse Y") * Time.deltaTime;
            rotationYaw += Data.mouseSens * Input.GetAxisRaw("Mouse X") * Time.deltaTime;
            rotationPitch = Mathf.Clamp(rotationPitch, -90, 90);
        }
        void ApplyFieldOfView () {
            if (Input.GetKey(KeyCode.C)) {
                cam.fieldOfView = 20;
            } else {
                cam.fieldOfView = 70;
            }
        }
        bool IsTouching (int check) {
            List<int> ids = CollidingIDs();
            foreach (int id in ids) {
                if (id == check) {
                    return true;
                }
            }
            return false;
        }
        float GetEyeHeight () {
            float value = 1.62F;
            if (Input.GetKey(KeyCode.LeftShift)) {
                value -= 0.08F;
            }
            return value;
        }
        void CalculateSelectingPos () {
            Vector3 _camPos = camTransform.position;
            for (int i = 0; i < 300; i++) {
                if (world.blockTypes[world.GetVoxelID(_camPos)].hasCollision) {
                    break;
                }
                _camPos += camTransform.forward * 0.02f;
            }
            SelectingPos = Vector3Int.FloorToInt(_camPos);
            tryPlacingPos = Vector3Int.FloorToInt(CalculateNormal(_camPos) + _camPos);
            Vector3Int CalculateNormal (Vector3 pp) {
                Vector3Int normal = Vector3Int.zero;
                Vector3 p = new Vector3(pp.x < 0 ? 1 : 0, pp.y < 0 ? 1 : 0, pp.z < 0 ? 1 : 0) + new Vector3(pp.x % 1, pp.y % 1, pp.z % 1) - Vector3.one * 0.5f;
                Vector3 v = new(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
                if (v.x < v.z && v.y < v.z) {
                    normal += Vector3Int.forward * Mathf.RoundToInt(Mathf.Sign(p.z));
                } else if (v.x < v.y) {
                    normal += Vector3Int.up * Mathf.RoundToInt(Mathf.Sign(p.y));
                } else if (v.x > v.y) {
                    normal += Vector3Int.right * Mathf.RoundToInt(Mathf.Sign(p.x));
                }
                return normal;
            }
        }
        Vector3 PlayerVel () {
            return (isGrounded ? Data.resistance : Data.resistance * 0.2F) * (Quaternion.Euler(0, rotationYaw, 0) * PlayerInput() * Data.playerSpeed - new Vector3((float)motionX, 0, (float)motionZ));
            Vector3 PlayerInput () {
                int x = 0;
                int z = 0;
                float dash = 0;
                float shift = 1;
                if (Input.GetKey(KeyCode.D))
                    x++;
                if (Input.GetKey(KeyCode.A))
                    x--;
                if (Input.GetKey(KeyCode.W))
                    z++;
                if (Input.GetKey(KeyCode.S))
                    z--;
                if (z == 1 && Input.GetKey(KeyCode.LeftControl))
                    dash = 1.0F / 3.0F;
                if (Input.GetKey(KeyCode.LeftShift))
                    shift = 0.4F;
                return (new Vector3(x, 0, z).normalized + dash * Vector3.forward) * shift;
            }
        }
    }
    private protected override void OnGrounded () {
        AddHealth(Mathf.Min(0, 13 + (float)motionY));
    }
}
