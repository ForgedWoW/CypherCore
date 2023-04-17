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

[CreatureScript(49670)]
public class NPCGlubtokDm : BossAI
{
    public uint FlagResetTimer;

    public NPCGlubtokDm(Creature creature) : base(creature, DmData.DATA_NIGHTMARE_MECHANICAL) { }

    public override void Reset()
    {
        _Reset();
        FlagResetTimer = 10000;
        Events.ScheduleEvent(BossVanessaVancleef.BossEvents.EVENT_ICYCLE_AOE, TimeSpan.FromMilliseconds(RandomHelper.URand(11000, 15000)));
    }

    public override void JustEnteredCombat(Unit who)
    {
        base.JustEnteredCombat(who);
        Events.RescheduleEvent(BossVanessaVancleef.BossEvents.EVENT_ICYCLE_AOE, TimeSpan.FromMilliseconds(RandomHelper.URand(6000, 8000)));

        Events.ScheduleEvent(BossVanessaVancleef.BossEvents.EVENT_SPIRIT_STRIKE, TimeSpan.FromMilliseconds(6000));
    }

    public override void JustDied(Unit killer)
    {
        var players = new List<Unit>();

        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
        var searcher = new PlayerListSearcher(Me, players, checker);
        Cell.VisitGrid(Me, searcher, 150f);

        foreach (var item in players)
            item.AddAura(BossVanessaVancleef.Spells.EFFECT_1, item);

        Me.TextEmote(BossVanessaVancleef.VANESSA_NIGHTMARE_14, null, true);

        var vanessa = Me.FindNearestCreature(DmCreatures.NPC_VANESSA_NIGHTMARE, 500, true);

        if (vanessa != null)
        {
            var pAI = (NPCVanessaNightmare)vanessa.AI;

            if (pAI != null)
                pAI.NightmarePass();
        }

        base.JustDied(killer);
    }

    public override void UpdateAI(uint diff)
    {
        if (FlagResetTimer <= diff)
        {
            Me.SetVisible(true);
            Me.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
        }
        else
        {
            FlagResetTimer -= diff;
        }

        Events.Update(diff);

        uint eventId;

        while ((eventId = Events.ExecuteEvent()) != 0)
            switch (eventId)
            {
                case BossVanessaVancleef.BossEvents.EVENT_ICYCLE_AOE:
                    var pPlayer = Me.FindNearestPlayer(200.0f, true);

                    if (pPlayer != null)
                        DoCast(pPlayer, BossVanessaVancleef.Spells.ICYCLE);

                    Events.ScheduleEvent(BossVanessaVancleef.BossEvents.EVENT_ICYCLE_AOE, TimeSpan.FromMilliseconds(RandomHelper.URand(6000, 8000)));

                    break;
                case BossVanessaVancleef.BossEvents.EVENT_SPIRIT_STRIKE:
                    DoCastVictim(BossVanessaVancleef.Spells.SPIRIT_STRIKE);
                    Events.ScheduleEvent(BossVanessaVancleef.BossEvents.EVENT_SPIRIT_STRIKE, TimeSpan.FromMilliseconds(RandomHelper.URand(5000, 7000)));

                    break;
            }

        DoMeleeAttackIfReady();
    }
}