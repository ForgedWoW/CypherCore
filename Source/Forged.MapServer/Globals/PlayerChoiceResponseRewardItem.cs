using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseRewardItem
{
    public uint Id;
    public List<uint> BonusListIDs = new();
    public int Quantity;
    public PlayerChoiceResponseRewardItem() { }

    public PlayerChoiceResponseRewardItem(uint id, List<uint> bonusListIDs, int quantity)
    {
        Id = id;
        BonusListIDs = bonusListIDs;
        Quantity = quantity;
    }
}