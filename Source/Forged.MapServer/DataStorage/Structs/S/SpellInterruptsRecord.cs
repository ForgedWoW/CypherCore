namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellInterruptsRecord
{
    public uint Id;
    public byte DifficultyID;
    public short InterruptFlags;
    public int[] AuraInterruptFlags = new int[2];
    public int[] ChannelInterruptFlags = new int[2];
    public uint SpellID;
}