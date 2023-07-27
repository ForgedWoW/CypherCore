namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AzeriteLevelInfoRecord
{
    public uint Id;
    public ulong BaseExperienceToNextLevel;
    public ulong MinimumExperienceToNextLevel;
    public uint ItemLevel;
}