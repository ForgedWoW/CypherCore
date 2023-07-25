using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.F;

public sealed class FriendshipRepReactionRecord
{
    public uint Id;
    public LocalizedString Reaction;
    public uint FriendshipRepID;
    public ushort ReactionThreshold;
    public int OverrideColor;
}