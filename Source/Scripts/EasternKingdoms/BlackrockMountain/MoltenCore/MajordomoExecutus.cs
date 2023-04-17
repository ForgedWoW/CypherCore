// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Majordomo;

internal struct SpellIds
{
    public const uint SUMMON_RAGNAROS = 19774;
    public const uint BLAST_WAVE = 20229;
    public const uint TELEPORT = 20618;
    public const uint MAGIC_REFLECTION = 20619;
    public const uint AEGIS_OF_RAGNAROS = 20620;
    public const uint DAMAGE_REFLECTION = 21075;
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_SPAWN = 1;
    public const uint SAY_SLAY = 2;
    public const uint SAY_SPECIAL = 3;
    public const uint SAY_DEFEAT = 4;

    public const uint SAY_SUMMON_MAJ = 5;
    public const uint SAY_ARRIVAL2_MAJ = 6;

    public const uint OPTION_ID_YOU_CHALLENGED_US = 0;
    public const uint MENU_OPTION_YOU_CHALLENGED_US = 4108;
}

[Script]
internal class BossMajordomo : BossAI
{
    public BossMajordomo(Creature creature) : base(creature, DataTypes.MAJORDOMO_EXECUTUS) { }

    public override void KilledUnit(Unit victim)
    {
        if (RandomHelper.URand(0, 99) < 25)
            Talk(TextIds.SAY_SLAY);
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.SAY_AGGRO);

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           task =>
                           {
                               DoCast(Me, SpellIds.MAGIC_REFLECTION);
                               task.Repeat(TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCast(Me, SpellIds.DAMAGE_REFLECTION);
                               task.Repeat(TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.BLAST_WAVE);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 1);

                               if (target)
                                   DoCast(target, SpellIds.TELEPORT);

                               task.Repeat(TimeSpan.FromSeconds(20));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);

        if (Instance.GetBossState(DataTypes.MAJORDOMO_EXECUTUS) != EncounterState.Done)
        {
            if (!UpdateVictim())
                return;

            if (!Me.FindNearestCreature(McCreatureIds.FLAMEWAKER_HEALER, 100.0f) &&
                !Me.FindNearestCreature(McCreatureIds.FLAMEWAKER_ELITE, 100.0f))
            {
                Instance.UpdateEncounterStateForKilledCreature(Me.Entry, Me);
                Me.Faction = (uint)FactionTemplates.Friendly;
                EnterEvadeMode();
                Talk(TextIds.SAY_DEFEAT);
                _JustDied();

                Scheduler.Schedule(TimeSpan.FromSeconds(32),
                                   (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                              {
                                                                                  Me.NearTeleportTo(McMiscConst.RagnarosTelePos.X, McMiscConst.RagnarosTelePos.Y, McMiscConst.RagnarosTelePos.Z, McMiscConst.RagnarosTelePos.Orientation);
                                                                                  Me.SetNpcFlag(NPCFlags.Gossip);
                                                                              }));

                return;
            }

            if (Me.HasUnitState(UnitState.Casting))
                return;

            if (HealthBelowPct(50))
                DoCast(Me, SpellIds.AEGIS_OF_RAGNAROS, new CastSpellExtraArgs(true));

            DoMeleeAttackIfReady();
        }
    }

    public override void DoAction(int action)
    {
        if (action == ActionIds.START_RAGNAROS)
        {
            Me.RemoveNpcFlag(NPCFlags.Gossip);
            Talk(TextIds.SAY_SUMMON_MAJ);

            Scheduler.Schedule(TimeSpan.FromSeconds(8), task => { Instance.Instance.SummonCreature(McCreatureIds.RAGNAROS, McMiscConst.RagnarosSummonPos); });
            Scheduler.Schedule(TimeSpan.FromSeconds(24), task => { Talk(TextIds.SAY_ARRIVAL2_MAJ); });
        }
        else if (action == ActionIds.START_RAGNAROS_ALT)
        {
            Me.Faction = (uint)FactionTemplates.Friendly;
            Me.SetNpcFlag(NPCFlags.Gossip);
        }
    }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        if (menuId == TextIds.MENU_OPTION_YOU_CHALLENGED_US &&
            gossipListId == TextIds.OPTION_ID_YOU_CHALLENGED_US)
        {
            player.CloseGossipMenu();
            DoAction(ActionIds.START_RAGNAROS);
        }

        return false;
    }
}