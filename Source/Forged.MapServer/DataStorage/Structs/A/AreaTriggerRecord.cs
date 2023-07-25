using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AreaTriggerRecord
{
    public Vector3 Pos;
    public uint Id;
    public ushort ContinentID;
    public sbyte PhaseUseFlags;
    public ushort PhaseID;
    public ushort PhaseGroupID;
    public float Radius;
    public float BoxLength;
    public float BoxWidth;
    public float BoxHeight;
    public float BoxYaw;
    public sbyte ShapeType;
    public short ShapeID;
    public int AreaTriggerActionSetID;
    public sbyte Flags;
}