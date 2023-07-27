namespace Forged.MapServer.DataStorage.Structs.Q;

public sealed record QuestMoneyRewardRecord
{
    public uint Id;
    public uint[] Difficulty = new uint[10];
}