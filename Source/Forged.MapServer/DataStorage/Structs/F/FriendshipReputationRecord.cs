using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.F;

public sealed class FriendshipReputationRecord
{
    public LocalizedString Description;
    public LocalizedString StandingModified;
    public LocalizedString StandingChanged;
    public uint Id;
    public int FactionID;
    public int TextureFileID;
    public FriendshipReputationFlags Flags;
}