// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Spells;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.AI.ScriptedAI;

public class ScriptedAI : CreatureAI
{
    private readonly Difficulty _difficulty;
    private readonly bool _isHeroic;
    private bool _isCombatMovementAllowed;

    public ScriptedAI(Creature creature) : base(creature)
    {
        _isCombatMovementAllowed = true;
        _isHeroic = Me.Location.Map.IsHeroic;
        _difficulty = Me.Location.Map.DifficultyID;
    }

    //Plays a sound to all nearby players
    public static void DoPlaySoundToSet(WorldObject source, uint soundId)
    {
        if (source == null)
            return;

        if (!source.CliDB.SoundKitStorage.ContainsKey(soundId))
        {
            Log.Logger.Error($"ScriptedAI::DoPlaySoundToSet: Invalid soundId {soundId} used in DoPlaySoundToSet (Source: {source.GUID})");

            return;
        }

        source.PlayDirectSound(soundId);
    }

    public static Creature GetClosestCreatureWithEntry(WorldObject source, uint entry, float maxSearchRange, bool alive = true)
    {
        return source.Location.FindNearestCreature(entry, maxSearchRange, alive);
    }

    public static Creature GetClosestCreatureWithOptions(WorldObject source, float maxSearchRange, FindCreatureOptions options)
    {
        return source.Location.FindNearestCreatureWithOptions(maxSearchRange, options);
    }

    public static GameObject GetClosestGameObjectWithEntry(WorldObject source, uint entry, float maxSearchRange, bool spawnedOnly = true)
    {
        return source.Location.FindNearestGameObject(entry, maxSearchRange, spawnedOnly);
    }

    /// <summary>
    ///     Add specified amount of threat directly to victim (ignores redirection effects) - also puts victim in combat and engages them if necessary
    /// </summary>
    /// <param name="victim"> </param>
    /// <param name="amount"> </param>
    /// <param name="who"> </param>
    public void AddThreat(Unit victim, double amount, Unit who = null)
    {
        if (!victim)
            return;

        who ??= Me;

        who.GetThreatManager().AddThreat(victim, amount, null, true, true);
    }

    // Called before JustEngagedWith even before the creature is in combat.
    public override void AttackStart(Unit target)
    {
        if (IsCombatMovementAllowed())
            base.AttackStart(target);
        else
            AttackStartNoMove(target);
    }

    public void AttackStartNoMove(Unit target)
    {
        if (target == null)
            return;

        if (Me.Attack(target, true))
            DoStartNoMovement(target);
    }

    //Cast spell by spell info
    public void DoCastSpell(Unit target, SpellInfo spellInfo, bool triggered = false)
    {
        if (target == null || Me.IsNonMeleeSpellCast(false))
            return;

        Me.StopMoving();
        Me.SpellFactory.CastSpell(target, spellInfo.Id, triggered);
    }

    //Returns a list of friendly CC'd units within range
    public List<Creature> DoFindFriendlyCc(float range)
    {
        List<Creature> list = new();
        var uCheck = new FriendlyCCedInRange(Me, range);
        var searcher = new CreatureListSearcher(Me, list, uCheck, GridType.All);
        Cell.VisitGrid(Me, searcher, range);

        return list;
    }

    //Returns a list of all friendly units missing a specific buff within range
    public List<Creature> DoFindFriendlyMissingBuff(float range, uint spellId)
    {
        List<Creature> list = new();
        var uCheck = new FriendlyMissingBuffInRange(Me, range, spellId);
        var searcher = new CreatureListSearcher(Me, list, uCheck, GridType.All);
        Cell.VisitGrid(Me, searcher, range);

        return list;
    }

    //Returns friendly unit with the most amount of hp missing from max hp
    public Unit DoSelectLowestHpFriendly(float range, uint minHpDiff = 1)
    {
        var uCheck = new MostHPMissingInRange<Unit>(Me, range, minHpDiff);
        var searcher = new UnitLastSearcher(Me, uCheck, GridType.All);
        Cell.VisitGrid(Me, searcher, range);

        return searcher.GetTarget();
    }

    //Spawns a creature relative to me
    public Creature DoSpawnCreature(uint entry, float offsetX, float offsetY, float offsetZ, float angle, TempSummonType type, TimeSpan despawntime)
    {
        return Me.SummonCreature(entry, new Position(Me.Location.X + offsetX, Me.Location.Y + offsetY, Me.Location.Z + offsetZ, angle), type, despawntime);
    }

    //Start movement toward victim
    public void DoStartMovement(Unit target, float distance = 0.0f, float angle = 0.0f)
    {
        if (target != null)
            Me.MotionMaster.MoveChase(target, distance, angle);
    }

