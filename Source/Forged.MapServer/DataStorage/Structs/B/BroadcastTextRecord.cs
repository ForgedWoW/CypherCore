using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.B;

public sealed record BroadcastTextRecord
{
    public LocalizedString Text;
    public LocalizedString Text1;
    public uint Id;
    public int LanguageID;
    public int ConditionID;
    public ushort EmotesID;
    public byte Flags;
    public uint ChatBubbleDurationMs;
    public int VoiceOverPriorityID;
    public uint[] SoundKitID = new uint[2];
    public ushort[] EmoteID = new ushort[3];
    public ushort[] EmoteDelay = new ushort[3];
}