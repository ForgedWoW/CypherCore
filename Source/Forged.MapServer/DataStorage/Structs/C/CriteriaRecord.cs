using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CriteriaRecord
{
    public uint Id;
    public CriteriaType Type;
    public uint Asset;
    public uint ModifierTreeId;
    public byte StartEvent;
    public uint StartAsset;
    public ushort StartTimer;
    public byte FailEvent;
    public uint FailAsset;
    public byte Flags;
    public ushort EligibilityWorldStateID;
    public byte EligibilityWorldStateValue;

    public CriteriaFlags GetFlags() => (CriteriaFlags)Flags;
}