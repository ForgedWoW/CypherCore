using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UiMapLinkRecord
{
    public Vector2 UiMin;
    public Vector2 UiMax;
    public uint Id;
    public int ParentUiMapID;
    public int OrderIndex;
    public int ChildUiMapID;
    public int PlayerConditionID;
    public int OverrideHighlightFileDataID;
    public int OverrideHighlightAtlasID;
    public int Flags;
}