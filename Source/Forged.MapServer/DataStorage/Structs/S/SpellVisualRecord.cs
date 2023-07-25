namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellVisualRecord
{
    public uint Id;
    public float[] MissileCastOffset = new float[3];
    public float[] MissileImpactOffset = new float[3];
    public uint AnimEventSoundID;
    public int Flags;
    public sbyte MissileAttachment;
    public sbyte MissileDestinationAttachment;
    public uint MissileCastPositionerID;
    public uint MissileImpactPositionerID;
    public int MissileTargetingKit;
    public uint HostileSpellVisualID;
    public uint CasterSpellVisualID;
    public ushort SpellVisualMissileSetID;
    public ushort DamageNumberDelay;
    public uint LowViolenceSpellVisualID;
    public uint RaidSpellVisualMissileSetID;
    public int ReducedUnexpectedCameraMovementSpellVisualID;
}