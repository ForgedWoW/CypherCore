// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.MotherSmolderweb;

internal struct SpellIds
{
    public const uint CRYSTALIZE = 16104;
    public const uint MOTHERSMILK = 16468;
    public const uint SUMMON_SPIRE_SPIDERLING = 16103;
}

[Script]
internal class BossMotherSmolderweb : BossAI
{
    public BossMotherSmolderweb(Creature creature) : base(creature, DataTypes.MOTHER_SMOLDERWEB) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCast(Me, SpellIds.CRYSTALIZE);
                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCast(Me, SpellIds.MOTHERSMILK);
                               task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(12500));
                           });
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
    }

    public override void DamageTaken(Unit doneBy, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (Me.Health <= damage)
            DoCast(Me, SpellIds.SUMMON_SPIRE_SPIDERLING, new CastSpellExtraArgs(true));
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}