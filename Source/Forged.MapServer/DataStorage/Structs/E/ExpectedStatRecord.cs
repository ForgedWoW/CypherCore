namespace Forged.MapServer.DataStorage.Structs.E;

public sealed record ExpectedStatRecord
{
    public uint Id;
    public int ExpansionID;
    public float CreatureHealth;
    public float PlayerHealth;
    public float CreatureAutoAttackDps;
    public float CreatureArmor;
    public float PlayerMana;
    public float PlayerPrimaryStat;
    public float PlayerSecondaryStat;
    public float ArmorConstant;
    public float CreatureSpellDamage;
    public uint Lvl;
}