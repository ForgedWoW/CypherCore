// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GarrSiteLevelRecord
{
    public byte GarrLevel;
    public uint GarrSiteID;
    public uint Id;
    public ushort MapID;
    public byte MaxBuildingLevel;
    public Vector2 TownHallUiPos;
    public ushort UiTextureKitID;
    public ushort UpgradeCost;
    public ushort UpgradeGoldCost;
    public ushort UpgradeMovieID;
}