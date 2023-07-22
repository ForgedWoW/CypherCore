// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Framework.Constants;

public struct SpellConst
{
    public const int EffectAll = 255;
    public const int EffectFirstFound = 254;
    public const uint MAX_EFFECT_MASK = 0xFFFFFFFF;

    public const int MaxAuras = 255;
    public const int MaxPowersPerSpell = 5;
    public const int MaxReagents = 8;
    public const int MaxShapeshift = 8;
    public const int MaxTotems = 2;
    public const float TrajectoryMissileSize = 3.0f;
    public const uint VisualKitDrink = 438;
    public const uint VisualKitFood = 406;
    public static HashSet<int> MaxEffects => new HashSet<int>().Fill(32);
}

// only used in code

//Spell targets used by SelectSpell

//Spell Effects used by SelectSpell

// Enum with EffectRadiusIndex and their actual radius

// Spell dispel type

// Spell clasification

// Spell mechanics

//Effects

// Spell aura states

// target enum name consist of:
// [OBJECTTYPE][REFERENCETYPE(skipped for caster)][SELECTIONTYPE(skipped for default)][additional specifiers(friendly, BACKLEFT, etc.]