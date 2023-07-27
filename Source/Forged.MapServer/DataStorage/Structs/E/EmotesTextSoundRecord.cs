namespace Forged.MapServer.DataStorage.Structs.E;

public sealed record EmotesTextSoundRecord
{
    public uint Id;
    public byte RaceId;
    public byte ClassId;
    public byte SexId;
    public uint SoundId;
    public uint EmotesTextId;
}