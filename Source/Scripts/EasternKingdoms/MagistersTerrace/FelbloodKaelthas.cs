// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.EasternKingdoms.MagistersTerrace.FelbloodKaelthas;

internal struct TextIds
{
    // Kael'thas Sunstrider
    public const uint SAY_INTRO1 = 0;
    public const uint SAY_INTRO2 = 1;
    public const uint SAY_GRAVITY_LAPSE1 = 2;
    public const uint SAY_GRAVITY_LAPSE2 = 3;
    public const uint SAY_POWER_FEEDBACK = 4;
    public const uint SAY_SUMMON_PHOENIX = 5;
    public const uint SAY_ANNOUNCE_PYROBLAST = 6;
    public const uint SAY_FLAME_STRIKE = 7;
    public const uint SAY_DEATH = 8;
}

internal struct SpellIds
{
    // Kael'thas Sunstrider
    public const uint FIREBALL = 44189;
    public const uint GRAVITY_LAPSE = 49887;
    public const uint H_GRAVITY_LAPSE = 44226;
    public const uint GRAVITY_LAPSE_CENTER_TELEPORT = 44218;
    public const uint GRAVITY_LAPSE_LEFT_TELEPORT = 44219;
    public const uint GRAVITY_LAPSE_FRONT_LEFT_TELEPORT = 44220;
    public const uint GRAVITY_LAPSE_FRONT_TELEPORT = 44221;
    public const uint GRAVITY_LAPSE_FRONT_RIGHT_TELEPORT = 44222;
    public const uint GRAVITY_LAPSE_RIGHT_TELEPORT = 44223;
    public const uint GRAVITY_LAPSE_INITIAL = 44224;
    public const uint GRAVITY_LAPSE_FLY = 44227;
    public const uint GRAVITY_LAPSE_BEAM_VISUAL_PERIODIC = 44251;
    public const uint SUMMON_ARCANE_SPHERE = 44265;
    public const uint FLAME_STRIKE = 46162;
    public const uint SHOCK_BARRIER = 46165;
    public const uint POWER_FEEDBACK = 44233;
    public const uint H_POWER_FEEDBACK = 47109;
    public const uint PYROBLAST = 36819;
    public const uint PHOENIX = 44194;
    public const uint EMOTE_TALK_EXCLAMATION = 48348;
    public const uint EMOTE_POINT = 48349;
    public const uint EMOTE_ROAR = 48350;
    public const uint CLEAR_FLIGHT = 44232;
    public const uint QUITE_SUICIDE = 3617; // Serverside public const uint 

    // Flame Strike
    public const uint FLAME_STRIKE_DUMMY = 44191;
    public const uint FLAME_STRIKE_DAMAGE = 44190;

    // Phoenix
    public const uint REBIRTH = 44196;
    public const uint BURN = 44197;
    public const uint EMBER_BLAST = 44199;
    public const uint SUMMON_PHOENIX_EGG = 44195; // Serverside public const uint 
    public const uint FULL_HEAL = 17683;
}

internal enum Phase
{
    Intro = 0,
    One = 1,
    Two = 2,
    Outro = 3
}

internal struct MiscConst
{
    public static uint[] GravityLapseTeleportSpells =
    {
        SpellIds.GRAVITY_LAPSE_LEFT_TELEPORT, SpellIds.GRAVITY_LAPSE_FRONT_LEFT_TELEPORT, SpellIds.GRAVITY_LAPSE_FRONT_TELEPORT, SpellIds.GRAVITY_LAPSE_FRONT_RIGHT_TELEPORT, SpellIds.GRAVITY_LAPSE_RIGHT_TELEPORT
    };
}

[Script]
internal class BossFelbloodKaelthas : BossAI
{
    private static readonly uint GroupFireBall = 1;
    private bool _firstGravityLapse;
    private byte _gravityLapseTargetCount;

    private Phase _phase;