    //Start no movement on victim
    public void DoStartNoMovement(Unit target)
    {
        if (target == null)
            return;

        Me.MotionMaster.MoveIdle();
    }

    //Stop attack of current victim
    public void DoStopAttack()
    {
        if (Me.Victim != null)
            Me.AttackStop();
    }

    public void DoTeleportAll(float x, float y, float z, float o)
    {
        var map = Me.Location.Map;

        if (!map.IsDungeon)
            return;

        var playerList = map.Players;

        foreach (var player in playerList)
            if (player.IsAlive)
                player.TeleportTo(Me.Location.MapId, x, y, z, o, TeleportToOptions.NotLeaveCombat);
    }

    //Teleports a player without dropping threat (only teleports to same map)
    public void DoTeleportPlayer(Unit unit, float x, float y, float z, float o)
    {
        if (unit == null)
            return;

        var player = unit.AsPlayer;

        if (player != null)
            player.TeleportTo(unit.Location.MapId, x, y, z, o, TeleportToOptions.NotLeaveCombat);
        else
            Log.Logger.Error($"ScriptedAI::DoTeleportPlayer: Creature {Me.GUID} Tried to teleport non-player unit ({unit.GUID}) to X: {x} Y: {y} Z: {z} O: {o}. Aborted.");
    }

    public void DoTeleportTo(float x, float y, float z, uint time = 0)
    {
        Me.Location.Relocate(x, y, z);
        var speed = Me.Location.GetDistance(x, y, z) / (time * 0.001f);
        Me.MonsterMoveWithSpeed(x, y, z, speed);
    }

    public void DoTeleportTo(float[] position)
    {
        Me.NearTeleportTo(position[0], position[1], position[2], position[3]);
    }

    public T DungeonMode<T>(T normal5, T heroic10)
    {
        return _difficulty switch
        {
            Difficulty.Normal => normal5,
            _ => heroic10,
        };
    }

    // return the dungeon or raid difficulty
    public Difficulty GetDifficulty()
    {
        return _difficulty;
    }

    //Return a player with at least minimumRange from me
    public Player GetPlayerAtMinimumRange(float minimumRange)
    {
        var check = new PlayerAtMinimumRangeAway(Me, minimumRange);
        var searcher = new PlayerSearcher(Me, check, GridType.World);
        Cell.VisitGrid(Me, searcher, minimumRange);

        return searcher.GetTarget();
    }

    /// <summary>
    ///     Returns the threat level of victim towards who (or me if not specified)
    /// </summary>
    /// <param name="victim"> </param>
    /// <param name="who"> </param>
    /// <returns> </returns>
    public double GetThreat(Unit victim, Unit who = null)
    {
        if (!victim)
            return 0.0f;

        who ??= Me;

        return who.GetThreatManager().GetThreat(victim);
    }

    public bool HealthAbovePct(int pct)
    {
        return Me.HealthAbovePct(pct);
    }

    public bool HealthBelowPct(int pct)
    {
        return Me.HealthBelowPct(pct);
    }

    // return true for 25 man or 25 man heroic mode
    public bool Is25ManRaid()
    {
        return _difficulty == Difficulty.Raid25N || _difficulty == Difficulty.Raid25HC;
    }

    public bool IsCombatMovementAllowed()
    {
        return _isCombatMovementAllowed;
    }

    // return true for heroic mode. i.e.
    //   - for dungeon in mode 10-heroic,
    //   - for raid in mode 10-Heroic
    //   - for raid in mode 25-heroic
    // DO NOT USE to check raid in mode 25-normal.
    public bool IsHeroic()
    {
        return _isHeroic;
    }

    /// <summary>
    ///     Adds/removes the specified percentage from the specified victim's threat (to who, or me if not specified)
    /// </summary>
    /// <param name="victim"> </param>
    /// <param name="pct"> </param>
    /// <param name="who"> </param>
    public void ModifyThreatByPercent(Unit victim, int pct, Unit who = null)
    {
        if (!victim)
            return;

        who ??= Me;

        who.GetThreatManager().ModifyThreatByPercent(victim, pct);
    }

    public T RaidMode<T>(T normal10, T normal25)
    {
        return _difficulty switch
        {
            Difficulty.Raid10N => normal10,
            _ => normal25,
        };
    }

    public T RaidMode<T>(T normal10, T normal25, T heroic10, T heroic25)
    {
        return _difficulty switch
        {
            Difficulty.Raid10N => normal10,
            Difficulty.Raid25N => normal25,
            Difficulty.Raid10HC => heroic10,
            _ => heroic25,
        };
    }

    /// <summary>
    ///     Resets the victim's threat level to who (or me if not specified) to zero
    /// </summary>
    /// <param name="victim"> </param>
    /// <param name="who"> </param>
    public void ResetThreat(Unit victim, Unit who)
    {
        if (!victim)
            return;

        if (!who)
            who = Me;

        who.GetThreatManager().ResetThreat(victim);
    }

