using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PowerTypeRecord
{
    public string NameGlobalStringTag;
    public string CostGlobalStringTag;
    public uint Id;
    public PowerType PowerTypeEnum;
    public int MinPower;
    public int MaxBasePower;
    public int CenterPower;
    public int DefaultPower;
    public int DisplayModifier;
    public int RegenInterruptTimeMS;
    public float RegenPeace;
    public float RegenCombat;
    public short Flags;
    
}