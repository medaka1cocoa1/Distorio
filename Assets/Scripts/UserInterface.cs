using UnityEngine;
using UnityEngine.UI;

public class UserInterface : MonoBehaviour {

    public Inventory inventory;
    public int selectedBlockIndex = 0;
    public Sprite slot;
    public Sprite nothing;
    public GameObject inventoryBackGround;
    public GameObject toolbarBackGround;
    public GameObject toolbar;
    public GameObject inventoryObj;
    public World world;
    public RectTransform highlight;
    public readonly Image[] images = new Image[Data.InventoryWidth * Data.InventoryHeight];
    public readonly Image[] toolbarImages = new Image[Data.InventoryWidth];
    public int slotIndex = 0;
    public bool inUI;

    [SerializeField] public Text blockName;
    [SerializeField] public Hand hand;

    private void Start () {

        hand = GameObject.Find("Hand").GetComponent<Hand>();

        inventory.AddItemStackToInventory(new ItemStack(256, 1));
        inventory.AddItemStackToInventory(new ItemStack(257, 1));
        inventory.AddItemStackToInventory(new ItemStack(258, 1));
        inventory.AddItemStackToInventory(new ItemStack(259, 1));
        inventory.AddItemStackToInventory(new ItemStack(0, 4));
        inventory.AddItemStackToInventory(new ItemStack(1, 4));
        inventory.AddItemStackToInventory(new ItemStack(2, 4));
        inventory.AddItemStackToInventory(new ItemStack(3, 4));

        Cursor.lockState = CursorLockMode.Locked;
        SetSprites();
        inventoryBackGround.SetActive(false);
        inventoryObj.SetActive(false);
        highlight.position = toolbarImages[0].transform.position;
        RenderSprites();
        selectedBlockIndex = GetSelected();
    }
    private int GetSelected () {
        return inventory.GetItemStack(slotIndex).id;
    }
    private void Update () {


        Scroll();
        SetSlidebarValues();

        if (Input.GetKeyDown(KeyCode.E)) {
            inUI = false;
            inventoryBackGround.SetActive(false);
            inventoryObj.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    private void SetSlidebarValues () {
    }
    private void Scroll () {
        float value = Input.GetAxis("Mouse ScrollWheel");
        if (value != 0) {
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
                slotIndex--;
            else
                slotIndex++;

            if (slotIndex >= Data.InventoryWidth)
                slotIndex = 0;
            else if (slotIndex < 0)
                slotIndex = Data.InventoryWidth - 1;

            if(inventory.GetItemStack(GetSelected()) != null) {
                selectedBlockIndex = inventory.GetItemStack(GetSelected()).id;
                hand.GenerateMesh(0);
            }
            highlight.position = toolbarImages[slotIndex].transform.position;
            selectedBlockIndex = GetSelected();
        }
    }
    private void SetSprites () {

        int f = 0;

        for (int x = 0; x < Data.InventoryWidth; x++) {
            for (int y = 0; y < Data.InventoryHeight; y++) {


                Vector2 pos = new Vector2(100 * x - 450, -100 * y + 10) + new Vector2(1060, 540);

                GameObject obj = new();
                obj.AddComponent<Image>().sprite = slot;
                obj.layer = 5;
                obj.transform.SetParent(inventoryBackGround.transform);
                obj.GetComponent<RectTransform>().position = pos;

                GameObject a = new();
                images[f] = a.AddComponent<Image>();
                images[f].sprite = nothing;
                a.layer = 5;
                a.transform.SetParent(inventoryObj.transform);
                a.GetComponent<RectTransform>().position = pos;
                a.GetComponent<RectTransform>().sizeDelta *= 0.8f;

                f++;
            }
        }
        for (int i = 0; i < Data.InventoryWidth; i++) {


            Vector2 pos = new Vector2(100 * i - 450, -500 + 10) + new Vector2(1060, 540);

            GameObject obj = new();
            obj.AddComponent<Image>().sprite = slot;
            obj.layer = 5;
            obj.transform.SetParent(toolbarBackGround.transform);
            obj.GetComponent<RectTransform>().position = pos;

            GameObject a = new();
            toolbarImages[i] = a.AddComponent<Image>();
            toolbarImages[i].sprite = nothing;
            a.layer = 5;
            a.transform.SetParent(toolbar.transform);
            a.GetComponent<RectTransform>().position = pos;
            a.GetComponent<RectTransform>().sizeDelta *= 0.8f;
        }
    }
    private void RenderSprites () {

        for (int i = 0; i < Data.InventoryWidth * Data.InventoryHeight; i++) {

            Sprite a = nothing;

            if (inventory.GetItemStack(i) != null) {

                a = world.itemTypes[inventory.GetItemStack(i).id - 256].sprite;

                if (i < 8) {
                    toolbarImages[i].sprite = a;
                }

            }
            images[i].sprite = a;
        }
    }
}
