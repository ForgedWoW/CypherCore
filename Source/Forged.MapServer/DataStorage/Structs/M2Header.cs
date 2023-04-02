// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.GameMath;

namespace Forged.MapServer.DataStorage.Structs;

public struct M2Header
{
    public AxisAlignedBox BoundingBox;
    // min/max( [1].z, 2.0277779f ) - 0.16f seems to be the maximum camera height
    public float BoundingSphereRadius;

    public AxisAlignedBox CollisionBox;
    public float CollisionSphereRadius;
    public uint GlobalModelFlags;
    public uint lName;
    public uint Magic;            // "MD20"
    public uint nAnimationLookup;
    public uint nAnimations;
    public uint nAttachLookup;
    public uint nAttachments;
    public uint nBlendMaps;
    public uint nBoneLookupTable;
    public uint nBones;
    public uint nBoundingNormals;
    public uint nBoundingTriangles;
    public uint nBoundingVertices;
    public uint nCameraLookup;
    public uint nCameras;
    public uint nEvents;
    // 0x0001: tilt x, 0x0002: tilt y, 0x0008: add 2 fields in header, 0x0020: load .phys data (MoP+), 0x0080: has _lod .skin files (MoP?+), 0x0100: is camera related.
    public uint nGlobalSequences;

    public uint nKeyBoneLookup;
    public uint nLights;
    public uint nParticleEmitters;
    public uint nRenderFlags;
    public uint nRibbonEmitters;
    public uint nSubmeshAnimations;
    public uint nTexLookup;
    public uint nTexReplace;
    public uint nTextures;
    public uint nTexUnits;
    public uint nTransLookup;
    public uint nTransparency;
    public uint nUVAnimation;
    public uint nUVAnimLookup;
    public uint nVertices;
    public uint nViews;
    public uint ofsAnimationLookup;
    public uint ofsAnimations;
    public uint ofsAttachLookup;
    public uint ofsAttachments;
    // This has to deal with blending. Exists IFF (flags & 0x8) != 0. When set, textures blending is overriden by the associated array. See M2/WotLK#Blend_mode_overrides
    public uint ofsBlendMaps;

    public uint ofsBoneLookupTable;
    // Information about the animations in the model.
    // Mapping of global IDs to the entries in the Animation sequences block.
    // MAX_BONES = 0x100
    public uint ofsBones;

    public uint ofsBoundingNormals;
    public uint ofsBoundingTriangles;
    // Our bounding volumes. Similar structure like in the old ofsViews.
    public uint ofsBoundingVertices;

    public uint ofsCameraLookup;
    // Format of Cameras changed with version 271!
    public uint ofsCameras;

    // Attachments are for weapons etc.
    // Of course with a lookup.
    public uint ofsEvents;

    public uint ofsGlobalSequences;
    // A list of timestamps.
    // Information about the bones in this model.
    public uint ofsKeyBoneLookup;

    // Used for playing sounds when dying and a lot else.
    public uint ofsLights;

    // Length of the model's name including the trailing \0
    public uint ofsName;

    public uint ofsParticleEmitters;
    public uint ofsRenderFlags;
    // Lights are mainly used in loginscreens but in wands and some doodads too.
    // The cameras are present in most models for having a model in the Character-Tab.
    // And lookup-time again.
    public uint ofsRibbonEmitters;

    // Views (LOD) are now in .skins.
    public uint ofsSubmeshAnimations;

    // Blending modes / render flags.
    // A bone lookup table.
    public uint ofsTexLookup;

    public uint ofsTexReplace;
    // Submesh color and alpha animations definitions.
    public uint ofsTextures;

    // Replaceable Textures.
    // The same for textures.
    // possibly removed with cata?!
    public uint ofsTexUnits;

    // And texture units. Somewhere they have to be too.
    public uint ofsTransLookup;

    // Textures of this model.
    public uint ofsTransparency;

    // Transparency of textures.
    public uint ofsUVAnimation;

    // Everything needs its lookup. Here are the transparencies.
    public uint ofsUVAnimLookup;

    // Offset to the name, it seems like models can get reloaded by this name.should be unique, i guess.
    // Lookup table for key skeletal bones.
    public uint ofsVertices;

    public uint Version;          // The version of the format.
                                  // Vertices of the model.
                                  // Things swirling around. See the CoT-entrance for light-trails.
                                  // Spells and weapons, doodads and loginscreens use them. Blood dripping of a blade? Particles.
                                  // Same as above. Points to an array of uint16 of nBlendMaps entries -- From WoD information.};
}