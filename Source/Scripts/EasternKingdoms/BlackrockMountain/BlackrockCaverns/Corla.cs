// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.Corla;

internal struct SpellIds
{
    public const uint Evolution = 75610;
    public const uint DrainEssense = 75645;
    public const uint ShadowPower = 35322;
    public const uint HShadowPower = 39193;
}

internal struct TextIds
{
    public const uint YellAggro = 0;
    public const uint YellKill = 1;
    public const uint YellEvolvedZealot = 2;
    public const uint YellDeath = 3;

    public const uint EmoteEvolvedZealot = 4;
}

[Script]
internal class boss_corla : BossAI
{
    private bool combatPhase;

    public boss_corla(Creature creature) : base(creature, DataTypes.Corla) { }

    public override void Reset()
    {
        _Reset();
        combatPhase = false;

        Scheduler.SetValidator(() => !combatPhase);

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           drainTask =>
                           {
                               DoCast(Me, SpellIds.DrainEssense);

                               drainTask.Schedule(TimeSpan.FromSeconds(15),
                                                  stopDrainTask =>
                                                  {
                                                      Me.InterruptSpell(CurrentSpellTypes.Channeled);

                                                      stopDrainTask.Schedule(TimeSpan.FromSeconds(2),
                                                                             evolutionTask =>
                                                                             {
                                                                                 DoCast(Me, SpellIds.Evolution);
                                                                                 drainTask.Repeat(TimeSpan.FromSeconds(2));
                                                                             });
                                                  });
                           });
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.YellAggro);
        Scheduler.CancelAll();
        combatPhase = true;
    }

    public override void KilledUnit(Unit who)
    {
        if (who.IsPlayer)
            Talk(TextIds.YellKill);
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        Talk(TextIds.YellDeath);
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);

        DoMeleeAttackIfReady();
    }
}