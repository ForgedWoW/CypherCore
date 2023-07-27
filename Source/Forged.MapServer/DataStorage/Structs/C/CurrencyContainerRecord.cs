using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CurrencyContainerRecord
{
    public uint Id;
    public LocalizedString ContainerName;
    public LocalizedString ContainerDescription;
    public int MinAmount;
    public int MaxAmount;
    public int ContainerIconID;
    public sbyte ContainerQuality;
    public int OnLootSpellVisualKitID;
    public uint CurrencyTypesID;
}