// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script("spell_gen_break_shield")]
[Script("spell_gen_tournament_counterattack")]
internal class spell_gen_break_shield : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(int effIndex)
	{
		var target = HitUnit;

		switch (effIndex)
		{
			case 0: // On spells wich trigger the damaging spell (and also the visual)
			{
				uint spellId;

				switch (SpellInfo.Id)
				{
					case GenericSpellIds.BreakShieldTriggerUnk:
					case GenericSpellIds.BreakShieldTriggerCampaingWarhorse:
						spellId = GenericSpellIds.BreakShieldDamage10k;

						break;
					case GenericSpellIds.BreakShieldTriggerFactionMounts:
						spellId = GenericSpellIds.BreakShieldDamage2k;

						break;
					default:
						return;
				}

				var rider = Caster.Charmer;

				if (rider)
					rider.CastSpell(target, spellId, false);
				else
					Caster.CastSpell(target, spellId, false);

				break;
			}
			case 1: // On damaging spells, for removing a defend layer
			{
				var auras = target.GetAppliedAurasQuery();

				foreach (var pair in auras.HasSpellIds(62552, 62719, 64100, 66482).GetResults())
				{
					var aura = pair.Base;

					if (aura != null)
					{
						aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
						// Remove dummys from rider (Necessary for updating visual shields)
						var rider = target.Charmer;

						if (rider)
						{
							var defend = rider.GetAura(aura.Id);

							defend?.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
						}

						break;
					}
				}

				break;
			}
			default:
				break;
		}
	}
}