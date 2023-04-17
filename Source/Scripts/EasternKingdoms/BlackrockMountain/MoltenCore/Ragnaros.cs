// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore;

internal struct SpellIds
{
    public const uint HAND_OF_RAGNAROS = 19780;
    public const uint WRATH_OF_RAGNAROS = 20566;
    public const uint LAVA_BURST = 21158;
    public const uint MAGMA_BLAST = 20565;       // Ranged attack
    public const uint SONS_OF_FLAME_DUMMY = 21108; // Server side effect
    public const uint RAGSUBMERGE = 21107;      // Stealth aura
    public const uint RAGEMERGE = 20568;
    public const uint MELT_WEAPON = 21388;
    public const uint ELEMENTAL_FIRE = 20564;
    public const uint ERRUPTION = 17731;
}

internal struct TextIds
{
    public const uint SAY_SUMMON_MAJ = 0;
    public const uint SAY_ARRIVAL1_RAG = 1;
    public const uint SAY_ARRIVAL2_MAJ = 2;
    public const uint SAY_ARRIVAL3_RAG = 3;
    public const uint SAY_ARRIVAL5_RAG = 4;
    public const uint SAY_REINFORCEMENTS1 = 5;
    public const uint SAY_REINFORCEMENTS2 = 6;
    public const uint SAY_HAND = 7;
    public const uint SAY_WRATH = 8;
    public const uint SAY_KILL = 9;
    public const uint SAY_MAGMABURST = 10;
}

internal struct EventIds
{
    public const uint ERUPTION = 1;
    public const uint WRATH_OF_RAGNAROS = 2;
    public const uint HAND_OF_RAGNAROS = 3;
    public const uint LAVA_BURST = 4;
    public const uint ELEMENTAL_FIRE = 5;
    public const uint MAGMA_BLAST = 6;
    public const uint SUBMERGE = 7;

    public const uint INTRO1 = 8;
    public const uint INTRO2 = 9;
    public const uint INTRO3 = 10;
    public const uint INTRO4 = 11;
    public const uint INTRO5 = 12;
}

[Script]
internal class BossRagnaros : BossAI
{
    private uint _emergeTimer;
    private bool _hasSubmergedOnce;
    private bool _hasYelledMagmaBurst;
    private byte _introState;
    private bool _isBanished;

