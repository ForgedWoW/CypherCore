// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellModType
{
	Flat = 0,      // SPELL_AURA_ADD_FLAT_MODIFIER
	Pct = 1,       // SPELL_AURA_ADD_PCT_MODIFIER
	LabelFlat = 2, // SPELL_AURA_ADD_FLAT_MODIFIER_BY_SPELL_LABEL
	LabelPct = 3,  // SPELL_AURA_ADD_PCT_MODIFIER_BY_SPELL_LABEL
	End
}