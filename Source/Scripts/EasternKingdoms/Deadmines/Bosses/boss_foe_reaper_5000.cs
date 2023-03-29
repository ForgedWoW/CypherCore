// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Deadmines.Bosses;

[CreatureScript(43778)]
public class boss_foe_reaper_5000 : BossAI
{
    public const string MONSTER_START = "A stray jolt from the Foe Reaper has distrupted the foundry controls!";
    public const string MONSTER_SLAG = "The monster slag begins to bubble furiously!";
    public static readonly Position PrototypeSpawn = new(-200.499f, -553.946f, 51.2295f, 4.32651f);

    public static readonly Position[] HarvestSpawn =
    {
        new(-229.72f, -590.37f, 19.38f, 0.71f), new(-229.67f, -565.75f, 19.38f, 5.98f), new(-205.53f, -552.74f, 19.38f, 4.53f), new(-182.74f, -565.96f, 19.38f, 3.35f)
    };

    private uint _step;
    private ObjectGuid _prototypeGUID;

    private bool _below;

    public boss_foe_reaper_5000(Creature creature) : base(creature, DMData.DATA_FOEREAPER)
    {
        Me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.ImmuneToPc | UnitFlags.Stunned);
    }

    public override void Reset()
    {
        if (!Me)
            return;

        _Reset();
        Me.ReactState = ReactStates.Passive;
        Me.SetPower(PowerType.Energy, 100);
        Me.SetMaxPower(PowerType.Energy, 100);
        Me.SetPowerType(PowerType.Energy);
        Me.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.ImmuneToPc);
        _step = 0;
        _below = false;

        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);

        Me.SetFullHealth();
        Me.Location.Orientation = 4.273f;

        DespawnOldWatchers();
        RespawnWatchers();

        if (IsHeroic())
        {
            var Reaper = ObjectAccessor.GetCreature(Me, _prototypeGUID);

            if (Reaper != null)
                Reaper.DespawnOrUnsummon();

            Creature prototype = Me.SummonCreature(DMCreatures.NPC_PROTOTYPE_REAPER, PrototypeSpawn, TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));

            if (prototype != null)
            {
                prototype.SetFullHealth();
                _prototypeGUID = prototype.GUID;
            }
        }
    }

    public override void JustEnteredCombat(Unit who)
    {
        base.JustEnteredCombat(who);
        Events.ScheduleEvent(BossEvents.EVENT_REAPER_STRIKE, TimeSpan.FromMilliseconds(10000));
        Events.ScheduleEvent(BossEvents.EVENT_OVERDRIVE, TimeSpan.FromMilliseconds(11000));
        Events.ScheduleEvent(BossEvents.EVENT_HARVEST, TimeSpan.FromMilliseconds(25000));

        if (IsHeroic())
            Events.ScheduleEvent(BossEvents.EVENT_MOLTEN_SLAG, TimeSpan.FromMilliseconds(15000));

        if (!Me)
            return;

        Instance.SendEncounterUnit(EncounterFrameType.Engage, Me);
    }

    public override void JustDied(Unit killer)
    {
        if (!Me)
            return;

        base.JustDied(killer);
        DespawnOldWatchers();
        Talk(eSays.SAY_JUSTDIED);
        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);

        if (IsHeroic())
        {
            var Reaper = ObjectAccessor.GetCreature(Me, _prototypeGUID);

            if (Reaper != null)
                Reaper.DespawnOrUnsummon();
        }
    }

    //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
    //ORIGINAL LINE: uint GetData(uint type) const
    public override uint GetData(uint type)
    {
        if (type == (uint)eAchievementMisc.DATA_ACHIV_PROTOTYPE_PRODIGY)
        {
            if (!IsHeroic())
                return false ? 1 : 0;

            var prototypeReaper = ObjectAccessor.GetCreature(Me, _prototypeGUID);

            if (prototypeReaper != null)
                if (prototypeReaper.Health >= 0.9 * prototypeReaper.MaxHealth)
                    return true ? 1 : 0;
        }

        return false ? 1 : 0;
    }

    public override void JustReachedHome()
    {
        if (!Me)
            return;

        base.JustReachedHome();
        Talk(eSays.SAY_KILLED_UNIT);
        Me.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.ImmuneToPc | UnitFlags.Stunned);
        Instance.SetBossState(DMData.DATA_FOEREAPER, EncounterState.Fail);
    }

    public void DespawnOldWatchers()
    {
        var reapers = Me.GetCreatureListWithEntryInGrid(47403, 250.0f);

        reapers.Sort(new ObjectDistanceOrderPred(Me));

        foreach (var reaper in reapers)
            if (reaper && reaper.TypeId == TypeId.Unit)
                reaper.DespawnOrUnsummon();
    }

    public void RespawnWatchers()
    {
        for (byte i = 0; i < 4; ++i)
            Me.SummonCreature(47403, HarvestSpawn[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
    }

    public void SpellHit(Unit UnnamedParameter, SpellInfo spell)
    {
        if (spell == null || !Me)
            return;

        if (spell.Id == eSpell.ENERGIZE)
        {
            if (_step == 3)
                Events.ScheduleEvent(BossEvents.EVENT_START, TimeSpan.FromMilliseconds(100));

            _step++;
        }
    }

    public void MovementInform(uint UnnamedParameter, uint id)
    {
        if (id == 0)
        {
            var HarvestTarget = Me.FindNearestCreature(DMCreatures.NPC_HARVEST_TARGET, 200.0f, true);

            if (HarvestTarget != null)
            {
                //DoCast(HarvestTarget, IsHeroic() ? HARVEST_SWEEP_H : HARVEST_SWEEP);
                Me.RemoveAura(eSpell.HARVEST_AURA);
                Events.ScheduleEvent(BossEvents.EVENT_START_ATTACK, TimeSpan.FromMilliseconds(1000));
            }
        }
    }

    public void HarvestChase()
    {
        var HarvestTarget = Me.FindNearestCreature(DMCreatures.NPC_HARVEST_TARGET, 200.0f, true);

        if (HarvestTarget != null)
        {
            Me.SetSpeed(UnitMoveType.Run, 3.0f);
            Me.MotionMaster.MoveCharge(HarvestTarget.Location.X, HarvestTarget.Location.Y, HarvestTarget.Location.Z, 5.0f, 0);
            HarvestTarget.DespawnOrUnsummon(TimeSpan.FromMilliseconds(8500));
        }
    }

    public override void UpdateAI(uint uiDiff)
    {
        if (!Me)
            return;

        DoMeleeAttackIfReady();

        Events.Update(uiDiff);

        uint eventId;

        while ((eventId = Events.ExecuteEvent()) != 0)
        {
            switch (eventId)
            {
                case BossEvents.EVENT_START:
                    Talk(eSays.SAY_EVENT_START);
                    Me.AddAura(eSpell.ENERGIZED, Me);
                    Me.TextEmote(MONSTER_START, null, true);
                    Events.ScheduleEvent(BossEvents.EVENT_START_2, TimeSpan.FromMilliseconds(5000));

                    break;

                case BossEvents.EVENT_START_2:
                    Me.TextEmote(MONSTER_SLAG, null, true);
                    Me.SetHealth(Me.MaxHealth);
                    DoZoneInCombat();
                    Me.ReactState = ReactStates.Aggressive;
                    Me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    Me.RemoveUnitFlag(UnitFlags.ImmuneToPc);
                    Me.RemoveUnitFlag(UnitFlags.Stunned);
                    Me.RemoveAura(eSpell.ENERGIZED);
                    Events.ScheduleEvent(BossEvents.EVENT_SRO, TimeSpan.FromMilliseconds(1000));

                    break;

                case BossEvents.EVENT_SRO:
                    Me.RemoveAura(DMSharedSpells.OFFLINE);

                    var victim = Me.FindNearestPlayer(40.0f);

                    if (victim != null)
                        Me.Attack(victim, false);

                    break;

                case BossEvents.EVENT_START_ATTACK:
                    Me.RemoveAura(eSpell.HARVEST_AURA);
                    Me.SetSpeed(UnitMoveType.Run, 2.0f);
                    var victim2 = Me.FindNearestPlayer(40.0f);

                    if (victim2 != null)
                        Me.Attack(victim2, true);

                    break;

                case BossEvents.EVENT_OVERDRIVE:
                    if (!UpdateVictim())
                        return;

                    Me.TextEmote("|TInterface\\Icons\\ability_whirlwind.blp:20|tFoe Reaper 5000 begins to activate |cFFFF0000|Hspell:91716|h[Overdrive]|h|r!", null, true);
                    Me.AddAura(eSpell.OVERDRIVE, Me);
                    Me.SetSpeed(UnitMoveType.Run, 4.0f);
                    Events.ScheduleEvent(BossEvents.EVENT_SWITCH_TARGET, TimeSpan.FromMilliseconds(1500));
                    Events.ScheduleEvent(BossEvents.EVENT_OVERDRIVE, TimeSpan.FromMilliseconds(45000));

                    break;

                case BossEvents.EVENT_SWITCH_TARGET:
                    var victim3 = SelectTarget(SelectTargetMethod.Random, 0, 150, true);

                    if (victim3 != null)
                        Me.Attack(victim3, true);

                    if (Me.HasAura(eSpell.OVERDRIVE))
                        Events.ScheduleEvent(BossEvents.EVENT_SWITCH_TARGET, TimeSpan.FromMilliseconds(1500));

                    break;

                case BossEvents.EVENT_HARVEST:
                    if (!UpdateVictim())
                        return;

                    var target = SelectTarget(SelectTargetMethod.Random, 0, 150, true);

                    if (target != null)
                        Me.CastSpell(target, eSpell.HARVEST);

                    Events.RescheduleEvent(BossEvents.EVENT_HARVEST_SWEAP, TimeSpan.FromMilliseconds(5500));

                    break;

                case BossEvents.EVENT_HARVEST_SWEAP:
                    if (!UpdateVictim())
                        return;

                    HarvestChase();
                    Talk(eSays.SAY_HARVEST_SWEAP);
                    Events.ScheduleEvent(BossEvents.EVENT_START_ATTACK, TimeSpan.FromMilliseconds(8000));
                    Events.RescheduleEvent(BossEvents.EVENT_HARVEST, TimeSpan.FromMilliseconds(45000));

                    break;

                case BossEvents.EVENT_REAPER_STRIKE:
                    if (!UpdateVictim())
                        return;

                    var victim4 = Me.Victim;

                    if (victim4 != null)
                        if (Me.IsWithinDist(victim4, 25.0f))
                            DoCast(victim4, IsHeroic() ? eSpell.REAPER_STRIKE_H : eSpell.REAPER_STRIKE);

                    Events.ScheduleEvent(BossEvents.EVENT_REAPER_STRIKE, TimeSpan.FromMilliseconds(RandomHelper.URand(9000, 12000)));

                    break;

                case BossEvents.EVENT_MOLTEN_SLAG:
                    Me.TextEmote(MONSTER_SLAG, null, true);
                    Me.CastSpell(-213.21f, -576.85f, 20.97f, eSpell.SUMMON_MOLTEN_SLAG, false);
                    Events.ScheduleEvent(BossEvents.EVENT_MOLTEN_SLAG, TimeSpan.FromMilliseconds(20000));

                    break;

                case BossEvents.EVENT_SAFETY_OFFLINE:
                    Talk(eSays.SAY_EVENT_SRO);
                    DoCast(Me, IsHeroic() ? eSpell.SAFETY_REST_OFFLINE_H : eSpell.SAFETY_REST_OFFLINE);

                    break;
            }

            if (HealthBelowPct(30) && !_below)
            {
                Events.ScheduleEvent(BossEvents.EVENT_SAFETY_OFFLINE, TimeSpan.FromMilliseconds(0));
                _below = true;
            }
        }
    }

    public struct eSpell
    {
        public const uint ENERGIZE = 89132;
        public const uint ENERGIZED = 91733; // -> 89200;
        public const uint ON_FIRE = 91737;
        public const uint COSMETIC_STAND = 88906;

        // BOSS spells
        public const uint OVERDRIVE = 88481; // 88484
        public const uint HARVEST = 88495;
        public const uint HARVEST_AURA = 88497;

        public const uint HARVEST_SWEEP = 88521;
        public const uint HARVEST_SWEEP_H = 91718;

        public const uint REAPER_STRIKE = 88490;
        public const uint REAPER_STRIKE_H = 91717;

        public const uint SAFETY_REST_OFFLINE = 88522;
        public const uint SAFETY_REST_OFFLINE_H = 91720;

        public const uint SUMMON_MOLTEN_SLAG = 91839;
    }

    public struct eAchievementMisc
    {
        public const uint ACHIEVEMENT_PROTOTYPE_PRODIGY = 5368;
        public const uint DATA_ACHIV_PROTOTYPE_PRODIGY = 1;
    }

    public struct BossEvents
    {
        public const uint EVENT_NULL = 0;
        public const uint EVENT_START = 1;
        public const uint EVENT_START_2 = 2;
        public const uint EVENT_SRO = 3;
        public const uint EVENT_OVERDRIVE = 4;
        public const uint EVENT_HARVEST = 5;
        public const uint EVENT_HARVEST_SWEAP = 6;
        public const uint EVENT_REAPER_STRIKE = 7;
        public const uint EVENT_SAFETY_OFFLINE = 8;
        public const uint EVENT_SWITCH_TARGET = 9;
        public const uint EVENT_MOLTEN_SLAG = 10;
        public const uint EVENT_START_ATTACK = 11;
    }

    public struct eSays
    {
        public const uint SAY_CAST_OVERDRIVE = 0;
        public const uint SAY_JUSTDIED = 1;
        public const uint SAY_KILLED_UNIT = 2;
        public const uint SAY_EVENT_START = 3;

        public const uint SAY_HARVEST_SWEAP = 4;
        public const uint SAY_CAST_OVERDRIVE_E = 5;
        public const uint SAY_EVENT_SRO = 6;
    }
}