namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseRewardEntry
{
    public uint Id;
    public int Quantity;

    public PlayerChoiceResponseRewardEntry(uint id, int quantity)
    {
        Id = id;
        Quantity = quantity;
    }
}