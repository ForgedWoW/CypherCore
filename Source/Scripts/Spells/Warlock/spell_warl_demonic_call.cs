﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(new uint[]
{
	686, 6353, 103964, 205145
})]
public class spell_warl_demonic_call : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var _player = Caster.AsPlayer;

		if (_player != null)
			if (HitUnit)
				if (_player.HasAura(WarlockSpells.DEMONIC_CALL) && !_player.HasAura(WarlockSpells.DISRUPTED_NETHER))
				{
					_player.CastSpell(_player, WarlockSpells.HAND_OF_GULDAN_SUMMON, true);
					_player.RemoveAura(WarlockSpells.DEMONIC_CALL);
				}
	}
}