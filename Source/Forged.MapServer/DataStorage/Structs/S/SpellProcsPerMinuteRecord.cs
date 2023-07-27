namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellProcsPerMinuteRecord
{
    public uint Id;
    public float BaseProcRate;
    public byte Flags;
}