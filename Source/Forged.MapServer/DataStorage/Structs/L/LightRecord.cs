using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed record LightRecord
{
    public uint Id;
    public Vector3 GameCoords;
    public float GameFalloffStart;
    public float GameFalloffEnd;
    public short ContinentID;
    public ushort[] LightParamsID = new ushort[8];
}