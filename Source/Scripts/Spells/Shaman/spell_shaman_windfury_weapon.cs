﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
    // 8232 - Windfury Weapon
    [SpellScript(8232)]
	public class spell_shaman_windfury_weapon : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return Global.SpellMgr.GetSpellInfo(ShamanSpells.WINDFURY_WEAPON_PASSIVE, Difficulty.None) != null;
		}

		private void HandleDummy(int effIndex)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var auraEffect = caster.GetAuraEffect(ShamanSpells.WINDFURY_WEAPON_PASSIVE, 0);

				if (auraEffect != null)
					auraEffect.SetAmount(GetEffectValue());
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}