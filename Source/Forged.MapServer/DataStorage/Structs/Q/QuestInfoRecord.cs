using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.Q;

public sealed record QuestInfoRecord
{
    public uint Id;
    public LocalizedString InfoName;
    public sbyte Type;
    public int Modifiers;
    public ushort Profession;
}