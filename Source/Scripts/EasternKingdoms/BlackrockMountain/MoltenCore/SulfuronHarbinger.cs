// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Sulfuron;

internal struct SpellIds
{
    // Sulfuron Harbringer
    public const uint DARK_STRIKE = 19777;
    public const uint DEMORALIZING_SHOUT = 19778;
    public const uint INSPIRE = 19779;
    public const uint KNOCKDOWN = 19780;
    public const uint FLAMESPEAR = 19781;

    // Adds
    public const uint HEAL = 19775;
    public const uint SHADOWWORDPAIN = 19776;
    public const uint IMMOLATE = 20294;
}

[Script]
internal class BossSulfuron : BossAI
{
    public BossSulfuron(Creature creature) : base(creature, DataTypes.SULFURON_HARBINGER) { }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCast(Me, SpellIds.DARK_STRIKE);
                               task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(18));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCastVictim(SpellIds.DEMORALIZING_SHOUT);
                               task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(13),
                           task =>
                           {
                               var healers = DoFindFriendlyMissingBuff(45.0f, SpellIds.INSPIRE);

                               if (!healers.Empty())
                                   DoCast(healers.SelectRandom(), SpellIds.INSPIRE);

                               DoCast(Me, SpellIds.INSPIRE);
                               task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(26));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.KNOCKDOWN);
                               task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

                               if (target)
                                   DoCast(target, SpellIds.FLAMESPEAR);

                               task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(16));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}

[Script]
internal class NPCFlamewakerPriest : ScriptedAI
{
    public NPCFlamewakerPriest(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustDied(Unit killer)
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           TimeSpan.FromSeconds(30),
                           task =>
                           {
                               var target = DoSelectLowestHpFriendly(60.0f, 1);

                               if (target)
                                   DoCast(target, SpellIds.HEAL);

                               task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.SHADOWWORDPAIN);

                               if (target)
                                   DoCast(target, SpellIds.SHADOWWORDPAIN);

                               task.Repeat(TimeSpan.FromSeconds(18), TimeSpan.FromSeconds(26));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.IMMOLATE);

                               if (target)
                                   DoCast(target, SpellIds.IMMOLATE);

                               task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}