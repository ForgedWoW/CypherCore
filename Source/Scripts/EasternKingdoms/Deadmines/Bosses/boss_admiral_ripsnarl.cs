// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.Bosses;

[CreatureScript(47626)]
public class boss_admiral_ripsnarl : BossAI
{
    public static readonly Position CookieSpawn = new(-88.1319f, -819.33f, 39.23453f, 0.0f);

    public static readonly Position[] VaporFinalSpawn =
    {
        new(-70.59f, -820.57f, 40.56f, 6.28f), new(-55.73f, -815.84f, 41.97f, 3.85f), new(-55.73f, -825.54f, 41.99f, 2.60f)
    };


    private byte _vaporCount;
    private uint _phase;
    private uint _numberCastCoalesce;

    private bool _below_10;
    private bool _below_25;
    private bool _below_50;
    private bool _below_75;

    public boss_admiral_ripsnarl(Creature creature) : base(creature, DMData.DATA_RIPSNARL) { }

    public override void Reset()
    {
        if (!Me)
            return;

        _Reset();
        Summons.DespawnAll();
        Events.Reset();
        _vaporCount = 0;
        Me.SetFullHealth();
        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
        RemoveAuraFromMap();
        SetFog(false);

        _below_10 = false;
        _below_25 = false;
        _below_50 = false;
        _below_75 = false;
        _numberCastCoalesce = 0;
        _phase = AdmiralPhases.PHASE_NORMAL;
    }

    public override void JustEnteredCombat(Unit who)
    {
        if (!Me)
            return;

        base.JustEnteredCombat(who);
        Talk(Says.SAY_AGGRO);
        Instance.SendEncounterUnit(EncounterFrameType.Engage, Me);

        Events.ScheduleEvent(BossEvents.EVENT_THIRST_FOR_BLOOD, TimeSpan.FromMilliseconds(0));
        Events.ScheduleEvent(BossEvents.EVENT_SWIPE, TimeSpan.FromMilliseconds(10000));

        if (IsHeroic())
            Events.ScheduleEvent(BossEvents.EVENT_GO_FOR_THROAT, TimeSpan.FromMilliseconds(10000));
    }

    public override void JustSummoned(Creature summoned)
    {
        if (summoned.AI != null)
            summoned.AI.AttackStart(SelectTarget(SelectTargetMethod.Random));

        Summons.Summon(summoned);
    }

    public override void JustReachedHome()
    {
        if (!Me)
            return;

        base.JustReachedHome();
        Talk(Says.SAY_KILL);
        RemoveAuraFromMap();
    }

    public override void SummonedCreatureDespawn(Creature summon)
    {
        Summons.Despawn(summon);
    }

    public override void JustDied(Unit killer)
    {
        if (!Me)
            return;

        base.JustDied(killer);
        Summons.DespawnAll();
        Talk(Says.SAY_DEATH);
        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
        RemoveAuraFromMap();
        RemoveFog();
        Me.SummonCreature(DMCreatures.NPC_CAPTAIN_COOKIE, CookieSpawn);
    }

    public override void SetData(uint uiI, uint uiValue)
    {
        if (uiValue == eAchievementMisc.VAPOR_CASTED_COALESCE && _numberCastCoalesce < 3)
        {
            _numberCastCoalesce++;

            if (_numberCastCoalesce >= 3)
            {
                var map = Me.Map;
                var its_frost_damage = Global.AchievementMgr.GetAchievementByReferencedId(eAchievementMisc.ACHIEVEMENT_ITS_FROST_DAMAGE).FirstOrDefault();

                if (map != null && map.IsDungeon && map.DifficultyID == Difficulty.Heroic)
                {
                    var players = map.Players;

                    if (!players.Empty())
                        foreach (var player in map.Players)
                            if (player != null)
                                if (player.GetDistance(Me) < 300.0f)
                                    player.CompletedAchievement(its_frost_damage);
                }
            }
        }
    }

    public void VaporsKilled()
    {
        _vaporCount++;

        if (_vaporCount == 4)
            Events.ScheduleEvent(BossEvents.EVENT_SHOW_UP, TimeSpan.FromMilliseconds(1000));
    }

