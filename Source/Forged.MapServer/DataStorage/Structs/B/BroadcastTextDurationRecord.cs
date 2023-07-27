namespace Forged.MapServer.DataStorage.Structs.B;

public sealed record BroadcastTextDurationRecord
{
    public uint Id;
    public int BroadcastTextID;
    public int Locale;
    public int Duration;
}