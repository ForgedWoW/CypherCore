using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellProcsPerMinuteModRecord
{
    public uint Id;
    public SpellProcsPerMinuteModType Type;
    public uint Param;
    public float Coeff;
    public uint SpellProcsPerMinuteID;
}