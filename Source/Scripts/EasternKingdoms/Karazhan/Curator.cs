// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Karazhan.Curator;

internal struct SpellIds
{
    public const uint HATEFUL_BOLT = 30383;
    public const uint EVOCATION = 30254;
    public const uint ARCANE_INFUSION = 30403;
    public const uint BERSERK = 26662;
    public const uint SUMMON_ASTRAL_FLARE_NE = 30236;
    public const uint SUMMON_ASTRAL_FLARE_NW = 30239;
    public const uint SUMMON_ASTRAL_FLARE_SE = 30240;
    public const uint SUMMON_ASTRAL_FLARE_SW = 30241;
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_SUMMON = 1;
    public const uint SAY_EVOCATE = 2;
    public const uint SAY_ENRAGE = 3;
    public const uint SAY_KILL = 4;
    public const uint SAY_DEATH = 5;
}

internal struct MiscConst
{
    public const uint GROUP_ASTRAL_FLARE = 1;
}

[Script]
internal class BossCurator : BossAI
{
    private bool _infused;

    public BossCurator(Creature creature) : base(creature, DataTypes.CURATOR) { }

    public override void Reset()
    {
        _Reset();
        _infused = false;
    }

    public override void KilledUnit(Unit victim)
    {
        if (victim.IsPlayer)
            Talk(TextIds.SAY_KILL);
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        Talk(TextIds.SAY_DEATH);
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.SAY_AGGRO);

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.MaxThreat, 1);

                               if (target)
                                   DoCast(target, SpellIds.HATEFUL_BOLT);

                               task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           MiscConst.GROUP_ASTRAL_FLARE,
                           task =>
                           {
                               if (RandomHelper.randChance(50))
                                   Talk(TextIds.SAY_SUMMON);

                               DoCastSelf(RandomHelper.RAND(SpellIds.SUMMON_ASTRAL_FLARE_NE, SpellIds.SUMMON_ASTRAL_FLARE_NW, SpellIds.SUMMON_ASTRAL_FLARE_SE, SpellIds.SUMMON_ASTRAL_FLARE_SW), new CastSpellExtraArgs(true));

                               var mana = (Me.GetMaxPower(PowerType.Mana) / 10);

                               if (mana != 0)
                               {
                                   Me.ModifyPower(PowerType.Mana, -mana);

                                   if (Me.GetPower(PowerType.Mana) * 100 / Me.GetMaxPower(PowerType.Mana) < 10)
                                   {
                                       Talk(TextIds.SAY_EVOCATE);
                                       Me.InterruptNonMeleeSpells(false);
                                       DoCastSelf(SpellIds.EVOCATION);
                                   }
                               }

                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromMinutes(12),
                           scheduleTasks =>
                           {
                               Talk(TextIds.SAY_ENRAGE);
                               DoCastSelf(SpellIds.BERSERK, new CastSpellExtraArgs(true));
                           });
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!HealthAbovePct(15) &&
            !_infused)
        {
            _infused = true;
            Scheduler.Schedule(TimeSpan.FromMilliseconds(1), task => DoCastSelf(SpellIds.ARCANE_INFUSION, new CastSpellExtraArgs(true)));
            Scheduler.CancelGroup(MiscConst.GROUP_ASTRAL_FLARE);
        }
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}

[Script]
internal class NPCCuratorAstralFlare : ScriptedAI
{
    public NPCCuratorAstralFlare(Creature creature) : base(creature)
    {
        Me.ReactState = ReactStates.Passive;
    }

    public override void Reset()
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               Me.ReactState = ReactStates.Aggressive;
                               Me.RemoveUnitFlag(UnitFlags.Uninteractible);
                               DoZoneInCombat();
                           });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}