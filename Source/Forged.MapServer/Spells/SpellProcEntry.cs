// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.Spells;

public class SpellProcEntry
{
    public ProcAttributes AttributesMask { get; set; }
    public float Chance { get; set; }
    public uint Charges { get; set; }
    // if nonzero - owerwrite procChance field for given Spell.dbc entry, defines chance of proc to occur, not used if ProcsPerMinute set
    public uint Cooldown { get; set; }

    // bitmask, see ProcAttributes
    public uint DisableEffectsMask { get; set; }

    public ProcFlagsHit HitMask { get; set; }
    public ProcFlagsInit ProcFlags { get; set; }
    // if nonzero - bitmask for matching proc condition based on hit result, see enum ProcFlagsHit
    // bitmask
    public float ProcsPerMinute { get; set; }

    public SpellSchoolMask SchoolMask { get; set; }             // if nonzero - bitmask for matching proc condition based on spell's school
    public FlagArray128 SpellFamilyMask { get; set; } = new(4);
    public SpellFamilyNames SpellFamilyName { get; set; }       // if nonzero - for matching proc condition based on candidate spell's SpellFamilyName
    public ProcFlagsSpellPhase SpellPhaseMask { get; set; }

    // if nonzero - bitmask for matching proc condition based on candidate spell's SpellFamilyFlags
    // if nonzero - owerwrite procFlags field for given Spell.dbc entry, bitmask for matching proc condition, see enum ProcFlags
    public ProcFlagsSpellType SpellTypeMask { get; set; }       // if nonzero - bitmask for matching proc condition based on candidate spell's damage/heal effects, see enum ProcFlagsSpellType
         // if nonzero - bitmask for matching phase of a spellcast on which proc occurs, see enum ProcFlagsSpellPhase
                   // if nonzero - chance to proc is equal to value * aura caster's weapon speed / 60
                          // if nonzero - cooldown in secs for aura proc, applied to aura
                               // if nonzero - owerwrite procCharges field for given Spell.dbc entry, defines how many times proc can occur before aura remove, 0 - infinite
}