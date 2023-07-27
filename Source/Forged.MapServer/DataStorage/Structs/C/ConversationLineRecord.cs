namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ConversationLineRecord
{
    public uint Id;
    public uint BroadcastTextID;
    public uint SpellVisualKitID;
    public int AdditionalDuration;
    public ushort NextConversationLineID;
    public ushort AnimKitID;
    public byte SpeechType;
    public byte StartAnimation;
    public byte EndAnimation;
}