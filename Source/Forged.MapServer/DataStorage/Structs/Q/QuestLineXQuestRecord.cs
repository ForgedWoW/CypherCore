namespace Forged.MapServer.DataStorage.Structs.Q;

public sealed record QuestLineXQuestRecord
{
    public uint Id;
    public uint QuestLineID;
    public uint QuestID;
    public uint OrderIndex;
    public int Flags;
}