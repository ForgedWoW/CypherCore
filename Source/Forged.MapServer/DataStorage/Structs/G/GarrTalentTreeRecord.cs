namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GarrTalentTreeRecord
{
    public uint Id;
    public string Name;
    public sbyte GarrTypeID;
    public int ClassID;
    public sbyte MaxTiers;
    public sbyte UiOrder;
    public int Flags;
    public ushort UiTextureKitID;
    public int GarrTalentTreeType;
    public int PlayerConditionID;
    public byte FeatureTypeIndex;
    public sbyte FeatureSubtypeIndex;
    public int CurrencyID;
}