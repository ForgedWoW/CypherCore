﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // Used for some spells cast by vehicles or charmed creatures that do not send a cooldown event on their own
internal class spell_gen_charmed_unit_spell_cooldown : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = Caster;
		var owner = caster.CharmerOrOwnerPlayerOrPlayerItself;

		if (owner != null)
		{
			SpellCooldownPkt spellCooldown = new();
			spellCooldown.Caster = owner.GUID;
			spellCooldown.Flags = SpellCooldownFlags.None;
			spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(SpellInfo.Id, SpellInfo.RecoveryTime));
			owner.SendPacket(spellCooldown);
		}
	}
}