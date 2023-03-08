// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior;

// 203179 - Opportunity Strike
[SpellScript(203179)]
public class spell_warr_opportunity_strike : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo eventInfo)
	{
		if (!Caster)
			return;

		if (eventInfo?.DamageInfo?.GetSpellInfo() != null && eventInfo.DamageInfo.GetSpellInfo().Id == WarriorSpells.OPPORTUNITY_STRIKE_DAMAGE)
			return;

		var target = eventInfo.ActionTarget;

		if (target != null)
		{
			var _player = Caster.AsPlayer;

			if (_player != null)
			{
				var aur = Aura;

				if (aur != null)
				{
					var eff = aur.GetEffect(0);

					if (eff != null)
						if (RandomHelper.randChance(eff.Amount))
							_player.CastSpell(target, WarriorSpells.OPPORTUNITY_STRIKE_DAMAGE, true);
				}
			}
		}
	}
}