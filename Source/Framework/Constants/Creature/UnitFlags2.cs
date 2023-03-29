// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum UnitFlags2 : uint
{
    FeignDeath = 0x01,
    HideBody = 0x02,
    IgnoreReputation = 0x04,
    ComprehendLang = 0x08,
    MirrorImage = 0x10,
    DontFadeIn = 0x20,
    ForceMovement = 0x40,
    DisarmOffhand = 0x80,
    DisablePredStats = 0x100,
    AllowChangingTalents = 0x200,
    DisarmRanged = 0x400,
    RegeneratePower = 0x800,
    RestrictPartyInteraction = 0x1000,
    PreventSpellClick = 0x2000,
    InteractWhileHostile = 0x4000,
    CannotTurn = 0x8000,
    Unk2 = 0x10000,
    PlayDeathAnim = 0x20000,
    AllowCheatSpells = 0x40000,
    SuppressHighlightWhenTargetedOrMousedOver = 0x00080000,
    TreatAsRaidUnitForHelpfulSpells = 0x100000,
    LargeAoi = 0x00200000,
    GiganticAoi = 0x400000,
    NoActions = 0x800000,
    AiWillOnlySwimIfTargetSwims = 0x1000000,
    DontGenerateCombatLogWhenEngagedWithNpcs = 0x2000000,
    UntargetableByClient = 0x4000000,
    AttackerIgnoresMinimumRanges = 0x8000000,
    UninteractibleIfHostile = 0x10000000,
    Unsued11 = 0x20000000,
    InfiniteAoi = 0x40000000,
    Unused13 = 0x80000000,

    Disallowed = (FeignDeath |
                  IgnoreReputation |
                  ComprehendLang |
                  MirrorImage |
                  ForceMovement |
                  DisarmOffhand |
                  DisablePredStats |
                  AllowChangingTalents |
                  DisarmRanged |
                  /* UNIT_FLAG2_REGENERATE_POWER | */ RestrictPartyInteraction |
                  PreventSpellClick |
                  InteractWhileHostile | /* Unk2 | */
                  /* UNIT_FLAG2_PLAY_DEATH_ANIM | */ AllowCheatSpells |
                  SuppressHighlightWhenTargetedOrMousedOver |
                  TreatAsRaidUnitForHelpfulSpells |
                  LargeAoi |
                  GiganticAoi |
                  NoActions |
                  AiWillOnlySwimIfTargetSwims |
                  DontGenerateCombatLogWhenEngagedWithNpcs |
                  AttackerIgnoresMinimumRanges |
                  UninteractibleIfHostile |
                  Unsued11 |
                  InfiniteAoi |
                  Unused13),

    Allowed = (0xFFFFFFFF & ~Disallowed)
}