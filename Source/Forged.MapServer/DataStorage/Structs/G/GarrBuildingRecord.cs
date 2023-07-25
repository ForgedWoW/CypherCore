using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrBuildingRecord
{
    public uint Id;
    public string HordeName;
    public string AllianceName;
    public string Description;
    public string Tooltip;
    public sbyte GarrTypeID;
    public sbyte BuildingType;
    public uint HordeGameObjectID;
    public uint AllianceGameObjectID;
    public int GarrSiteID;
    public byte UpgradeLevel;
    public int BuildSeconds;
    public ushort CurrencyTypeID;
    public int CurrencyQty;
    public ushort HordeUiTextureKitID;
    public ushort AllianceUiTextureKitID;
    public int IconFileDataID;
    public ushort AllianceSceneScriptPackageID;
    public ushort HordeSceneScriptPackageID;
    public int MaxAssignments;
    public byte ShipmentCapacity;
    public ushort GarrAbilityID;
    public ushort BonusGarrAbilityID;
    public ushort GoldCost;
    public GarrisonBuildingFlags Flags;
}