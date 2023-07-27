using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GarrSiteLevelRecord
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