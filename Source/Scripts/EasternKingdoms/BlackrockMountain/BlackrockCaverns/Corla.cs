// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.Corla;

internal struct SpellIds
{
    public const uint EVOLUTION = 75610;
    public const uint DRAIN_ESSENSE = 75645;
    public const uint SHADOW_POWER = 35322;
    public const uint H_SHADOW_POWER = 39193;
}

internal struct TextIds
{
    public const uint YELL_AGGRO = 0;
    public const uint YELL_KILL = 1;
    public const uint YELL_EVOLVED_ZEALOT = 2;
    public const uint YELL_DEATH = 3;

    public const uint EMOTE_EVOLVED_ZEALOT = 4;
}

[Script]
internal class BossCorla : BossAI
{
    private bool _combatPhase;

    public BossCorla(Creature creature) : base(creature, DataTypes.CORLA) { }

    public override void Reset()
    {
        _Reset();
        _combatPhase = false;

        Scheduler.SetValidator(() => !_combatPhase);

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           drainTask =>
                           {
                               DoCast(Me, SpellIds.DRAIN_ESSENSE);

                               drainTask.Schedule(TimeSpan.FromSeconds(15),
                                                  stopDrainTask =>
                                                  {
                                                      Me.InterruptSpell(CurrentSpellTypes.Channeled);

                                                      stopDrainTask.Schedule(TimeSpan.FromSeconds(2),
                                                                             evolutionTask =>
                                                                             {
                                                                                 DoCast(Me, SpellIds.EVOLUTION);
                                                                                 drainTask.Repeat(TimeSpan.FromSeconds(2));
                                                                             });
                                                  });
                           });
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.YELL_AGGRO);
        Scheduler.CancelAll();
        _combatPhase = true;
    }

    public override void KilledUnit(Unit who)
    {
        if (who.IsPlayer)
            Talk(TextIds.YELL_KILL);
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        Talk(TextIds.YELL_DEATH);
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);

        DoMeleeAttackIfReady();
    }
}