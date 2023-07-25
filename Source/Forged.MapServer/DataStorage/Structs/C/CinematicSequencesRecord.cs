namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CinematicSequencesRecord
{
    public uint Id;
    public uint SoundID;
    public ushort[] Camera = new ushort[8];
}