// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Golemagg;

internal struct SpellIds
{
    // Golemagg
    public const uint MAGMASPLASH = 13879;
    public const uint PYROBLAST = 20228;
    public const uint EARTHQUAKE = 19798;
    public const uint ENRAGE = 19953;
    public const uint GOLEMAGG_TRUST = 20553;

    // Core Rager
    public const uint MANGLE = 19820;
}

internal struct TextIds
{
    public const uint EMOTE_LOWHP = 0;
}

[Script]
internal class BossGolemagg : BossAI
{
    public BossGolemagg(Creature creature) : base(creature, DataTypes.GOLEMAGG_THE_INCINERATOR) { }

    public override void Reset()
    {
        base.Reset();
        DoCast(Me, SpellIds.MAGMASPLASH, new CastSpellExtraArgs(true));
    }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(7),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0);

                               if (target)
                                   DoCast(target, SpellIds.PYROBLAST);

                               task.Repeat(TimeSpan.FromSeconds(7));
                           });
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!HealthBelowPct(10) ||
            Me.HasAura(SpellIds.ENRAGE))
            return;

        DoCast(Me, SpellIds.ENRAGE, new CastSpellExtraArgs(true));

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               DoCastVictim(SpellIds.EARTHQUAKE);
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
internal class NPCCoreRager : ScriptedAI
{
    private readonly InstanceScript _instance;

    public NPCCoreRager(Creature creature) : base(creature)
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
                               DoCastVictim(SpellIds.MANGLE);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (HealthAbovePct(50) ||
            _instance == null)
            return;

        var pGolemagg = ObjectAccessor.GetCreature(Me, _instance.GetGuidData(DataTypes.GOLEMAGG_THE_INCINERATOR));

        if (pGolemagg)
            if (pGolemagg.IsAlive)
            {
                Me.AddAura(SpellIds.GOLEMAGG_TRUST, Me);
                Talk(TextIds.EMOTE_LOWHP);
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