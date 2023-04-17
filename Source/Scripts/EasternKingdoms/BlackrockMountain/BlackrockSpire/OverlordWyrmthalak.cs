// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.OverlordWyrmthalak;

internal struct SpellIds
{
    public const uint BLASTWAVE = 11130;
    public const uint SHOUT = 23511;
    public const uint CLEAVE = 20691;
    public const uint KNOCKAWAY = 20686;
}

internal struct MiscConst
{
    public const uint NPC_SPIRESTONE_WARLORD = 9216;
    public const uint NPC_SMOLDERTHORN_BERSERKER = 9268;

    public static Position SummonLocation = new(-39.355f, -513.456f, 88.472f, 4.679f);
    public static Position SummonLocation2 = new(-49.875f, -511.896f, 88.195f, 4.613f);
}

[Script]
internal class BossOverlordWyrmthalak : BossAI
{
    private bool _summoned;

    public BossOverlordWyrmthalak(Creature creature) : base(creature, DataTypes.OVERLORD_WYRMTHALAK)
    {
        Initialize();
    }

    public override void Reset()
    {
        _Reset();
        Initialize();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.BLASTWAVE);
                               task.Repeat(TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHOUT);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(7));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.KNOCKAWAY);
                               task.Repeat(TimeSpan.FromSeconds(14));
                           });
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (!_summoned &&
            HealthBelowPct(51))
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target)
            {
                Creature warlord = Me.SummonCreature(MiscConst.NPC_SPIRESTONE_WARLORD, MiscConst.SummonLocation, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));

                if (warlord)
                    warlord.AI.AttackStart(target);

                Creature berserker = Me.SummonCreature(MiscConst.NPC_SMOLDERTHORN_BERSERKER, MiscConst.SummonLocation2, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));

                if (berserker)
                    berserker.AI.AttackStart(target);

                _summoned = true;
            }
        }

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize()
    {
        _summoned = false;
    }
}