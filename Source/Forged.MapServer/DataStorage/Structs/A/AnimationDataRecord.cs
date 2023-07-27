namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AnimationDataRecord
{
    public uint Id;
    public ushort Fallback;
    public byte BehaviorTier;
    public int BehaviorID;
    public int[] Flags = new int[2];
}