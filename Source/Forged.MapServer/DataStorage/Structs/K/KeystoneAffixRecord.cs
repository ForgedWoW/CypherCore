using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.K;

public sealed record KeystoneAffixRecord
{
    public LocalizedString Name;
    public LocalizedString Description;
    public uint Id;
    public int FiledataID;
}