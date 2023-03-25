// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.RealmServer.DataStorage;

public sealed class GarrSiteLevelRecord
{
	public uint Id;
	public Vector2 TownHallUiPos;
	public uint GarrSiteID;
	public byte GarrLevel;
	public ushort MapID;
	public ushort UpgradeMovieID;
	public ushort UiTextureKitID;
	public byte MaxBuildingLevel;
	public ushort UpgradeCost;
	public ushort UpgradeGoldCost;
}