// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore;

internal struct SpellIds
{
    public const uint HandOfRagnaros = 19780;
    public const uint WrathOfRagnaros = 20566;
    public const uint LavaBurst = 21158;
    public const uint MagmaBlast = 20565;       // Ranged attack
    public const uint SonsOfFlameDummy = 21108; // Server side effect
    public const uint Ragsubmerge = 21107;      // Stealth aura
    public const uint Ragemerge = 20568;
    public const uint MeltWeapon = 21388;
    public const uint ElementalFire = 20564;
    public const uint Erruption = 17731;
}

internal struct TextIds
{
    public const uint SaySummonMaj = 0;
    public const uint SayArrival1Rag = 1;
    public const uint SayArrival2Maj = 2;
    public const uint SayArrival3Rag = 3;
    public const uint SayArrival5Rag = 4;
    public const uint SayReinforcements1 = 5;
    public const uint SayReinforcements2 = 6;
    public const uint SayHand = 7;
    public const uint SayWrath = 8;
    public const uint SayKill = 9;
    public const uint SayMagmaburst = 10;
}

internal struct EventIds
{
    public const uint Eruption = 1;
    public const uint WrathOfRagnaros = 2;
    public const uint HandOfRagnaros = 3;
    public const uint LavaBurst = 4;
    public const uint ElementalFire = 5;
    public const uint MagmaBlast = 6;
    public const uint Submerge = 7;

    public const uint Intro1 = 8;
    public const uint Intro2 = 9;
    public const uint Intro3 = 10;
    public const uint Intro4 = 11;
    public const uint Intro5 = 12;
}

[Script]
internal class boss_ragnaros : BossAI
{
    private uint _emergeTimer;
    private bool _hasSubmergedOnce;
    private bool _hasYelledMagmaBurst;
    private byte _introState;
    private bool _isBanished;

    public boss_ragnaros(Creature creature) : base(creature, DataTypes.Ragnaros)
    {
        Initialize();
        _introState = 0;
        Me.ReactState = ReactStates.Passive;
        Me.SetUnitFlag(UnitFlags.NonAttackable);
        SetCombatMovement(false);
    }

