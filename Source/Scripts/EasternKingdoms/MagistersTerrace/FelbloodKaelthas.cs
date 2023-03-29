// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.EasternKingdoms.MagistersTerrace.FelbloodKaelthas;

internal struct TextIds
{
    // Kael'thas Sunstrider
    public const uint SayIntro1 = 0;
    public const uint SayIntro2 = 1;
    public const uint SayGravityLapse1 = 2;
    public const uint SayGravityLapse2 = 3;
    public const uint SayPowerFeedback = 4;
    public const uint SaySummonPhoenix = 5;
    public const uint SayAnnouncePyroblast = 6;
    public const uint SayFlameStrike = 7;
    public const uint SayDeath = 8;
}

internal struct SpellIds
{
    // Kael'thas Sunstrider
    public const uint Fireball = 44189;
    public const uint GravityLapse = 49887;
    public const uint HGravityLapse = 44226;
    public const uint GravityLapseCenterTeleport = 44218;
    public const uint GravityLapseLeftTeleport = 44219;
    public const uint GravityLapseFrontLeftTeleport = 44220;
    public const uint GravityLapseFrontTeleport = 44221;
    public const uint GravityLapseFrontRightTeleport = 44222;
    public const uint GravityLapseRightTeleport = 44223;
    public const uint GravityLapseInitial = 44224;
    public const uint GravityLapseFly = 44227;
    public const uint GravityLapseBeamVisualPeriodic = 44251;
    public const uint SummonArcaneSphere = 44265;
    public const uint FlameStrike = 46162;
    public const uint ShockBarrier = 46165;
    public const uint PowerFeedback = 44233;
    public const uint HPowerFeedback = 47109;
    public const uint Pyroblast = 36819;
    public const uint Phoenix = 44194;
    public const uint EmoteTalkExclamation = 48348;
    public const uint EmotePoint = 48349;
    public const uint EmoteRoar = 48350;
    public const uint ClearFlight = 44232;
    public const uint QuiteSuicide = 3617; // Serverside public const uint 

    // Flame Strike
    public const uint FlameStrikeDummy = 44191;
    public const uint FlameStrikeDamage = 44190;

    // Phoenix
    public const uint Rebirth = 44196;
    public const uint Burn = 44197;
    public const uint EmberBlast = 44199;
    public const uint SummonPhoenixEgg = 44195; // Serverside public const uint 
    public const uint FullHeal = 17683;
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
        SpellIds.GravityLapseLeftTeleport, SpellIds.GravityLapseFrontLeftTeleport, SpellIds.GravityLapseFrontTeleport, SpellIds.GravityLapseFrontRightTeleport, SpellIds.GravityLapseRightTeleport
    };
}

[Script]
internal class boss_felblood_kaelthas : BossAI
{
    private static readonly uint groupFireBall = 1;
    private bool _firstGravityLapse;
    private byte _gravityLapseTargetCount;

    private Phase _phase;

