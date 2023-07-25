using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.W;

public sealed class WorldMapOverlayRecord
{
    public uint Id;
    public uint UiMapArtID;
    public ushort TextureWidth;
    public ushort TextureHeight;
    public int OffsetX;
    public int OffsetY;
    public int HitRectTop;
    public int HitRectBottom;
    public int HitRectLeft;
    public int HitRectRight;
    public uint PlayerConditionID;
    public uint Flags;
    public uint[] AreaID = new uint[SharedConst.MaxWorldMapOverlayArea];
}