    public override void Reset()
    {
        base.Reset();
        Initialize();
        Me.EmoteState = Emote.OneshotNone;
    }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);
        Events.ScheduleEvent(EventIds.Eruption, TimeSpan.FromSeconds(15));
        Events.ScheduleEvent(EventIds.WrathOfRagnaros, TimeSpan.FromSeconds(30));
        Events.ScheduleEvent(EventIds.HandOfRagnaros, TimeSpan.FromSeconds(25));
        Events.ScheduleEvent(EventIds.LavaBurst, TimeSpan.FromSeconds(10));
        Events.ScheduleEvent(EventIds.ElementalFire, TimeSpan.FromSeconds(3));
        Events.ScheduleEvent(EventIds.MagmaBlast, TimeSpan.FromSeconds(2));
        Events.ScheduleEvent(EventIds.Submerge, TimeSpan.FromMinutes(3));
    }

    public override void KilledUnit(Unit victim)
    {
        if (RandomHelper.URand(0, 99) < 25)
            Talk(TextIds.SayKill);
    }

    public override void UpdateAI(uint diff)
    {
        if (_introState != 2)
        {
            if (_introState == 0)
            {
                Me.HandleEmoteCommand(Emote.OneshotEmerge);
                Events.ScheduleEvent(EventIds.Intro1, TimeSpan.FromSeconds(4));
                Events.ScheduleEvent(EventIds.Intro2, TimeSpan.FromSeconds(23));
                Events.ScheduleEvent(EventIds.Intro3, TimeSpan.FromSeconds(42));
                Events.ScheduleEvent(EventIds.Intro4, TimeSpan.FromSeconds(43));
                Events.ScheduleEvent(EventIds.Intro5, TimeSpan.FromSeconds(53));
                _introState = 1;
            }

            Events.Update(diff);

            Events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.Intro1:
                        Talk(TextIds.SayArrival1Rag);

                        break;
                    case EventIds.Intro2:
                        Talk(TextIds.SayArrival3Rag);

                        break;
                    case EventIds.Intro3:
                        Me.HandleEmoteCommand(Emote.OneshotAttack1h);

                        break;
                    case EventIds.Intro4:
                        Talk(TextIds.SayArrival5Rag);
                        var executus = ObjectAccessor.GetCreature(Me, Instance.GetGuidData(DataTypes.MajordomoExecutus));

                        if (executus)
                            Unit.Kill(Me, executus);

                        break;
                    case EventIds.Intro5:
                        Me.ReactState = ReactStates.Aggressive;
                        Me.RemoveUnitFlag(UnitFlags.NonAttackable);
                        Me.SetImmuneToPC(false);
                        _introState = 2;

                        break;
                    default:
                        break;
                }
            });
        }
        else
        {
            if (_isBanished && ((_emergeTimer <= diff) || (Instance.GetData(MCMiscConst.DataRagnarosAdds)) > 8))
            {
                //Become unbanished again
                Me. //Become unbanished again
                    ReactState = ReactStates.Aggressive;

                Me.Faction = (uint)FactionTemplates.Monster;
                Me.RemoveUnitFlag(UnitFlags.Uninteractible);
                Me.EmoteState = Emote.OneshotNone;
                Me.HandleEmoteCommand(Emote.OneshotEmerge);
                var target = SelectTarget(SelectTargetMethod.Random, 0);

                if (target)
                    AttackStart(target);

                Instance.SetData(MCMiscConst.DataRagnarosAdds, 0);

                //DoCast(me, SpellRagemerge); //"phase spells" didnt worked correctly so Ive commented them and wrote solution witch doesnt need core support
                _isBanished = false;
            }
            else if (_isBanished)
            {
                _emergeTimer -= diff;

                //Do nothing while banished
                return;
            }

            //Return since we have no Target
            if (!UpdateVictim())
                return;

            Events.Update(diff);

            Events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.Eruption:
                        DoCastVictim(SpellIds.Erruption);
                        Events.ScheduleEvent(EventIds.Eruption, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(45));

                        break;
                    case EventIds.WrathOfRagnaros:
                        DoCastVictim(SpellIds.WrathOfRagnaros);

                        if (RandomHelper.URand(0, 1) != 0)
                            Talk(TextIds.SayWrath);

                        Events.ScheduleEvent(EventIds.WrathOfRagnaros, TimeSpan.FromSeconds(25));

                        break;
                    case EventIds.HandOfRagnaros:
                        DoCast(Me, SpellIds.HandOfRagnaros);

                        if (RandomHelper.URand(0, 1) != 0)
                            Talk(TextIds.SayHand);

                        Events.ScheduleEvent(EventIds.HandOfRagnaros, TimeSpan.FromSeconds(20));

                        break;
                    case EventIds.LavaBurst:
                        DoCastVictim(SpellIds.LavaBurst);
                        Events.ScheduleEvent(EventIds.LavaBurst, TimeSpan.FromSeconds(10));

                        break;
                    case EventIds.ElementalFire:
                        DoCastVictim(SpellIds.ElementalFire);
                        Events.ScheduleEvent(EventIds.ElementalFire, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14));

                        break;
                    case EventIds.MagmaBlast:
                        if (!Me.IsWithinMeleeRange(Me.Victim))
                        {
                            DoCastVictim(SpellIds.MagmaBlast);

                            if (!_hasYelledMagmaBurst)
                            {
                                //Say our dialog
                                Talk(TextIds.SayMagmaburst);
                                _hasYelledMagmaBurst = true;
                            }
                        }

                        Events.ScheduleEvent(EventIds.MagmaBlast, TimeSpan.FromMilliseconds(2500));

                        break;
                    case EventIds.Submerge:
                    {
                        if (!_isBanished)
                        {
                            //Creature spawning and ragnaros becomming unattackable
                            //is not very well supported in the core //no it really isnt
                            //so added normaly spawning and banish workaround and attack again after 90 secs.
                            Me.AttackStop();
                            ResetThreatList();
                            Me.ReactState = ReactStates.Passive;
                            Me.InterruptNonMeleeSpells(false);

                            //Root self
                            //DoCast(me, 23973);
                            Me. //Root self
                                //DoCast(me, 23973);
                                Faction = (uint)FactionTemplates.Friendly;

                            Me.SetUnitFlag(UnitFlags.Uninteractible);
                            Me.EmoteState = Emote.StateSubmerged;
                            Me.HandleEmoteCommand(Emote.OneshotSubmerge);
                            Instance.SetData(MCMiscConst.DataRagnarosAdds, 0);

                            if (!_hasSubmergedOnce)
                            {
                                Talk(TextIds.SayReinforcements1);

                                // summon 8 elementals
                                for (byte i = 0; i < 8; ++i)
                                {
                                    var target = SelectTarget(SelectTargetMethod.Random, 0);

                                    if (target != null)
                                    {
                                        Creature summoned = Me.SummonCreature(12143, target.Location.X, target.Location.Y, target.Location.Z, 0.0f, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromMinutes(15));

                                        summoned?.AI.AttackStart(target);
                                    }
                                }

                                _hasSubmergedOnce = true;
                                _isBanished = true;
                                //DoCast(me, SpellRagsubmerge);
                                _emergeTimer = 90000;
                            }
                            else
                            {
                                Talk(TextIds.SayReinforcements2);

                                for (byte i = 0; i < 8; ++i)
                                {
                                    var target = SelectTarget(SelectTargetMethod.Random, 0);

                                    if (target != null)
                                    {
                                        Creature summoned = Me.SummonCreature(12143, target.Location.X, target.Location.Y, target.Location.Z, 0.0f, TempSummonType.TimedOrCorpseDespawn, TimeSpan.FromMinutes(15));

                                        summoned?.AI.AttackStart(target);
                                    }
                                }

                                _isBanished = true;
                                //DoCast(me, SpellRagsubmerge);
                                _emergeTimer = 90000;
                            }
                        }

                        Events.ScheduleEvent(EventIds.Submerge, TimeSpan.FromMinutes(3));

                        break;
                    }
                    default:
                        break;
                }
            });


            DoMeleeAttackIfReady();
        }
    }

    private void Initialize()
    {
        _emergeTimer = 90000;
        _hasYelledMagmaBurst = false;
        _hasSubmergedOnce = false;
        _isBanished = false;
    }
}

[Script]
internal class npc_son_of_flame : ScriptedAI //didnt work correctly in Eai for me...
{
    private readonly InstanceScript instance;

    public npc_son_of_flame(Creature creature) : base(creature)
    {
        instance = Me.InstanceScript;
    }

    public override void JustDied(Unit killer)
    {
        instance.SetData(MCMiscConst.DataRagnarosAdds, 1);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();
    }
}