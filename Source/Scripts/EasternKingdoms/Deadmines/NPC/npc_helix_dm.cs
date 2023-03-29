﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
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

[CreatureScript(49674)]
public class npc_helix_dm : BossAI
{
    public static readonly Position[] NightmareSpidersSpawn =
    {
        new(-185.03f, -579.83f, -20.63f, 3.19f), new(-186.59f, -573.01f, -20.95f, 5.61f), new(-176.38f, -565.76f, -19.30f, 5.03f), new(-181.68f, -566.33f, -51.11f, 5.15f)
    };

    public uint FlagResetTimer;

    public npc_helix_dm(Creature creature) : base(creature, DMData.DATA_NIGHTMARE_HELIX) { }

    public override void Reset()
    {
        _Reset();
        FlagResetTimer = 15000;
        Instance.SetData(DMData.DATA_NIGHTMARE_HELIX, (uint)EncounterState.NotStarted);
    }

    public override void JustEnteredCombat(Unit who)
    {
        base.JustEnteredCombat(who);
        Events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_SPIRIT_STRIKE, TimeSpan.FromMilliseconds(6000));
        Events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_SPIDERS, TimeSpan.FromMilliseconds(2000));

        Me.SummonCreature(DMCreatures.NPC_MAIN_SPIDER, NightmareSpidersSpawn[3], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
    }

    public override void JustSummoned(Creature summoned)
    {
        switch (summoned.Entry)
        {
            case DMCreatures.NPC_NIGHTMARE_SPIDER:
            case DMCreatures.NPC_MAIN_SPIDER:
            case DMCreatures.NPC_CHATTERING_HORROR:
            {
                summoned.AI.AttackStart(Me.Victim);

                break;
            }
        }

        Summons.Summon(summoned);
    }

    public override void JustDied(Unit killer)
    {
        var players = new List<Unit>();

        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
        var searcher = new PlayerListSearcher(Me, players, checker);
        Cell.VisitGrid(Me, searcher, 150f);

        foreach (var item in players)
            item.AddAura(boss_vanessa_vancleef.Spells.EFFECT_1, item);

        Me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_14, null, true);

        var Vanessa = Me.FindNearestCreature(DMCreatures.NPC_VANESSA_NIGHTMARE, 500, true);

        if (Vanessa != null)
        {
            var pAI = (npc_vanessa_nightmare)Vanessa.AI;

            if (pAI != null)
                pAI.NightmarePass();
        }

        base.JustDied(killer);
    }

    public void SummonSpiders()
    {
        for (byte i = 0; i < 3; ++i)
            Me.SummonCreature(DMCreatures.NPC_NIGHTMARE_SPIDER, NightmareSpidersSpawn[i], TempSummonType.ManualDespawn);
    }

    public override void UpdateAI(uint diff)
    {
        if (FlagResetTimer <= diff)
            Me.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
        else
            FlagResetTimer -= diff;

        Events.Update(diff);

        uint eventId;

        while ((eventId = Events.ExecuteEvent()) != 0)
            switch (eventId)
            {
                case boss_vanessa_vancleef.BossEvents.EVENT_SPIRIT_STRIKE:
                    DoCastVictim(boss_vanessa_vancleef.Spells.SPIRIT_STRIKE);
                    Events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_SPIRIT_STRIKE, TimeSpan.FromMilliseconds(RandomHelper.URand(5000, 7000)));

                    break;
                case boss_vanessa_vancleef.BossEvents.EVENT_SPIDERS:
                    SummonSpiders();
                    Events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_SPIDERS, TimeSpan.FromMilliseconds(RandomHelper.URand(3000, 4000)));

                    break;
            }

        DoMeleeAttackIfReady();
    }
}