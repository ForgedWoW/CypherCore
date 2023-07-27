namespace Forged.MapServer.DataStorage.Structs.D;

public sealed record DurabilityCostsRecord
{
    public uint Id;
    public ushort[] WeaponSubClassCost = new ushort[21];
    public ushort[] ArmorSubClassCost = new ushort[8];
}