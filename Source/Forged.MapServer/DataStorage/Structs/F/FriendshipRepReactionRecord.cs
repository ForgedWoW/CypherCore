using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.F;

public sealed record FriendshipRepReactionRecord
{
    public uint Id;
    public LocalizedString Reaction;
    public uint FriendshipRepID;
    public ushort ReactionThreshold;
    public int OverrideColor;
}