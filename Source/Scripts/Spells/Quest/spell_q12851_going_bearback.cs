﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 54798 FLAMING Arrow Triggered Effect
internal class spell_q12851_going_bearback : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
	}

	private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = Caster;

		if (caster)
		{
			var target = Target;

			// Already in fire
			if (target.HasAura(QuestSpellIds.Ablaze))
				return;

			var player = caster.CharmerOrOwnerPlayerOrPlayerItself;

			if (player)
				switch (target.Entry)
				{
					case CreatureIds.Frostworg:
						target.CastSpell(player, QuestSpellIds.FrostworgCredit, true);
						target.CastSpell(target, QuestSpellIds.Immolation, true);
						target.CastSpell(target, QuestSpellIds.Ablaze, true);

						break;
					case CreatureIds.Frostgiant:
						target.CastSpell(player, QuestSpellIds.FrostgiantCredit, true);
						target.CastSpell(target, QuestSpellIds.Immolation, true);
						target.CastSpell(target, QuestSpellIds.Ablaze, true);

						break;
				}
		}
	}
}