    public void SetFog(bool apply)
    {
        if (!Me)
            return;

        _phase = AdmiralPhases.PHASE_FOG;

        return;
    }

    public void RemoveFog()
    {
        _phase = AdmiralPhases.PHASE_NORMAL;
        var players = new List<Unit>();

        var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
        var searcher = new PlayerListSearcher(Me, players, checker);
        Cell.VisitGrid(Me, searcher, 150f);

        foreach (var item in players)
            item.RemoveAura(eSpells.FOG_AURA);
    }

    public void RemoveAuraFromMap()
    {
        if (!Me)
            return;

        SetFog(false);
    }

    public void SummonFinalVapors()
    {
        for (byte i = 0; i < 3; ++i)
            Me.SummonCreature(DMCreatures.NPC_VAPOR, VaporFinalSpawn[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
    }

    public override void UpdateAI(uint uiDiff)
    {
        if (!Me || Instance != null)
            return;

        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();

        Events.Update(uiDiff);

        if (Me.HealthPct < 75 && !_below_75)
        {
            Talk(Says.SAY_FOG_1);

            SetFog(true);
            Events.ScheduleEvent(BossEvents.EVENT_PHASE_TWO, TimeSpan.FromMilliseconds(1000));
            Events.ScheduleEvent(BossEvents.EVENT_UPDATE_FOG, TimeSpan.FromMilliseconds(100));
            _below_75 = true;
        }
        else if (Me.HealthPct < 50 && !_below_50)
        {
            Talk(Says.SAY_FOG_1);
            Events.ScheduleEvent(BossEvents.EVENT_PHASE_TWO, TimeSpan.FromMilliseconds(500));
            _below_50 = true;
        }
        else if (Me.HealthPct < 25 && !_below_25)
        {
            Talk(Says.SAY_FOG_1);
            Events.ScheduleEvent(BossEvents.EVENT_PHASE_TWO, TimeSpan.FromMilliseconds(500));
            _below_25 = true;
        }
        else if (Me.HealthPct < 10 && !_below_10)
        {
            if (IsHeroic())
            {
                SummonFinalVapors();
                _below_10 = true;
            }
        }

        uint eventId;

        while ((eventId = Events.ExecuteEvent()) != 0)
        {
            switch (eventId)
            {
                case BossEvents.EVENT_SWIPE:
                    var victim = Me.Victim;

                    if (victim != null)
                        Me.CastSpell(victim, IsHeroic() ? eSpells.SWIPE_H : eSpells.SWIPE);

                    Events.ScheduleEvent(BossEvents.EVENT_SWIPE, TimeSpan.FromMilliseconds(3000));

                    break;

                case BossEvents.EVENT_UPDATE_FOG:
                    Instance.DoCastSpellOnPlayers(eSpells.FOG_AURA);

                    break;

                case BossEvents.EVENT_GO_FOR_THROAT:
                    var target = SelectTarget(SelectTargetMethod.Random, 1, 100, true);

                    if (target != null)
                        DoCast(target, eSpells.GO_FOR_THE_THROAT);

                    Events.ScheduleEvent(BossEvents.EVENT_GO_FOR_THROAT, TimeSpan.FromMilliseconds(10000));

                    break;

                case BossEvents.EVENT_THIRST_FOR_BLOOD:
                    DoCast(Me, eSpells.THIRST_FOR_BLOOD);

                    break;

                case BossEvents.EVENT_PHASE_TWO:
                    Events.CancelEvent(BossEvents.EVENT_GO_FOR_THROAT);
                    Events.CancelEvent(BossEvents.EVENT_SWIPE);
                    Me.RemoveAura(eSpells.THIRST_FOR_BLOOD);
                    Me.SetVisible(false);
                    Events.ScheduleEvent(BossEvents.EVENT_FLEE_TO_FROG, TimeSpan.FromMilliseconds(100));

                    if (_vaporCount > 0)
                    {
                        Talk(Says.SAY_FOG_2);
                    }
                    else
                    {
                        var victim2 = Me.Victim;

                        if (victim2 != null)
                        {
                            Talk(Says.SAY_1);
                            Me.CastSpell(victim2, eSpells.GO_FOR_THE_THROAT);
                        }
                    }

                    break;

                case BossEvents.EVENT_FLEE_TO_FROG:
                    Me.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible | UnitFlags.Pacified);
                    Me.DoFleeToGetAssistance();
                    Talk(Says.SAY_AUUUU);
                    Events.RescheduleEvent(BossEvents.EVENT_SUMMON_VAPOR, TimeSpan.FromMilliseconds(1000));
                    Events.ScheduleEvent(BossEvents.EVENT_SHOW_UP, TimeSpan.FromMilliseconds(25000));

                    break;

                case BossEvents.EVENT_SHOW_UP:
                    Me.SetVisible(true);
                    _vaporCount = 0;
                    Me.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible | UnitFlags.Pacified);
                    Events.ScheduleEvent(BossEvents.EVENT_SWIPE, TimeSpan.FromMilliseconds(1000));
                    Events.ScheduleEvent(BossEvents.EVENT_GO_FOR_THROAT, TimeSpan.FromMilliseconds(3000));
                    Events.ScheduleEvent(BossEvents.EVENT_THIRST_FOR_BLOOD, TimeSpan.FromMilliseconds(0));

                    break;

                case BossEvents.EVENT_SUMMON_VAPOR:
                    if (_phase == AdmiralPhases.PHASE_FOG)
                    {
                        var target1 = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                        if (target1 != null)
                            Me.CastSpell(target1, eSpells.SUMMON_VAPOR);
                    }

                    Events.RescheduleEvent(BossEvents.EVENT_SUMMON_VAPOR, TimeSpan.FromMilliseconds(3500));

                    break;
            }

            eventId = Events.ExecuteEvent();
        }
    }

