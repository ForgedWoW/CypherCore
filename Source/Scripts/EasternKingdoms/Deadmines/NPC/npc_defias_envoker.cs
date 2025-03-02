﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(48418)]
public class npc_defias_envokerAI : ScriptedAI
{
	public uint HolyfireTimer;
	public uint ShieldTimer;

	public npc_defias_envokerAI(Creature creature) : base(creature) { }

	public override void Reset()
	{
		HolyfireTimer = 4000;
		ShieldTimer = 8000;
	}

	public override void UpdateAI(uint diff)
	{
		if (HolyfireTimer <= diff)
		{
			var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

			if (target != null)
				DoCast(target, boss_vanessa_vancleef.Spells.HOLY_FIRE);

			HolyfireTimer = RandomHelper.URand(8000, 11000);
		}
		else
		{
			HolyfireTimer -= diff;
		}

		if (ShieldTimer <= diff)
		{
			if (IsHeroic())
			{
				DoCast(Me, boss_vanessa_vancleef.Spells.SHIELD);
				ShieldTimer = RandomHelper.URand(18000, 20000);
			}
		}
		else
		{
			ShieldTimer -= diff;
		}

		DoMeleeAttackIfReady();
	}
}