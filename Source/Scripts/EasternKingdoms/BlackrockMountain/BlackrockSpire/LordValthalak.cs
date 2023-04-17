// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire;

internal struct SpellIds
{
    public const uint FRENZY = 8269;
    public const uint SUMMON_SPECTRAL_ASSASSIN = 27249;
    public const uint SHADOW_BOLT_VOLLEY = 27382;
    public const uint SHADOW_WRATH = 27286;
}

[Script]
internal class BossLordValthalak : BossAI
{
    private bool _frenzy15;
    private bool _frenzy40;

    public BossLordValthalak(Creature creature) : base(creature, DataTypes.LORD_VALTHALAK)
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

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           TimeSpan.FromSeconds(8),
                           1,
                           task =>
                           {
                               DoCast(Me, SpellIds.SUMMON_SPECTRAL_ASSASSIN);
                               task.Repeat(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(9),
                           TimeSpan.FromSeconds(18),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHADOW_WRATH);
                               task.Repeat(TimeSpan.FromSeconds(19), TimeSpan.FromSeconds(24));
                           });
    }

    public override void JustDied(Unit killer)
    {
        Instance.SetBossState(DataTypes.LORD_VALTHALAK, EncounterState.Done);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff);

        if (Me.HasUnitState(UnitState.Casting))
            return;

        if (!_frenzy40)
            if (HealthBelowPct(40))
            {
                DoCast(Me, SpellIds.FRENZY);
                Scheduler.CancelGroup(1);
                _frenzy40 = true;
            }

        if (!_frenzy15)
            if (HealthBelowPct(15))
            {
                DoCast(Me, SpellIds.FRENZY);

                Scheduler.Schedule(TimeSpan.FromSeconds(7),
                                   TimeSpan.FromSeconds(14),
                                   task =>
                                   {
                                       DoCastVictim(SpellIds.SHADOW_BOLT_VOLLEY);
                                       task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6));
                                   });

                _frenzy15 = true;
            }

        DoMeleeAttackIfReady();
    }

    private void Initialize()
    {
        _frenzy40 = false;
        _frenzy15 = false;
    }
}