    public struct eSpells
    {
        public const uint GO_FOR_THE_THROAT = 88836;
        public const uint GO_FOR_THE_THROAT_H = 91863;
        public const uint SWIPE = 88839;
        public const uint SWIPE_H = 91859;
        public const uint THIRST_FOR_BLOOD = 88736;
        public const uint STEAM_AURA = 95503;
        public const uint FOG_AURA = 89247;
        public const uint BUNNY_AURA = 88755;
        public const uint FOG = 88768;
        public const uint SUMMON_VAPOR = 88831;
        public const uint CONDENSE = 92016;
        public const uint CONDENSE_2 = 92020;
        public const uint CONDENSE_3 = 92029;
        public const uint CONDENSATION = 92013;
        public const uint FREEZING_VAPOR = 92011;
        public const uint COALESCE = 92042;
        public const uint SWIRLING_VAPOR = 92007;
        public const uint CONDENSING_VAPOR = 92008;
    }

    public struct eAchievementMisc
    {
        public const uint ACHIEVEMENT_ITS_FROST_DAMAGE = 5369;
        public const uint VAPOR_CASTED_COALESCE = 1;
    }

    public struct AdmiralPhases
    {
        public const uint PHASE_NORMAL = 1;
        public const uint PHASE_FOG = 2;
    }

    public struct BossEvents
    {
        public const uint EVENT_NULL = 0;
        public const uint EVENT_SWIPE = 1;
        public const uint EVENT_FLEE_TO_FROG = 2;
        public const uint EVENT_SUMMON_VAPOR = 3;
        public const uint EVENT_PHASE_TWO = 4;
        public const uint EVENT_UPDATE_FOG = 5;
        public const uint EVENT_GO_FOR_THROAT = 6;
        public const uint EVENT_THIRST_FOR_BLOOD = 7;
        public const uint EVENT_SHOW_UP = 8;
    }

    public struct Says
    {
        public const uint SAY_DEATH = 0;
        public const uint SAY_KILL = 1;
        public const uint SAY_FOG_1 = 2;
        public const uint SAY_FOG_2 = 3;
        public const uint SAY_1 = 4;
        public const uint SAY_2 = 5;
        public const uint SAY_AUUUU = 6;
        public const uint SAY_AGGRO = 7;
    }
}