namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CharacterLoadoutRecord
{
    public uint Id;
    public long RaceMask;
    public sbyte ChrClassID;
    public int Purpose;
    public sbyte ItemContext;

    public bool IsForNewCharacter() { return Purpose == 9; }
}