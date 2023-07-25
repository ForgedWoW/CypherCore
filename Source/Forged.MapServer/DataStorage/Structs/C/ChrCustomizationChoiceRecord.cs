using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class ChrCustomizationChoiceRecord
{
    public LocalizedString Name;
    public uint Id;
    public uint ChrCustomizationOptionID;
    public uint ChrCustomizationReqID;
    public int ChrCustomizationVisReqID;
    public ushort SortOrder;
    public ushort UiOrderIndex;
    public int Flags;
    public int AddedInPatch;
    public int SoundKitID;
    public int[] SwatchColor = new int[2];
}