    public boss_felblood_kaelthas(Creature creature) : base(creature, DataTypes.KaelthasSunstrider)
    {
        Initialize();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        _phase = Phase.One;

        Scheduler.Schedule(TimeSpan.FromMilliseconds(1),
                           groupFireBall,
                           task =>
                           {
                               DoCastVictim(SpellIds.Fireball);
                               task.Repeat(TimeSpan.FromSeconds(2.5));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(44),
                           task =>
                           {
                               Talk(TextIds.SayFlameStrike);
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 40.0f, true);

                               if (target)
                                   DoCast(target, SpellIds.FlameStrike);

                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               Talk(TextIds.SaySummonPhoenix);
                               DoCastSelf(SpellIds.Phoenix);
                               task.Repeat(TimeSpan.FromSeconds(45));
                           });

        if (IsHeroic())
            Scheduler.Schedule(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1),
                               task =>
                               {
                                   Talk(TextIds.SayAnnouncePyroblast);
                                   DoCastSelf(SpellIds.ShockBarrier);
                                   task.RescheduleGroup(groupFireBall, TimeSpan.FromSeconds(2.5));

                                   task.Schedule(TimeSpan.FromSeconds(2),
                                                 pyroBlastTask =>
                                                 {
                                                     var target = SelectTarget(SelectTargetMethod.Random, 0, 40.0f, true);

                                                     if (target != null)
                                                         DoCast(target, SpellIds.Pyroblast);
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
        Instance.SetBossState(DataTypes.KaelthasSunstrider, EncounterState.Done);
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
        DoCastAOE(SpellIds.ClearFlight, new CastSpellExtraArgs(true));
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
            Me.RemoveAura(DungeonMode(SpellIds.PowerFeedback, SpellIds.HPowerFeedback));
            Summons.DespawnAll();
            DoCastAOE(SpellIds.ClearFlight);
            Talk(TextIds.SayDeath);

            _phase = Phase.Outro;
            Scheduler.CancelAll();

            Scheduler.Schedule(TimeSpan.FromSeconds(1), task => { DoCastSelf(SpellIds.EmoteTalkExclamation); });
            Scheduler.Schedule(TimeSpan.FromSeconds(3.8), task => { DoCastSelf(SpellIds.EmotePoint); });
            Scheduler.Schedule(TimeSpan.FromSeconds(7.4), task => { DoCastSelf(SpellIds.EmoteRoar); });
            Scheduler.Schedule(TimeSpan.FromSeconds(10), task => { DoCastSelf(SpellIds.EmoteRoar); });
            Scheduler.Schedule(TimeSpan.FromSeconds(11), task => { DoCastSelf(SpellIds.QuiteSuicide); });
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
                                   Talk(_firstGravityLapse ? TextIds.SayGravityLapse1 : TextIds.SayGravityLapse2);
                                   _firstGravityLapse = false;
                                   Me.ReactState = ReactStates.Passive;
                                   Me.AttackStop();
                                   Me.MotionMaster.Clear();

                                   task.Schedule(TimeSpan.FromSeconds(1),
                                                 _ =>
                                                 {
                                                     DoCastSelf(SpellIds.GravityLapseCenterTeleport);

                                                     task.Schedule(TimeSpan.FromSeconds(1),
                                                                   _ =>
                                                                   {
                                                                       _gravityLapseTargetCount = 0;
                                                                       DoCastAOE(SpellIds.GravityLapseInitial);

                                                                       Scheduler.Schedule(TimeSpan.FromSeconds(4),
                                                                                          _ =>
                                                                                          {
                                                                                              for (byte i = 0; i < 3; i++)
                                                                                                  DoCastSelf(SpellIds.SummonArcaneSphere, new CastSpellExtraArgs(true));
                                                                                          });

                                                                       Scheduler.Schedule(TimeSpan.FromSeconds(5), _ => { DoCastAOE(SpellIds.GravityLapseBeamVisualPeriodic); });

                                                                       Scheduler.Schedule(TimeSpan.FromSeconds(35),
                                                                                          _ =>
                                                                                          {
                                                                                              Talk(TextIds.SayPowerFeedback);
                                                                                              DoCastAOE(SpellIds.ClearFlight);
                                                                                              DoCastSelf(DungeonMode(SpellIds.PowerFeedback, SpellIds.HPowerFeedback));
                                                                                              Summons.DespawnEntry(CreatureIds.ArcaneSphere);
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
        if (type == DataTypes.KaelthasIntro)
        {
            // skip the intro if Kael'thas is engaged already
            if (_phase != Phase.Intro)
                return;

            Me.SetImmuneToPC(true);

            Scheduler.Schedule(TimeSpan.FromSeconds(6),
                               task =>
                               {
                                   Talk(TextIds.SayIntro1);
                                   Me.EmoteState = Emote.StateTalk;

                                   Scheduler.Schedule(TimeSpan.FromSeconds(20.6),
                                                      _ =>
                                                      {
                                                          Talk(TextIds.SayIntro2);

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
            case SpellIds.GravityLapseInitial:
            {
                DoCast(unitTarget, MiscConst.GravityLapseTeleportSpells[_gravityLapseTargetCount], new CastSpellExtraArgs(true));

                target.Events.AddEventAtOffset(() =>
                                               {
                                                   target.CastSpell(target, DungeonMode(SpellIds.GravityLapse, SpellIds.HGravityLapse));
                                                   target.CastSpell(target, SpellIds.GravityLapseFly);
                                               },
                                               TimeSpan.FromMilliseconds(400));

                _gravityLapseTargetCount++;

                break;
            }
            case SpellIds.ClearFlight:
                unitTarget.RemoveAura(SpellIds.GravityLapseFly);
                unitTarget.RemoveAura(DungeonMode(SpellIds.GravityLapse, SpellIds.HGravityLapse));

                break;
            default:
                break;
        }
    }

    public override void JustSummoned(Creature summon)
    {
        Summons.Summon(summon);

        switch (summon.Entry)
        {
            case CreatureIds.ArcaneSphere:
                var target = SelectTarget(SelectTargetMethod.Random, 0, 70.0f, true);

                if (target)
                    summon.MotionMaster.MoveFollow(target, 0.0f, 0.0f);

                break;
            case CreatureIds.FlameStrike:
                summon.CastSpell(summon, SpellIds.FlameStrikeDummy);
                summon.DespawnOrUnsummon(TimeSpan.FromSeconds(15));

                break;
            default:
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
internal class npc_felblood_kaelthas_phoenix : ScriptedAI
{
    private readonly InstanceScript _instance;
    private ObjectGuid _eggGUID;

    private bool _isInEgg;

    public npc_felblood_kaelthas_phoenix(Creature creature) : base(creature)
    {
        _instance = creature.InstanceScript;
        Initialize();
    }

    public override void IsSummonedBy(WorldObject summoner)
    {
        DoZoneInCombat();
        DoCastSelf(SpellIds.Burn);
        DoCastSelf(SpellIds.Rebirth);
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
                DoCastSelf(SpellIds.EmberBlast);
                // DoCastSelf(SpellSummonPhoenixEgg); -- We do a manual summon for now. Feel free to move it to spelleffect_dbc
                var egg = DoSummon(CreatureIds.PhoenixEgg, Me.Location, TimeSpan.FromSeconds(0));

                if (egg)
                {
                    var kaelthas = _instance.GetCreature(DataTypes.KaelthasSunstrider);

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
                                                         DoCastSelf(SpellIds.Rebirth);

                                                         rebirthTask.Schedule(TimeSpan.FromSeconds(2),
                                                                              engageTask =>
                                                                              {
                                                                                  _isInEgg = false;
                                                                                  DoCastSelf(SpellIds.FullHeal);
                                                                                  DoCastSelf(SpellIds.Burn);
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
internal class spell_felblood_kaelthas_flame_strike : AuraScript, IHasAuraEffects
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
            target.CastSpell(target, SpellIds.FlameStrikeDamage);
    }
}