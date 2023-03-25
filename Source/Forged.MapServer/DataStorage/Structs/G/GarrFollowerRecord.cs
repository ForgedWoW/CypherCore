// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrFollowerRecord
{
	public uint Id;
	public string HordeSourceText;
	public string AllianceSourceText;
	public string TitleName;
	public byte GarrTypeID;
	public sbyte GarrFollowerTypeID;
	public int HordeCreatureID;
	public int AllianceCreatureID;
	public byte HordeGarrFollRaceID;
	public byte AllianceGarrFollRaceID;
	public uint HordeGarrClassSpecID;
	public uint AllianceGarrClassSpecID;
	public sbyte Quality;
	public byte FollowerLevel;
	public ushort ItemLevelWeapon;
	public ushort ItemLevelArmor;
	public sbyte HordeSourceTypeEnum;
	public sbyte AllianceSourceTypeEnum;
	public int HordeIconFileDataID;
	public int AllianceIconFileDataID;
	public ushort HordeGarrFollItemSetID;
	public ushort AllianceGarrFollItemSetID;
	public ushort HordeUITextureKitID;
	public ushort AllianceUITextureKitID;
	public byte Vitality;
	public byte HordeFlavorGarrStringID;
	public byte AllianceFlavorGarrStringID;
	public uint HordeSlottingBroadcastTextID;
	public uint AllySlottingBroadcastTextID;
	public byte ChrClassID;
	public int Flags;
	public byte Gender;
	public int AutoCombatantID;
	public int CovenantID;
}