    public BossRagnaros(Creature creature) : base(creature, DataTypes.RAGNAROS)
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
        Events.ScheduleEvent(EventIds.ERUPTION, TimeSpan.FromSeconds(15));
        Events.ScheduleEvent(EventIds.WRATH_OF_RAGNAROS, TimeSpan.FromSeconds(30));
        Events.ScheduleEvent(EventIds.HAND_OF_RAGNAROS, TimeSpan.FromSeconds(25));
        Events.ScheduleEvent(EventIds.LAVA_BURST, TimeSpan.FromSeconds(10));
        Events.ScheduleEvent(EventIds.ELEMENTAL_FIRE, TimeSpan.FromSeconds(3));
        Events.ScheduleEvent(EventIds.MAGMA_BLAST, TimeSpan.FromSeconds(2));
        Events.ScheduleEvent(EventIds.SUBMERGE, TimeSpan.FromMinutes(3));
    }

    public override void KilledUnit(Unit victim)
    {
        if (RandomHelper.URand(0, 99) < 25)
            Talk(TextIds.SAY_KILL);
    }

    public override void UpdateAI(uint diff)
    {
        if (_introState != 2)
        {
            if (_introState == 0)
            {
                Me.HandleEmoteCommand(Emote.OneshotEmerge);
                Events.ScheduleEvent(EventIds.INTRO1, TimeSpan.FromSeconds(4));
                Events.ScheduleEvent(EventIds.INTRO2, TimeSpan.FromSeconds(23));
                Events.ScheduleEvent(EventIds.INTRO3, TimeSpan.FromSeconds(42));
                Events.ScheduleEvent(EventIds.INTRO4, TimeSpan.FromSeconds(43));
                Events.ScheduleEvent(EventIds.INTRO5, TimeSpan.FromSeconds(53));
                _introState = 1;
            }

            Events.Update(diff);

            Events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.INTRO1:
                        Talk(TextIds.SAY_ARRIVAL1_RAG);

                        break;
                    case EventIds.INTRO2:
                        Talk(TextIds.SAY_ARRIVAL3_RAG);

                        break;
                    case EventIds.INTRO3:
                        Me.HandleEmoteCommand(Emote.OneshotAttack1h);

                        break;
                    case EventIds.INTRO4:
                        Talk(TextIds.SAY_ARRIVAL5_RAG);
                        var executus = ObjectAccessor.GetCreature(Me, Instance.GetGuidData(DataTypes.MAJORDOMO_EXECUTUS));

                        if (executus)
                            Unit.Kill(Me, executus);

                        break;
                    case EventIds.INTRO5:
                        Me.ReactState = ReactStates.Aggressive;
                        Me.RemoveUnitFlag(UnitFlags.NonAttackable);
                        Me.SetImmuneToPC(false);
                        _introState = 2;

                        break;
                }
            });
        }
        else
        {
            if (_isBanished && ((_emergeTimer <= diff) || (Instance.GetData(McMiscConst.DATA_RAGNAROS_ADDS)) > 8))
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

                Instance.SetData(McMiscConst.DATA_RAGNAROS_ADDS, 0);

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
                    case EventIds.ERUPTION:
                        DoCastVictim(SpellIds.ERRUPTION);
                        Events.ScheduleEvent(EventIds.ERUPTION, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(45));

                        break;
                    case EventIds.WRATH_OF_RAGNAROS:
                        DoCastVictim(SpellIds.WRATH_OF_RAGNAROS);

                        if (RandomHelper.URand(0, 1) != 0)
                            Talk(TextIds.SAY_WRATH);

                        Events.ScheduleEvent(EventIds.WRATH_OF_RAGNAROS, TimeSpan.FromSeconds(25));

                        break;
                    case EventIds.HAND_OF_RAGNAROS:
                        DoCast(Me, SpellIds.HAND_OF_RAGNAROS);

                        if (RandomHelper.URand(0, 1) != 0)
                            Talk(TextIds.SAY_HAND);

                        Events.ScheduleEvent(EventIds.HAND_OF_RAGNAROS, TimeSpan.FromSeconds(20));

                        break;
                    case EventIds.LAVA_BURST:
                        DoCastVictim(SpellIds.LAVA_BURST);
                        Events.ScheduleEvent(EventIds.LAVA_BURST, TimeSpan.FromSeconds(10));

                        break;
                    case EventIds.ELEMENTAL_FIRE:
                        DoCastVictim(SpellIds.ELEMENTAL_FIRE);
                        Events.ScheduleEvent(EventIds.ELEMENTAL_FIRE, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14));

                        break;
                    case EventIds.MAGMA_BLAST:
                        if (!Me.IsWithinMeleeRange(Me.Victim))
                        {
                            DoCastVictim(SpellIds.MAGMA_BLAST);

                            if (!_hasYelledMagmaBurst)
                            {
                                //Say our dialog
                                Talk(TextIds.SAY_MAGMABURST);
                                _hasYelledMagmaBurst = true;
                            }
                        }

                        Events.ScheduleEvent(EventIds.MAGMA_BLAST, TimeSpan.FromMilliseconds(2500));

                        break;
                    case EventIds.SUBMERGE:
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
                            Instance.SetData(McMiscConst.DATA_RAGNAROS_ADDS, 0);

                            if (!_hasSubmergedOnce)
                            {
                                Talk(TextIds.SAY_REINFORCEMENTS1);

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
                                Talk(TextIds.SAY_REINFORCEMENTS2);

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

                        Events.ScheduleEvent(EventIds.SUBMERGE, TimeSpan.FromMinutes(3));

                        break;
                    }
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
internal class NPCSonOfFlame : ScriptedAI //didnt work correctly in Eai for me...
{
    private readonly InstanceScript _instance;

    public NPCSonOfFlame(Creature creature) : base(creature)
    {
        _instance = Me.InstanceScript;
    }

    public override void JustDied(Unit killer)
    {
        _instance.SetData(McMiscConst.DATA_RAGNAROS_ADDS, 1);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();
    }
}