using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CriteriaRecord
{
    public uint Id;
    public CriteriaType Type;
    public uint Asset;
    public uint ModifierTreeId;
    public int StartEvent;
    public uint StartAsset;
    public ushort StartTimer;
    public int FailEvent;
    public uint FailAsset;
    public int Flags;
    public ushort EligibilityWorldStateID;
    public byte EligibilityWorldStateValue;

    public CriteriaFlags GetFlags() => (CriteriaFlags)Flags;
}