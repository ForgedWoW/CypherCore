// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(198813)]
public class spell_dh_vengeful_retreat_damage : SpellScript, IHasSpellEffects, ISpellOnCast
{
	private bool _targetHit;
	public List<ISpellEffect> SpellEffects { get; } = new();

	public void OnCast()
	{
		var caster = Caster;

		if (caster != null)
		{
			if (caster.HasAura(DemonHunterSpells.PREPARED) && _targetHit)
				caster.CastSpell(caster, DemonHunterSpells.PREPARED_FURY, true);

			var aur = caster.GetAura(DemonHunterSpells.GLIMPSE);

			if (aur != null)
			{
				var aurEff = aur.GetEffect(0);

				if (aurEff != null)
				{
					var blur = caster.AddAura(DemonHunterSpells.BLUR_BUFF, caster);

					if (blur != null)
						blur.SetDuration(aurEff.BaseAmount);
				}
			}

			if (caster.HasAura(DemonHunterSpells.RUSHING_VAULT))
			{
				var chargeCatId = Global.SpellMgr.GetSpellInfo(DemonHunterSpells.FEL_RUSH, Difficulty.None).ChargeCategoryId;
				caster.SpellHistory.RestoreCharge(chargeCatId);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitSrcAreaEnemy)); // 33
	}

	private void CountTargets(List<WorldObject> targets)
	{
		_targetHit = targets.Count > 0;
	}
}