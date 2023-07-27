using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellRangeRecord
{
    public uint Id;
    public string DisplayName;
    public string DisplayNameShort;
    public SpellRangeFlag Flags;
    public float[] RangeMin = new float[2];
    public float[] RangeMax = new float[2];
}