﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 46023 The Ultrasonic Screwdriver
internal class spell_q11730_ultrasonic_screwdriver : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return Caster.IsTypeId(TypeId.Player) && CastItem;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var castItem = CastItem;
		var caster = Caster;

		var target = HitCreature;

		if (target)
		{
			uint spellId;

			switch (target.Entry)
			{
				case CreatureIds.Scavengebot004a8:
					spellId = QuestSpellIds.SummonScavengebot004a8;

					break;
				case CreatureIds.Sentrybot57k:
					spellId = QuestSpellIds.SummonSentrybot57k;

					break;
				case CreatureIds.Defendotank66d:
					spellId = QuestSpellIds.SummonDefendotank66d;

					break;
				case CreatureIds.Scavengebot005b6:
					spellId = QuestSpellIds.SummonScavengebot005b6;

					break;
				case CreatureIds.Npc55dCollectatron:
					spellId = QuestSpellIds.Summon55dCollectatron;

					break;
				default:
					return;
			}

			caster.CastSpell(caster, spellId, new CastSpellExtraArgs(castItem));
			caster.CastSpell(caster, QuestSpellIds.RobotKillCredit, true);
			target.DespawnOrUnsummon();
		}
	}
}