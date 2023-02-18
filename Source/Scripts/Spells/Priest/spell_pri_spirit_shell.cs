﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(109964)]
public class spell_pri_spirit_shell : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
		{
			var target = GetHitUnit();

			if (target != null)
				if (_player.HasAura(PriestSpells.SPIRIT_SHELL_AURA))
				{
					var bp = GetHitHeal();

					SetHitHeal(0);

					var shell = _player.GetAuraEffect(114908, 0);

					if (shell != null)
					{
						shell.SetAmount(Math.Min(shell.GetAmount() + bp, (int)_player.CountPctFromMaxHealth(60)));
					}
					else
					{
						var args = new CastSpellExtraArgs();
						args.AddSpellMod(SpellValueMod.BasePoint0, (int)bp);
						args.SetTriggerFlags(TriggerCastFlags.FullMask);
						_player.CastSpell(target, PriestSpells.SPIRIT_SHELL_ABSORPTION, args);
					}
				}
		}
	}
}