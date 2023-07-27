namespace Forged.MapServer.DataStorage.Structs.Q;

public sealed record QuestXPRecord
{
    public uint Id;
    public ushort[] Difficulty = new ushort[10];
}