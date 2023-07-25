using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrSiteLevelPlotInstRecord
{
    public uint Id;
    public Vector2 UiMarkerPos;
    public ushort GarrSiteLevelID;
    public byte GarrPlotInstanceID;
    public byte UiMarkerSize;
}