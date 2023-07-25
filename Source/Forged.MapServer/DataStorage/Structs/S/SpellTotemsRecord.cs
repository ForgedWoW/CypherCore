using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellTotemsRecord
{
    public uint Id;
    public uint SpellID;
    public ushort[] RequiredTotemCategoryID = new ushort[SpellConst.MaxTotems];
    public uint[] Totem = new uint[SpellConst.MaxTotems];
}