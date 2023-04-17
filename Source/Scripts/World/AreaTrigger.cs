// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.World.Areatriggers;

internal struct TextIds
{
    //Brewfest
    public const uint SAY_WELCOME = 4;
}

internal struct SpellIds
{
    //Legion Teleporter
    public const uint TELE_A_TO = 37387;
    public const uint TELE_H_TO = 37389;

    //Sholazar Waygate
    public const uint SHOLAZAR_TO_UNGORO_TELEPORT = 52056;
    public const uint UNGORO_TO_SHOLAZAR_TELEPORT = 52057;

    //Nats Landing
    public const uint FISH_PASTE = 42644;

    //Area 52
    public const uint A52_NEURALYZER = 34400;

    //Stormwind teleport
    public const uint DUST_IN_THE_STORMWIND = 312593;
}

internal struct QuestIds
{
    //Legion Teleporter
    public const uint GAINING_ACCESS_A = 10589;
    public const uint GAINING_ACCESS_H = 10604;

    //Scent Larkorwi
    public const uint SCENT_OF_LARKORWI = 4291;

    //Last Rites
    public const uint LAST_RITES = 12019;
    public const uint BREAKING_THROUGH = 11898;

    //Sholazar Waygate
    public const uint THE_MAKERS_OVERLOOK = 12613;
    public const uint THE_MAKERS_PERCH = 12559;
    public const uint MEETING_A_GREAT_ONE = 13956;

    //Nats Landing
    public const uint NATS_BARGAIN = 11209;

    //Frostgrips Hollow
    public const uint THE_LONESOME_WATCHER = 12877;
}

internal struct CreatureIds
{
    //Scent Larkorwi
    public const uint LARKORWI_MATE = 9683;

    //Nats Landing
    public const uint LURKING_SHARK = 23928;

    //Brewfest
    public const uint TAPPER_SWINDLEKEG = 24711;
    public const uint IPFELKOFER_IRONKEG = 24710;

    //Area 52
    public const uint SPOTLIGHT = 19913;

    //Frostgrips Hollow
    public const uint STORMFORGED_MONITOR = 29862;
    public const uint STORMFORGED_ERADICTOR = 29861;

    //Stormwind Teleport
    public const uint KILL_CREDIT_TELEPORT_STORMWIND = 160561;
}

internal struct GameObjectIds
{
    //Coilfang Waterfall
    public const uint COILFANG_WATERFALL = 184212;
}

internal struct AreaTriggerIds
{
    //Sholazar Waygate
    public const uint SHOLAZAR = 5046;
    public const uint UNGORO = 5047;

    //Brewfest
    public const uint BREWFEST_DUROTAR = 4829;
    public const uint BREWFEST_DUN_MOROGH = 4820;

    //Area 52
    public const uint AREA52_SOUTH = 4472;
    public const uint AREA52_NORTH = 4466;
    public const uint AREA52_WEST = 4471;
    public const uint AREA52_EAST = 4422;
}

internal struct Misc
{
    //Brewfest
    public const uint AREATRIGGER_TALK_COOLDOWN = 5; // In Seconds

    //Area 52
    public const uint SUMMON_COOLDOWN = 5;

    //Frostgrips Hollow
    public const uint TYPE_WAYPOINT = 0;
    public const uint DATA_START = 0;

    public static Position StormforgedMonitorPosition = new(6963.95f, 45.65f, 818.71f, 4.948f);
    public static Position StormforgedEradictorPosition = new(6983.18f, 7.15f, 806.33f, 2.228f);
}

[Script]
internal class AreaTriggerAtCoilfangWaterfall : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    public AreaTriggerAtCoilfangWaterfall() : base("at_coilfang_waterfall") { }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        var go = player.FindNearestGameObject(GameObjectIds.COILFANG_WATERFALL, 35.0f);

        if (go)
            if (go.LootState == LootState.Ready)
                go.UseDoorOrButton();

        return false;
    }
}

