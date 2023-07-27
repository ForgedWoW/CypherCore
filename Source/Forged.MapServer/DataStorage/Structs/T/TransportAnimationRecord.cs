using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TransportAnimationRecord
{
    public uint Id;
    public Vector3 Pos;
    public byte SequenceID;
    public uint TimeIndex;
    public uint TransportID;
}