    public BossFelbloodKaelthas(Creature creature) : base(creature, DataTypes.KAELTHAS_SUNSTRIDER)
    {
        Initialize();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        _phase = Phase.One;

        Scheduler.Schedule(TimeSpan.FromMilliseconds(1),
                           GroupFireBall,
                           task =>
                           {
                               DoCastVictim(SpellIds.FIREBALL);
                               task.Repeat(TimeSpan.FromSeconds(2.5));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(44),
                           task =>
                           {
                               Talk(TextIds.SAY_FLAME_STRIKE);
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 40.0f, true);

                               if (target)
                                   DoCast(target, SpellIds.FLAME_STRIKE);

                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               Talk(TextIds.SAY_SUMMON_PHOENIX);
                               DoCastSelf(SpellIds.PHOENIX);
                               task.Repeat(TimeSpan.FromSeconds(45));
                           });

        if (IsHeroic())
            Scheduler.Schedule(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1),
                               task =>
                               {
                                   Talk(TextIds.SAY_ANNOUNCE_PYROBLAST);
                                   DoCastSelf(SpellIds.SHOCK_BARRIER);
                                   task.RescheduleGroup(GroupFireBall, TimeSpan.FromSeconds(2.5));

                                   task.Schedule(TimeSpan.FromSeconds(2),
                                                 pyroBlastTask =>
                                                 {
                                                     var target = SelectTarget(SelectTargetMethod.Random, 0, 40.0f, true);

                                                     if (target != null)
                                                         DoCast(target, SpellIds.PYROBLAST);
                                                 });

                                   task.Repeat(TimeSpan.FromMinutes(1));
                               });
    }

    public override void Reset()
    {
        _Reset();
        Initialize();
        _phase = Phase.Intro;
    }

    public override void JustDied(Unit killer)
    {
        // No _JustDied() here because otherwise we would reset the events which will trigger the death sequence twice.
        Instance.SetBossState(DataTypes.KAELTHAS_SUNSTRIDER, EncounterState.Done);
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
        DoCastAOE(SpellIds.CLEAR_FLIGHT, new CastSpellExtraArgs(true));
        _EnterEvadeMode();
        Summons.DespawnAll();
        _DespawnAtEvade();
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        // Checking for lethal Damage first so we trigger the outro phase without triggering phase two in case of oneshot attacks
        if (damage >= Me.Health &&
            _phase != Phase.Outro)
        {
            Me.AttackStop();
            Me.ReactState = ReactStates.Passive;
            Me.InterruptNonMeleeSpells(true);
            Me.RemoveAura(DungeonMode(SpellIds.POWER_FEEDBACK, SpellIds.H_POWER_FEEDBACK));
            Summons.DespawnAll();
            DoCastAOE(SpellIds.CLEAR_FLIGHT);
            Talk(TextIds.SAY_DEATH);

            _phase = Phase.Outro;
            Scheduler.CancelAll();

            Scheduler.Schedule(TimeSpan.FromSeconds(1), task => { DoCastSelf(SpellIds.EMOTE_TALK_EXCLAMATION); });
            Scheduler.Schedule(TimeSpan.FromSeconds(3.8), task => { DoCastSelf(SpellIds.EMOTE_POINT); });
            Scheduler.Schedule(TimeSpan.FromSeconds(7.4), task => { DoCastSelf(SpellIds.EMOTE_ROAR); });
            Scheduler.Schedule(TimeSpan.FromSeconds(10), task => { DoCastSelf(SpellIds.EMOTE_ROAR); });
            Scheduler.Schedule(TimeSpan.FromSeconds(11), task => { DoCastSelf(SpellIds.QUITE_SUICIDE); });
        }

        // Phase two checks. Skip phase two if we are in the outro already
        if (Me.HealthBelowPctDamaged(50, damage) &&
            _phase != Phase.Two &&
            _phase != Phase.Outro)
        {
            _phase = Phase.Two;
            Scheduler.CancelAll();

            Scheduler.Schedule(TimeSpan.FromMilliseconds(1),
                               task =>
                               {
                                   Talk(_firstGravityLapse ? TextIds.SAY_GRAVITY_LAPSE1 : TextIds.SAY_GRAVITY_LAPSE2);
                                   _firstGravityLapse = false;
                                   Me.ReactState = ReactStates.Passive;
                                   Me.AttackStop();
                                   Me.MotionMaster.Clear();

                                   task.Schedule(TimeSpan.FromSeconds(1),
                                                 _ =>
                                                 {
                                                     DoCastSelf(SpellIds.GRAVITY_LAPSE_CENTER_TELEPORT);

                                                     task.Schedule(TimeSpan.FromSeconds(1),
                                                                   _ =>
                                                                   {
                                                                       _gravityLapseTargetCount = 0;
                                                                       DoCastAOE(SpellIds.GRAVITY_LAPSE_INITIAL);

                                                                       Scheduler.Schedule(TimeSpan.FromSeconds(4),
                                                                                          _ =>
                                                                                          {
                                                                                              for (byte i = 0; i < 3; i++)
                                                                                                  DoCastSelf(SpellIds.SUMMON_ARCANE_SPHERE, new CastSpellExtraArgs(true));
                                                                                          });

                                                                       Scheduler.Schedule(TimeSpan.FromSeconds(5), _ => { DoCastAOE(SpellIds.GRAVITY_LAPSE_BEAM_VISUAL_PERIODIC); });

                                                                       Scheduler.Schedule(TimeSpan.FromSeconds(35),
                                                                                          _ =>
                                                                                          {
                                                                                              Talk(TextIds.SAY_POWER_FEEDBACK);
                                                                                              DoCastAOE(SpellIds.CLEAR_FLIGHT);
                                                                                              DoCastSelf(DungeonMode(SpellIds.POWER_FEEDBACK, SpellIds.H_POWER_FEEDBACK));
                                                                                              Summons.DespawnEntry(CreatureIds.ARCANE_SPHERE);
                                                                                              task.Repeat(TimeSpan.FromSeconds(11));
                                                                                          });
                                                                   });
                                                 });
                               });
        }

        // Kael'thas may only kill himself via Quite Suicide
        if (damage >= Me.Health &&
            attacker != Me)
            damage = (uint)(Me.Health - 1);
    }

    public override void SetData(uint type, uint data)
    {
        if (type == DataTypes.KAELTHAS_INTRO)
        {
            // skip the intro if Kael'thas is engaged already
            if (_phase != Phase.Intro)
                return;

            Me.SetImmuneToPC(true);

            Scheduler.Schedule(TimeSpan.FromSeconds(6),
                               task =>
                               {
                                   Talk(TextIds.SAY_INTRO1);
                                   Me.EmoteState = Emote.StateTalk;

                                   Scheduler.Schedule(TimeSpan.FromSeconds(20.6),
                                                      _ =>
                                                      {
                                                          Talk(TextIds.SAY_INTRO2);

                                                          Scheduler.Schedule(TimeSpan.FromSeconds(15) + TimeSpan.FromMilliseconds(500),
                                                                             _ =>
                                                                             {
                                                                                 Me.EmoteState = Emote.OneshotNone;
                                                                                 Me.SetImmuneToPC(false);
                                                                             });
                                                      });

                                   Scheduler.Schedule(TimeSpan.FromSeconds(15.6), _ => Me.HandleEmoteCommand(Emote.OneshotLaughNoSheathe));
                               });
        }
    }

    public override void SpellHitTarget(WorldObject target, SpellInfo spellInfo)
    {
        var unitTarget = target.AsUnit;

        if (!unitTarget)
            return;

        switch (spellInfo.Id)
        {
            case SpellIds.GRAVITY_LAPSE_INITIAL:
            {
                DoCast(unitTarget, MiscConst.GravityLapseTeleportSpells[_gravityLapseTargetCount], new CastSpellExtraArgs(true));

                target.Events.AddEventAtOffset(() =>
                                               {
                                                   target.SpellFactory.CastSpell(target, DungeonMode(SpellIds.GRAVITY_LAPSE, SpellIds.H_GRAVITY_LAPSE));
                                                   target.SpellFactory.CastSpell(target, SpellIds.GRAVITY_LAPSE_FLY);
                                               },
                                               TimeSpan.FromMilliseconds(400));

                _gravityLapseTargetCount++;

                break;
            }
            case SpellIds.CLEAR_FLIGHT:
                unitTarget.RemoveAura(SpellIds.GRAVITY_LAPSE_FLY);
                unitTarget.RemoveAura(DungeonMode(SpellIds.GRAVITY_LAPSE, SpellIds.H_GRAVITY_LAPSE));

                break;
        }
    }

    public override void JustSummoned(Creature summon)
    {
        Summons.Summon(summon);

        switch (summon.Entry)
        {
            case CreatureIds.ARCANE_SPHERE:
                var target = SelectTarget(SelectTargetMethod.Random, 0, 70.0f, true);

                if (target)
                    summon.MotionMaster.MoveFollow(target, 0.0f, 0.0f);

                break;
            case CreatureIds.FLAME_STRIKE:
                summon.SpellFactory.CastSpell(summon, SpellIds.FLAME_STRIKE_DUMMY);
                summon.DespawnOrUnsummon(TimeSpan.FromSeconds(15));

                break;
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim() &&
            _phase != Phase.Intro)
            return;

        Scheduler.Update(diff);
    }

    private void Initialize()
    {
        _gravityLapseTargetCount = 0;
        _firstGravityLapse = true;
    }
}

