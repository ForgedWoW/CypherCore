// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrBuildingRecord
{
    public uint AllianceGameObjectID;
    public string AllianceName;
    public ushort AllianceSceneScriptPackageID;
    public ushort AllianceUiTextureKitID;
    public ushort BonusGarrAbilityID;
    public sbyte BuildingType;
    public int BuildSeconds;
    public int CurrencyQty;
    public ushort CurrencyTypeID;
    public string Description;
    public GarrisonBuildingFlags Flags;
    public ushort GarrAbilityID;
    public int GarrSiteID;
    public byte GarrTypeID;
    public ushort GoldCost;
    public uint HordeGameObjectID;
    public string HordeName;
    public ushort HordeSceneScriptPackageID;
    public ushort HordeUiTextureKitID;
    public int IconFileDataID;
    public uint Id;
    public int MaxAssignments;
    public byte ShipmentCapacity;
    public string Tooltip;
    public byte UpgradeLevel;
}