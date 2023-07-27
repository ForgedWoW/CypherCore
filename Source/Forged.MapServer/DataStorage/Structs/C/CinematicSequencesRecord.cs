namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CinematicSequencesRecord
{
    public uint Id;
    public uint SoundID;
    public ushort[] Camera = new ushort[8];
}