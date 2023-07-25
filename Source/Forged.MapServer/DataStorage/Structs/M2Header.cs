using Framework.GameMath;

namespace Forged.MapServer.DataStorage.Structs;

public struct M2Header
{
    public uint Magic;            // "MD20"
    public uint Version;          // The version of the format.
    public uint lName;            // Length of the model's name including the trailing \0
    public uint ofsName;          // Offset to the name, it seems like models can get reloaded by this name.should be unique, i guess.
    public uint GlobalModelFlags; // 0x0001: tilt x, 0x0002: tilt y, 0x0008: add 2 fields in header, 0x0020: load .phys data (MoP+), 0x0080: has _lod .skin files (MoP?+), 0x0100: is camera related.
    public uint nGlobalSequences;
    public uint ofsGlobalSequences; // A list of timestamps.
    public uint nAnimations;
    public uint ofsAnimations; // Information about the animations in the model.
    public uint nAnimationLookup;
    public uint ofsAnimationLookup; // Mapping of global IDs to the entries in the Animation sequences block.
    public uint nBones;             // MAX_BONES = 0x100
    public uint ofsBones;           // Information about the bones in this model.
    public uint nKeyBoneLookup;
    public uint ofsKeyBoneLookup; // Lookup table for key skeletal bones.
    public uint nVertices;
    public uint ofsVertices; // Vertices of the model.
    public uint nViews;      // Views (LOD) are now in .skins.
    public uint nSubmeshAnimations;
    public uint ofsSubmeshAnimations; // Submesh color and alpha animations definitions.
    public uint nTextures;
    public uint ofsTextures; // Textures of this model.
    public uint nTransparency;
    public uint ofsTransparency; // Transparency of textures.
    public uint nUVAnimation;
    public uint ofsUVAnimation;
    public uint nTexReplace;
    public uint ofsTexReplace; // Replaceable Textures.
    public uint nRenderFlags;
    public uint ofsRenderFlags; // Blending modes / render flags.
    public uint nBoneLookupTable;
    public uint ofsBoneLookupTable; // A bone lookup table.
    public uint nTexLookup;
    public uint ofsTexLookup; // The same for textures.
    public uint nTexUnits;    // possibly removed with cata?!
    public uint ofsTexUnits;  // And texture units. Somewhere they have to be too.
    public uint nTransLookup;
    public uint ofsTransLookup; // Everything needs its lookup. Here are the transparencies.
    public uint nUVAnimLookup;
    public uint ofsUVAnimLookup;
    public AxisAlignedBox BoundingBox; // min/max( [1].z, 2.0277779f ) - 0.16f seems to be the maximum camera height
    public float BoundingSphereRadius;
    public AxisAlignedBox CollisionBox;
    public float CollisionSphereRadius;
    public uint nBoundingTriangles;
    public uint ofsBoundingTriangles; // Our bounding volumes. Similar structure like in the old ofsViews.
    public uint nBoundingVertices;
    public uint ofsBoundingVertices;
    public uint nBoundingNormals;
    public uint ofsBoundingNormals;
    public uint nAttachments;
    public uint ofsAttachments; // Attachments are for weapons etc.
    public uint nAttachLookup;
    public uint ofsAttachLookup; // Of course with a lookup.
    public uint nEvents;
    public uint ofsEvents; // Used for playing sounds when dying and a lot else.
    public uint nLights;
    public uint ofsLights;  // Lights are mainly used in loginscreens but in wands and some doodads too.
    public uint nCameras;   // Format of Cameras changed with version 271!
    public uint ofsCameras; // The cameras are present in most models for having a model in the Character-Tab.
    public uint nCameraLookup;
    public uint ofsCameraLookup; // And lookup-time again.
    public uint nRibbonEmitters;
    public uint ofsRibbonEmitters; // Things swirling around. See the CoT-entrance for light-trails.
    public uint nParticleEmitters;
    public uint ofsParticleEmitters; // Spells and weapons, doodads and loginscreens use them. Blood dripping of a blade? Particles.
    public uint nBlendMaps;          // This has to deal with blending. Exists IFF (flags & 0x8) != 0. When set, textures blending is overriden by the associated array. See M2/WotLK#Blend_mode_overrides
    public uint ofsBlendMaps;        // Same as above. Points to an array of uint16 of nBlendMaps entries -- From WoD information.};
}