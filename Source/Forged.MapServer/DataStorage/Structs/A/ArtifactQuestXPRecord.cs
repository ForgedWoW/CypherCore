namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record ArtifactQuestXPRecord
{
    public uint Id;
    public uint[] Difficulty = new uint[10];
}