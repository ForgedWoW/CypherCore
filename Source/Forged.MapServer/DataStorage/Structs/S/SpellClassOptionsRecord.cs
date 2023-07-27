using Framework.Dynamic;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellClassOptionsRecord
{
    public uint Id;
    public uint SpellID;
    public uint ModalNextSpell;
    public byte SpellClassSet;
    public FlagArray128 SpellClassMask;
}