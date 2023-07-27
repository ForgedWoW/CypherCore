namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellCooldownsRecord
{
    public uint Id;
    public byte DifficultyID;
    public uint CategoryRecoveryTime;
    public uint RecoveryTime;
    public uint StartRecoveryTime;
    public uint AuraSpellID;
    public uint SpellID;
}