    /// <summary>
    ///     Resets the specified unit's threat list (me if not specified) - does not delete entries, just sets their threat to zero
    /// </summary>
    /// <param name="who"> </param>
    public void ResetThreatList(Unit who = null)
    {
        who ??= Me;
        who.GetThreatManager().ResetAllThreat();
    }

    //Returns spells that meet the specified criteria from the creatures spell list
    public SpellInfo SelectSpell(Unit target, SpellSchoolMask school, Mechanics mechanic, SelectTargetType targets, float rangeMin, float rangeMax, SelectEffect effect)
    {
        //No target so we can't cast
        if (target == null)
            return null;

        //Silenced so we can't cast
        if (Me.IsSilenced(school == SpellSchoolMask.None ? SpellSchoolMask.Magic : school))
            return null;

        //Using the extended script system we first create a list of viable spells
        var apSpell = new SpellInfo[SharedConst.MaxCreatureSpells];

        uint spellCount = 0;

        //Check if each spell is viable(set it to null if not)
        for (uint i = 0; i < SharedConst.MaxCreatureSpells; i++)
        {
            var tempSpell = Me.SpellManager.GetSpellInfo(Me.Spells[i], Me.Location.Map.DifficultyID);
            var aiSpell = GetAISpellInfo(Me.Spells[i], Me.Location.Map.DifficultyID);

            //This spell doesn't exist
            if (tempSpell == null || aiSpell == null)
                continue;

            // Targets and Effects checked first as most used restrictions
            //Check the spell targets if specified
            if (targets != 0 && !Convert.ToBoolean(aiSpell.Targets & (1 << ((int)targets - 1))))
                continue;

            //Check the type of spell if we are looking for a specific spell type
            if (effect != 0 && !Convert.ToBoolean(aiSpell.Effects & (1 << ((int)effect - 1))))
                continue;

            //Check for school if specified
            if (school != 0 && (tempSpell.SchoolMask & school) == 0)
                continue;

            //Check for spell mechanic if specified
            if (mechanic != 0 && tempSpell.Mechanic != mechanic)
                continue;

            // Continue if we don't have the mana to actually cast this spell
            var hasPower = true;

            foreach (var cost in tempSpell.CalcPowerCost(Me, tempSpell.GetSchoolMask()))
                if (cost.Amount > Me.GetPower(cost.Power))
                {
                    hasPower = false;

                    break;
                }

            if (!hasPower)
                continue;

            //Check if the spell meets our range requirements
            if (rangeMin != 0 && Me.WorldObjectCombat.GetSpellMinRangeForTarget(target, tempSpell) < rangeMin)
                continue;

            if (rangeMax != 0 && Me.WorldObjectCombat.GetSpellMaxRangeForTarget(target, tempSpell) > rangeMax)
                continue;

            //Check if our target is in range
            if (Me.Location.IsWithinDistInMap(target, Me.WorldObjectCombat.GetSpellMinRangeForTarget(target, tempSpell)) || !Me.Location.IsWithinDistInMap(target, Me.WorldObjectCombat.GetSpellMaxRangeForTarget(target, tempSpell)))
                continue;

            //All good so lets add it to the spell list
            apSpell[spellCount] = tempSpell;
            ++spellCount;
        }

        //We got our usable spells so now lets randomly pick one
        if (spellCount == 0)
            return null;

        return apSpell[RandomHelper.IRand(0, (int)(spellCount - 1))];
    }

    // Used to control if MoveChase() is to be used or not in AttackStart(). Some creatures does not chase victims
    // NOTE: If you use SetCombatMovement while the creature is in combat, it will do NOTHING - This only affects AttackStart
    //       You should make the necessary to make it happen so.
    //       Remember that if you modified _isCombatMovementAllowed (e.g: using SetCombatMovement) it will not be reset at Reset().
    //       It will keep the last value you set.
    public void SetCombatMovement(bool allowMovement)
    {
        _isCombatMovementAllowed = allowMovement;
    }

    public void SetEquipmentSlots(bool loadDefault, int mainHand = -1, int offHand = -1, int ranged = -1)
    {
        if (loadDefault)
        {
            Me.LoadEquipment(Me.OriginalEquipmentId);

            return;
        }

        if (mainHand >= 0)
            Me.SetVirtualItem(0, (uint)mainHand);

        if (offHand >= 0)
            Me.SetVirtualItem(1, (uint)offHand);

        if (ranged >= 0)
            Me.SetVirtualItem(2, (uint)ranged);
    }

    //Called at World update tick
    public override void UpdateAI(uint diff)
    {
        //Check if we have a current target
        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();
    }
}