using EMullen.Core.PlayerMgmt;
using EMullen.PlayerMgmt;

public class InventoryData : PlayerDatabaseDataClass 
{
    public static readonly int HOTBAR_SIZE = 3;

    public string _uid;
    public override string UID => _uid;
    public Item[] hotbarItems;

    public InventoryData() {}

    public InventoryData(string uid, Item[] hotbarItems) 
    {
        this._uid = uid;
        this.hotbarItems = hotbarItems;
    }

    /// <summary>
    /// Place an item in the hotbar, may not execute fully see returns boolean variable.
    /// </summary>
    /// <param name="item">The item to place in the hotbar</param>
    /// <returns>Boolean success status, true if the item was placed false if not</returns>
    public bool AddItemToHotbar(Item item) 
    {
        for(int i = 0; i < hotbarItems.Length; i++) {
            if(hotbarItems[i] == Item.NONE) {
                hotbarItems[i] = item;
                return true;
            }
        }
        
        return false;
    }

    public static Item[] CreateHotbar(int size) 
    {
        Item[] hotbar = new Item[size];
        for(int i = 0; i < hotbar.Length; i++)
            hotbar[i] = Item.NONE;
        return hotbar;
    }
    public static InventoryData CreateDefault(string uid) => new InventoryData(uid, CreateHotbar(HOTBAR_SIZE));
}