[Script]
internal class NPCFelbloodKaelthasPhoenix : ScriptedAI
{
    private readonly InstanceScript _instance;
    private ObjectGuid _eggGUID;

    private bool _isInEgg;

    public NPCFelbloodKaelthasPhoenix(Creature creature) : base(creature)
    {
        _instance = creature.InstanceScript;
        Initialize();
    }

    public override void IsSummonedBy(WorldObject summoner)
    {
        DoZoneInCombat();
        DoCastSelf(SpellIds.BURN);
        DoCastSelf(SpellIds.REBIRTH);
        Scheduler.Schedule(TimeSpan.FromSeconds(2), task => Me.ReactState = ReactStates.Aggressive);
    }

    public override void JustEngagedWith(Unit who) { }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (damage >= Me.Health)
        {
            if (!_isInEgg)
            {
                Me.AttackStop();
                Me.ReactState = ReactStates.Passive;
                Me.RemoveAllAuras();
                Me.SetUnitFlag(UnitFlags.Uninteractible);
                DoCastSelf(SpellIds.EMBER_BLAST);
                // DoCastSelf(SpellSummonPhoenixEgg); -- We do a manual summon for now. Feel free to move it to spelleffect_dbc
                var egg = DoSummon(CreatureIds.PHOENIX_EGG, Me.Location, TimeSpan.FromSeconds(0));

                if (egg)
                {
                    var kaelthas = _instance.GetCreature(DataTypes.KAELTHAS_SUNSTRIDER);

                    if (kaelthas)
                    {
                        kaelthas.AI.JustSummoned(egg);
                        _eggGUID = egg.GUID;
                    }
                }

                Scheduler.Schedule(TimeSpan.FromSeconds(15),
                                   task =>
                                   {
                                       var egg = ObjectAccessor.GetCreature(Me, _eggGUID);

                                       if (egg)
                                           egg.DespawnOrUnsummon();

                                       Me.RemoveAllAuras();

                                       task.Schedule(TimeSpan.FromSeconds(2),
                                                     rebirthTask =>
                                                     {
                                                         DoCastSelf(SpellIds.REBIRTH);

                                                         rebirthTask.Schedule(TimeSpan.FromSeconds(2),
                                                                              engageTask =>
                                                                              {
                                                                                  _isInEgg = false;
                                                                                  DoCastSelf(SpellIds.FULL_HEAL);
                                                                                  DoCastSelf(SpellIds.BURN);
                                                                                  Me.RemoveUnitFlag(UnitFlags.Uninteractible);
                                                                                  engageTask.Schedule(TimeSpan.FromSeconds(2), task => Me.ReactState = ReactStates.Aggressive);
                                                                              });
                                                     });
                                   });

                _isInEgg = true;
            }

            damage = (uint)(Me.Health - 1);
        }
    }

    public override void SummonedCreatureDies(Creature summon, Unit killer)
    {
        // Egg has been destroyed within 15 seconds so we lose the phoenix.
        Me.DespawnOrUnsummon();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize()
    {
        Me.ReactState = ReactStates.Passive;
        _isInEgg = false;
    }
}

[Script] // 44191 - Flame Strike
internal class SpellFelbloodKaelthasFlameStrike : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        if (target)
            target.SpellFactory.CastSpell(target, SpellIds.FLAME_STRIKE_DAMAGE);
    }
}