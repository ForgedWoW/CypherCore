namespace Forged.MapServer.DataStorage.Structs.B;

public sealed class BarberShopStyleRecord
{
    public uint Id;
    public string DisplayName;
    public string Description;
    public byte Type; // value 0 . hair, value 2 . facialhair
    public float CostModifier;
    public byte Race;
    public byte Sex;
    public byte Data; // real ID to hair/facial hair
}