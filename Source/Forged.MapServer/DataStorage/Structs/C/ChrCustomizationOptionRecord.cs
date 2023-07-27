using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ChrCustomizationOptionRecord
{
    public LocalizedString Name;
    public uint Id;
    public ushort SecondaryID;
    public int Flags;
    public uint ChrModelID;
    public int SortIndex;
    public int ChrCustomizationCategoryID;
    public int OptionType;
    public float BarberShopCostModifier;
    public int ChrCustomizationID;
    public int ChrCustomizationReqID;
    public int UiOrderIndex;
    public int AddedInPatch;
}