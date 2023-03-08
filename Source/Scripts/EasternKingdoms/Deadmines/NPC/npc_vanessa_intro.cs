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

[CreatureScript(DMCreatures.NPC_VANESSA_VANCLEEF)]
public class npc_vanessa_introAI : BossAI
{
	public bool EventStarted;

	public byte Phase;
	public uint PongTimer;

	public npc_vanessa_introAI(Creature creature) : base(creature, DMData.DATA_VANESSA_NIGHTMARE) { }

	public override void Reset()
	{
		if (!me)
			return;

		EventStarted = true;
		Phase = 0;
		PongTimer = 2000;

		me.AddAura(boss_vanessa_vancleef.Spells.SITTING, me);
		me.SetSpeed(UnitMoveType.Walk, 1.0f);
		me.AddUnitMovementFlag(MovementFlag.Walking);
	}

	public override void UpdateAI(uint diff)
	{
		if (EventStarted)
		{
			if (PongTimer <= diff)
				switch (Phase)
				{
					case 0:
						me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_1, null, true);
						me.RemoveAura(boss_vanessa_vancleef.Spells.SITTING);
						PongTimer = 2000;
						Phase++;

						break;
					case 1:
						me.MotionMaster.MoveJump(-65.93f, -820.33f, 40.98f, 10.0f, 8.0f);
						me.Say(boss_vanessa_vancleef.VANESSA_SAY_1, Language.Universal);
						PongTimer = 6000;
						Phase++;

						break;
					case 2:
						me.MotionMaster.MovePoint(0, -65.41f, -838.43f, 41.10f);
						me.Say(boss_vanessa_vancleef.VANESSA_SAY_2, Language.Universal);
						PongTimer = 8000;
						Phase++;

						break;
					case 3:
						me.Say(boss_vanessa_vancleef.VANESSA_SAY_3, Language.Universal);
						PongTimer = 4000;
						Phase++;

						break;
					case 4:
						me.Say(boss_vanessa_vancleef.VANESSA_SAY_4, Language.Universal);
						me.SetFacingTo(1.57f);
						PongTimer = 3000;
						Phase++;

						break;
					case 5:
					{
						var players = new List<Unit>();

						var checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
						var searcher = new PlayerListSearcher(me, players, checker);
						Cell.VisitGrid(me, searcher, 150f);

						foreach (var item in players)
							me.CastSpell(item, boss_vanessa_vancleef.Spells.NOXIOUS_CONCOCTION, true);

						PongTimer = 2000;
						Phase++;
					}

						break;
					case 6:
						me.Say(boss_vanessa_vancleef.VANESSA_SAY_5, Language.Universal);
						PongTimer = 4000;
						Phase++;

						break;
					case 7:
					{
						var players = new List<Unit>();

						var checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
						var searcher = new PlayerListSearcher(me, players, checker);
						Cell.VisitGrid(me, searcher, 150f);

						var controller_achi = me.FindNearestCreature(boss_vanessa_vancleef.eAchievementMisc.NPC_ACHIEVEMENT_CONTROLLER, 300.0f);

						if (controller_achi != null)
							controller_achi.GetAI().SetData(0, boss_vanessa_vancleef.eAchievementMisc.START_TIMER_ACHIEVEMENT);

						foreach (var item in players)
						{
							me.CastSpell(item, DMSharedSpells.NIGHTMARE_ELIXIR, true);
							me.CastSpell(item, boss_vanessa_vancleef.Spells.BLACKOUT, true);
						}

						me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_2, null, true);
						PongTimer = 4100;
						Phase++;
					}

						break;
					case 8:
					{
						var players = new List<Unit>();

						var checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
						var searcher = new PlayerListSearcher(me, players, checker);
						Cell.VisitGrid(me, searcher, 150f);

						foreach (var item in players)
							me.CastSpell(item, boss_vanessa_vancleef.Spells.BLACKOUT, true);

						// me.SummonCreature(DMCreatures.NPC_TRAP_BUNNY, -65.93f, -820.33f, 40.98f, 0, TempSummonType.ManualDespawn);
						PongTimer = 4000;
						Phase++;
					}

						break;
					case 9:
					{
						me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(3000));
					}

						break;
					default:
						break;
				}
			else
				PongTimer -= diff;
		}
	}
}