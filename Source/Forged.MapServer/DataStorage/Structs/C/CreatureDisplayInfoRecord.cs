namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CreatureDisplayInfoRecord
{
    public uint Id;
    public ushort ModelID;
    public ushort SoundID;
    public sbyte SizeClass;
    public float CreatureModelScale;
    public byte CreatureModelAlpha;
    public byte BloodID;
    public int ExtendedDisplayInfoID;
    public ushort NPCSoundID;
    public ushort ParticleColorID;
    public int PortraitCreatureDisplayInfoID;
    public int PortraitTextureFileDataID;
    public ushort ObjectEffectPackageID;
    public ushort AnimReplacementSetID;
    public byte Flags;
    public int StateSpellVisualKitID;
    public float PlayerOverrideScale;
    public float PetInstanceScale; // scale of not own player pets inside dungeons/raids/scenarios
    public sbyte UnarmedWeaponType;
    public int MountPoofSpellVisualKitID;
    public int DissolveEffectID;
    public sbyte Gender;
    public int DissolveOutEffectID;
    public sbyte CreatureModelMinLod;
    public int[] TextureVariationFileDataID = new int[4];
}