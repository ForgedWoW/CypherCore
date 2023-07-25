using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CurvePointRecord
{
    public Vector2 Pos;
    public Vector2 PreSLSquishPos;
    public uint Id;
    public uint CurveID;
    public byte OrderIndex;
}