using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GarrBuildingPlotInstRecord
{
    public Vector2 MapOffset;
    public uint Id;
    public byte GarrBuildingID;
    public ushort GarrSiteLevelPlotInstID;
    public ushort UiTextureAtlasMemberID;
}