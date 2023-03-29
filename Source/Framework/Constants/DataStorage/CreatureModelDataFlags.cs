// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CreatureModelDataFlags
{
    NoFootprintParticles = 0x01,
    NoBreathParticles = 0x02,
    IsPlayerModel = 0x04,
    NoAttachedWeapons = 0x10,
    NoFootprintTrailTextures = 0x20,
    DisableHighlight = 0x40,
    CanMountWhileTransformedAsThis = 0x80,
    DisableScaleInterpolation = 0x100,
    ForceProjectedTex = 0x200,
    CanJumpInPlaceAsMount = 0x400,
    AICannotUseWalkBackwardsAnim = 0x800,
    IgnoreSpineLowForSplitBody = 0x1000,
    IgnoreHeadForSplitBody = 0x2000,
    IgnoreSpineLowForSplitBodyWhenFlying = 0x4000,
    IgnoreHeadForSplitBodyWhenFlying = 0x8000,
    UseWheelAnimationOnUnitWheelBones = 0x10000,
    IsHDModel = 0x20000,
    SuppressEmittersOnLowSettings = 0x40000
}