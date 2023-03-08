// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49681)]
public class npc_mechanical_dm : ScriptedAI
{
	public npc_mechanical_dm(Creature creature) : base(creature) { }

	public override void Reset()
	{
		_events.Reset();
	}

	public override void MoveInLineOfSight(Unit who)
	{
		if (me.IsWithinDistInMap(who, 10) && me.IsWithinLOSInMap(who))
			JustEnteredCombat(who);
	}

	public override void JustEnteredCombat(Unit who)
	{
		DoZoneInCombat();
		me.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
		me.RemoveAura(DMSharedSpells.OFFLINE);
		_events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_SPIRIT_STRIKE, TimeSpan.FromMilliseconds(6000));
	}

	public override void JustDied(Unit killer)
	{
		var players = new List<Unit>();

		var checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
		var searcher = new PlayerListSearcher(me, players, checker);
		Cell.VisitGrid(me, searcher, 150f);

		foreach (var item in players)
			item.AddAura(boss_vanessa_vancleef.Spells.EFFECT_1, item);

		me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_14, null, true);

		var Vanessa = me.FindNearestCreature(DMCreatures.NPC_VANESSA_NIGHTMARE, 500, true);

		if (Vanessa != null)
		{
			var pAI = (npc_vanessa_nightmare)Vanessa.AI;

			if (pAI != null)
				pAI.NightmarePass();
		}
	}

	public override void UpdateAI(uint diff)
	{
		_events.Update(diff);

		uint eventId;

		while ((eventId = _events.ExecuteEvent()) != 0)
			switch (eventId)
			{
				case boss_vanessa_vancleef.BossEvents.EVENT_SPIRIT_STRIKE:
					DoCastVictim(boss_vanessa_vancleef.Spells.SPIRIT_STRIKE);
					_events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_SPIRIT_STRIKE, TimeSpan.FromMilliseconds(RandomHelper.URand(5000, 7000)));

					break;
			}

		DoMeleeAttackIfReady();
	}
}