using Framework.Constants;

namespace Forged.MapServer.LootManagement;

public class PlayerRollVote
{
    public RollVote Vote;
    public byte RollNumber;

    public PlayerRollVote()
    {
        Vote = RollVote.NotValid;
        RollNumber = 0;
    }
}
