// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Golemagg;

internal struct SpellIds
{
    // Golemagg
    public const uint Magmasplash = 13879;
    public const uint Pyroblast = 20228;
    public const uint Earthquake = 19798;
    public const uint Enrage = 19953;
    public const uint GolemaggTrust = 20553;

    // Core Rager
    public const uint Mangle = 19820;
}

internal struct TextIds
{
    public const uint EmoteLowhp = 0;
}

[Script]
internal class boss_golemagg : BossAI
{
    public boss_golemagg(Creature creature) : base(creature, DataTypes.GolemaggTheIncinerator) { }

    public override void Reset()
    {
        base.Reset();
        DoCast(Me, SpellIds.Magmasplash, new CastSpellExtraArgs(true));
    }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(7),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0);

                               if (target)
                                   DoCast(target, SpellIds.Pyroblast);

                               task.Repeat(TimeSpan.FromSeconds(7));
                           });
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!HealthBelowPct(10) ||
            Me.HasAura(SpellIds.Enrage))
            return;

        DoCast(Me, SpellIds.Enrage, new CastSpellExtraArgs(true));

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               DoCastVictim(SpellIds.Earthquake);
                               task.Repeat(TimeSpan.FromSeconds(3));
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
internal class npc_core_rager : ScriptedAI
{
    private readonly InstanceScript _instance;

    public npc_core_rager(Creature creature) : base(creature)
    {
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(7),
                           task => // These times are probably wrong
                           {
                               DoCastVictim(SpellIds.Mangle);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (HealthAbovePct(50) ||
            _instance == null)
            return;

        var pGolemagg = ObjectAccessor.GetCreature(Me, _instance.GetGuidData(DataTypes.GolemaggTheIncinerator));

        if (pGolemagg)
            if (pGolemagg.IsAlive)
            {
                Me.AddAura(SpellIds.GolemaggTrust, Me);
                Talk(TextIds.EmoteLowhp);
                Me.SetFullHealth();
            }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}