[Script]
internal class AreaTriggerAtLegionTeleporter : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    public AreaTriggerAtLegionTeleporter() : base("at_legion_teleporter") { }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        if (player.IsAlive &&
            !player.IsInCombat)
        {
            if (player.Team == TeamFaction.Alliance &&
                player.GetQuestRewardStatus(QuestIds.GAINING_ACCESS_A))
            {
                player.SpellFactory.CastSpell(player, SpellIds.TELE_A_TO, false);

                return true;
            }

            if (player.Team == TeamFaction.Horde &&
                player.GetQuestRewardStatus(QuestIds.GAINING_ACCESS_H))
            {
                player.SpellFactory.CastSpell(player, SpellIds.TELE_H_TO, false);

                return true;
            }

            return false;
        }

        return false;
    }
}

[Script]
internal class AreaTriggerAtScentLarkorwi : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    public AreaTriggerAtScentLarkorwi() : base("at_scent_larkorwi") { }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        if (!player.IsDead &&
            player.GetQuestStatus(QuestIds.SCENT_OF_LARKORWI) == QuestStatus.Incomplete)
            if (!player.FindNearestCreature(CreatureIds.LARKORWI_MATE, 15))
                player.SummonCreature(CreatureIds.LARKORWI_MATE, new Position(player.Location.X + 5, player.Location.Y, player.Location.Z, 3.3f), TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(100));

        return false;
    }
}

[Script]
internal class AreaTriggerAtSholazarWaygate : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    public AreaTriggerAtSholazarWaygate() : base("at_sholazar_waygate") { }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        if (!player.IsDead &&
            (player.GetQuestStatus(QuestIds.MEETING_A_GREAT_ONE) != QuestStatus.None ||
             (player.GetQuestStatus(QuestIds.THE_MAKERS_OVERLOOK) == QuestStatus.Rewarded && player.GetQuestStatus(QuestIds.THE_MAKERS_PERCH) == QuestStatus.Rewarded)))
            switch (areaTrigger.Id)
            {
                case AreaTriggerIds.SHOLAZAR:
                    player.SpellFactory.CastSpell(player, SpellIds.SHOLAZAR_TO_UNGORO_TELEPORT, true);

                    break;

                case AreaTriggerIds.UNGORO:
                    player.SpellFactory.CastSpell(player, SpellIds.UNGORO_TO_SHOLAZAR_TELEPORT, true);

                    break;
            }

        return false;
    }
}

[Script]
internal class AreaTriggerAtNatsLanding : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    public AreaTriggerAtNatsLanding() : base("at_nats_landing") { }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        if (!player.IsAlive ||
            !player.HasAura(SpellIds.FISH_PASTE))
            return false;

        if (player.GetQuestStatus(QuestIds.NATS_BARGAIN) == QuestStatus.Incomplete)
            if (!player.FindNearestCreature(CreatureIds.LURKING_SHARK, 20.0f))
            {
                Creature shark = player.SummonCreature(CreatureIds.LURKING_SHARK, -4246.243f, -3922.356f, -7.488f, 5.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(100));

                if (shark)
                    shark.AI.AttackStart(player);

                return false;
            }

        return true;
    }
}

[Script]
internal class AreaTriggerAtBrewfest : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    private readonly Dictionary<uint, long> _triggerTimes;

    public AreaTriggerAtBrewfest() : base("at_brewfest")
    {
        // Initialize for cooldown
        _triggerTimes = new Dictionary<uint, long>()
        {
            {
                AreaTriggerIds.BREWFEST_DUROTAR, 0
            },
            {
                AreaTriggerIds.BREWFEST_DUN_MOROGH, 0
            }
        };
    }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        var triggerId = areaTrigger.Id;

        // Second trigger happened too early after first, skip for now
        if (GameTime.GetGameTime() - _triggerTimes[triggerId] < Misc.AREATRIGGER_TALK_COOLDOWN)
            return false;

        switch (triggerId)
        {
            case AreaTriggerIds.BREWFEST_DUROTAR:
                var tapper = player.FindNearestCreature(CreatureIds.TAPPER_SWINDLEKEG, 20.0f);

                if (tapper)
                    tapper.AI.Talk(TextIds.SAY_WELCOME, player);

                break;
            case AreaTriggerIds.BREWFEST_DUN_MOROGH:
                var ipfelkofer = player.FindNearestCreature(CreatureIds.IPFELKOFER_IRONKEG, 20.0f);

                if (ipfelkofer)
                    ipfelkofer.AI.Talk(TextIds.SAY_WELCOME, player);

                break;
        }

        _triggerTimes[triggerId] = GameTime.GetGameTime();

        return false;
    }
}

