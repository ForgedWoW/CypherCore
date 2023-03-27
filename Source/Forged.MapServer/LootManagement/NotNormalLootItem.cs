namespace Forged.MapServer.LootManagement;

public class NotNormalLootItem
{
    public byte LootListId; // position in quest_items or items;
    public bool IsLooted;

    public NotNormalLootItem()
    {
        LootListId = 0;
        IsLooted = false;
    }

    public NotNormalLootItem(byte index, bool islooted = false)
    {
        LootListId = index;
        IsLooted = islooted;
    }
}
