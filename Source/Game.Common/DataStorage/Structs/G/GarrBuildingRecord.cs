// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Common.DataStorage.Structs.G;

public sealed class GarrBuildingRecord
{
	public uint Id;
	public string HordeName;
	public string AllianceName;
	public string Description;
	public string Tooltip;
	public byte GarrTypeID;
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