[Script]
internal class AreaTriggerAtArea52Entrance : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    private readonly Dictionary<uint, long> _triggerTimes;

    public AreaTriggerAtArea52Entrance() : base("at_area_52_entrance")
    {
        _triggerTimes = new Dictionary<uint, long>()
        {
            {
                AreaTriggerIds.AREA52_SOUTH, 0
            },
            {
                AreaTriggerIds.AREA52_NORTH, 0
            },
            {
                AreaTriggerIds.AREA52_WEST, 0
            },
            {
                AreaTriggerIds.AREA52_EAST, 0
            }
        };
    }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        float x = 0.0f, y = 0.0f, z = 0.0f;

        if (!player.IsAlive)
            return false;

        if (GameTime.GetGameTime() - _triggerTimes[areaTrigger.Id] < Misc.SUMMON_COOLDOWN)
            return false;

        switch (areaTrigger.Id)
        {
            case AreaTriggerIds.AREA52_EAST:
                x = 3044.176f;
                y = 3610.692f;
                z = 143.61f;

                break;
            case AreaTriggerIds.AREA52_NORTH:
                x = 3114.87f;
                y = 3687.619f;
                z = 143.62f;

                break;
            case AreaTriggerIds.AREA52_WEST:
                x = 3017.79f;
                y = 3746.806f;
                z = 144.27f;

                break;
            case AreaTriggerIds.AREA52_SOUTH:
                x = 2950.63f;
                y = 3719.905f;
                z = 143.33f;

                break;
        }

        player.SummonCreature(CreatureIds.SPOTLIGHT, x, y, z, 0.0f, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(5));
        player.AddAura(SpellIds.A52_NEURALYZER, player);
        _triggerTimes[areaTrigger.Id] = GameTime.GetGameTime();

        return false;
    }
}

[Script]
internal class AreaTriggerAtFrostgripsHollow : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    private ObjectGuid _stormforgedEradictorGUID;
    private ObjectGuid _stormforgedMonitorGUID;

    public AreaTriggerAtFrostgripsHollow() : base("at_frostgrips_hollow")
    {
        _stormforgedMonitorGUID.Clear();
        _stormforgedEradictorGUID.Clear();
    }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        if (player.GetQuestStatus(QuestIds.THE_LONESOME_WATCHER) != QuestStatus.Incomplete)
            return false;

        var stormforgedMonitor = ObjectAccessor.GetCreature(player, _stormforgedMonitorGUID);

        if (stormforgedMonitor)
            return false;

        var stormforgedEradictor = ObjectAccessor.GetCreature(player, _stormforgedEradictorGUID);

        if (stormforgedEradictor)
            return false;

        stormforgedMonitor = player.SummonCreature(CreatureIds.STORMFORGED_MONITOR, Misc.StormforgedMonitorPosition, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(60));

        if (stormforgedMonitor)
        {
            _stormforgedMonitorGUID = stormforgedMonitor.GUID;
            stormforgedMonitor.SetWalk(false);
            /// The npc would search an alternative way to get to the last waypoint without this unit State.
            stormforgedMonitor.AddUnitState(UnitState.IgnorePathfinding);
            stormforgedMonitor.MotionMaster.MovePath(CreatureIds.STORMFORGED_MONITOR * 100, false);
        }

        stormforgedEradictor = player.SummonCreature(CreatureIds.STORMFORGED_ERADICTOR, Misc.StormforgedEradictorPosition, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(60));

        if (stormforgedEradictor)
        {
            _stormforgedEradictorGUID = stormforgedEradictor.GUID;
            stormforgedEradictor.MotionMaster.MovePath(CreatureIds.STORMFORGED_ERADICTOR * 100, false);
        }

        return true;
    }
}

[Script]
internal class AreatriggerStormwindTeleportUnit : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
    public void OnUnitEnter(Unit unit)
    {
        var player = unit.AsPlayer;

        if (player == null)
            return;

        player.SpellFactory.CastSpell(unit, SpellIds.DUST_IN_THE_STORMWIND);
        player.KilledMonsterCredit(CreatureIds.KILL_CREDIT_TELEPORT_STORMWIND);
    }
}