using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellProcsPerMinuteModRecord
{
    public uint Id;
    public SpellProcsPerMinuteModType Type;
    public uint Param;
    public float Coeff;
    public uint SpellProcsPerMinuteID;
}