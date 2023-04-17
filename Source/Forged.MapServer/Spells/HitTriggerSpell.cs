// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells;

public struct HitTriggerSpell
{
    public HitTriggerSpell(SpellInfo spellInfo, SpellInfo auraSpellInfo, double procChance)
    {
        TriggeredSpell = spellInfo;
        TriggeredByAura = auraSpellInfo;
        Chance = procChance;
    }

    // ubyte triggeredByEffIdx          This might be needed at a later stage - No need known for now
    public double Chance { get; set; }

    public SpellInfo TriggeredByAura { get; set; }

    public SpellInfo TriggeredSpell { get; set; }
}