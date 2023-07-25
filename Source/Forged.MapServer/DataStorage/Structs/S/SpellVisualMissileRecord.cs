namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellVisualMissileRecord
{
    public float[] CastOffset = new float[3];
    public float[] ImpactOffset = new float[3];
    public uint Id;
    public ushort SpellVisualEffectNameID;
    public uint SoundEntriesID;
    public sbyte Attachment;
    public sbyte DestinationAttachment;
    public ushort CastPositionerID;
    public ushort ImpactPositionerID;
    public int FollowGroundHeight;
    public uint FollowGroundDropSpeed;
    public ushort FollowGroundApproach;
    public uint Flags;
    public ushort SpellMissileMotionID;
    public uint AnimKitID;
    public sbyte ClutterLevel;
    public int DecayTimeAfterImpact;
    public uint SpellVisualMissileSetID;
}