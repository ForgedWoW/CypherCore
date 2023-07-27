using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemSetRecord
{
    public uint Id;
    public LocalizedString Name;
    public ItemSetFlags SetFlags;
    public uint RequiredSkill;
    public ushort RequiredSkillRank;
    public uint[] ItemID = new uint[ItemConst.MaxItemSetItems];
}