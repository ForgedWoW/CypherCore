// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(DmCreatures.NPC_VANESSA_VANCLEEF)]
public class NPCVanessaIntroAI : BossAI
{
    public bool EventStarted;

    public byte Phase;
    public uint PongTimer;

    public NPCVanessaIntroAI(Creature creature) : base(creature, DmData.DATA_VANESSA_NIGHTMARE) { }

    public override void Reset()
    {
        if (!Me)
            return;

        EventStarted = true;
        Phase = 0;
        PongTimer = 2000;

        Me.AddAura(BossVanessaVancleef.Spells.SITTING, Me);
        Me.SetSpeed(UnitMoveType.Walk, 1.0f);
        Me.AddUnitMovementFlag(MovementFlag.Walking);
    }

    public override void UpdateAI(uint diff)
    {
        if (EventStarted)
        {
            if (PongTimer <= diff)
                switch (Phase)
                {
                    case 0:
                        Me.TextEmote(BossVanessaVancleef.VANESSA_NIGHTMARE_1, null, true);
                        Me.RemoveAura(BossVanessaVancleef.Spells.SITTING);
                        PongTimer = 2000;
                        Phase++;

                        break;
                    case 1:
                        Me.MotionMaster.MoveJump(-65.93f, -820.33f, 40.98f, 10.0f, 8.0f);
                        Me.Say(BossVanessaVancleef.VANESSA_SAY_1, Language.Universal);
                        PongTimer = 6000;
                        Phase++;

                        break;
                    case 2:
                        Me.MotionMaster.MovePoint(0, -65.41f, -838.43f, 41.10f);
                        Me.Say(BossVanessaVancleef.VANESSA_SAY_2, Language.Universal);
                        PongTimer = 8000;
                        Phase++;

                        break;
                    case 3:
                        Me.Say(BossVanessaVancleef.VANESSA_SAY_3, Language.Universal);
                        PongTimer = 4000;
                        Phase++;

                        break;
                    case 4:
                        Me.Say(BossVanessaVancleef.VANESSA_SAY_4, Language.Universal);
                        Me.SetFacingTo(1.57f);
                        PongTimer = 3000;
                        Phase++;

                        break;
                    case 5:
                    {
                        var players = new List<Unit>();

                        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
                        var searcher = new PlayerListSearcher(Me, players, checker);
                        Cell.VisitGrid(Me, searcher, 150f);

                        foreach (var item in players)
                            Me.SpellFactory.CastSpell(item, BossVanessaVancleef.Spells.NOXIOUS_CONCOCTION, true);

                        PongTimer = 2000;
                        Phase++;
                    }

                        break;
                    case 6:
                        Me.Say(BossVanessaVancleef.VANESSA_SAY_5, Language.Universal);
                        PongTimer = 4000;
                        Phase++;

                        break;
                    case 7:
                    {
                        var players = new List<Unit>();

                        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
                        var searcher = new PlayerListSearcher(Me, players, checker);
                        Cell.VisitGrid(Me, searcher, 150f);

                        var controllerAchi = Me.FindNearestCreature(BossVanessaVancleef.EAchievementMisc.NPC_ACHIEVEMENT_CONTROLLER, 300.0f);

                        if (controllerAchi != null)
                            controllerAchi.AI.SetData(0, BossVanessaVancleef.EAchievementMisc.START_TIMER_ACHIEVEMENT);

                        foreach (var item in players)
                        {
                            Me.SpellFactory.CastSpell(item, DmSharedSpells.NIGHTMARE_ELIXIR, true);
                            Me.SpellFactory.CastSpell(item, BossVanessaVancleef.Spells.BLACKOUT, true);
                        }

                        Me.TextEmote(BossVanessaVancleef.VANESSA_NIGHTMARE_2, null, true);
                        PongTimer = 4100;
                        Phase++;
                    }

                        break;
                    case 8:
                    {
                        var players = new List<Unit>();

                        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
                        var searcher = new PlayerListSearcher(Me, players, checker);
                        Cell.VisitGrid(Me, searcher, 150f);

                        foreach (var item in players)
                            Me.SpellFactory.CastSpell(item, BossVanessaVancleef.Spells.BLACKOUT, true);

                        // me.SummonCreature(DMCreatures.NPC_TRAP_BUNNY, -65.93f, -820.33f, 40.98f, 0, TempSummonType.ManualDespawn);
                        PongTimer = 4000;
                        Phase++;
                    }

                        break;
                    case 9:
                    {
                        Me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(3000));
                    }

                        break;
                }
            else
                PongTimer -= diff;
        }
    }
}