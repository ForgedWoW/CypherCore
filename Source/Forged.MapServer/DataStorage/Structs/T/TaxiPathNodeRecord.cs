using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TaxiPathNodeRecord
{
    public Vector3 Loc;
    public uint Id;
    public ushort PathID;
    public int NodeIndex;
    public ushort ContinentID;
    public TaxiPathNodeFlags Flags;
    public uint Delay;
    public uint ArrivalEventID;
    public uint DepartureEventID;
}