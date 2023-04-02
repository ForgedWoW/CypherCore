// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells;

public class SpellLearnSpellNode
{
    public bool Active;
    // show in spellbook or not
    public bool AutoLearned;

    public uint OverridesSpell;
    public uint Spell;
    // This marks the spell as automatically learned from another source that - will only be used for unlearning
}