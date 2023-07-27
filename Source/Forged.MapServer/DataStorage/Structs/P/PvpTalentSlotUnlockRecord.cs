namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PvpTalentSlotUnlockRecord
{
    public uint Id;
    public sbyte Slot;
    public uint LevelRequired;
    public uint DeathKnightLevelRequired;
    public uint DemonHunterLevelRequired;
}