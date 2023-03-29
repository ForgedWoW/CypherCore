// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum MapFlags : uint
{
    Optimize = 0x01,
    DevelopmentMap = 0x02,
    WeightedBlend = 0x04,
    VertexColoring = 0x08,
    SortObjects = 0x10,
    LimitToPlayersFromOneRealm = 0x20,
    EnableLighting = 0x40,
    InvertedTerrain = 0x80,
    DynamicDifficulty = 0x100,
    ObjectFile = 0x200,
    TextureFile = 0x400,
    GenerateNormals = 0x800,
    FixBorderShadowSeams = 0x1000,
    InfiniteOcean = 0x2000,
    UnderwaterMap = 0x4000,
    FlexibleRaidLocking = 0x8000,
    LimitFarclip = 0x10000,
    UseParentMapFlightBounds = 0x20000,
    NoRaceChangeOnThisMap = 0x40000,
    DisabledForNonGMs = 0x80000,
    WeightedNormals1 = 0x100000,
    DisableLowDetailTerrain = 0x200000,
    EnableOrgArenaBlinkRule = 0x400000,
    WeightedHeightBlend = 0x800000,
    CoalescingAreaSharing = 0x1000000,
    ProvingGrounds = 0x2000000,
    Garrison = 0x4000000,
    EnableAINeedSystem = 0x8000000,
    SingleVServer = 0x10000000,
    UseInstancePool = 0x20000000,
    MapUsesRaidGraphics = 0x40000000,
    ForceCustomUIMap = 0x80000000,
}