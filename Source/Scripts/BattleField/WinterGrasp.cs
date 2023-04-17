// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.BattleFields;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IBattlefield;
using Forged.MapServer.Text;
using Framework.Constants;
using Serilog;

namespace Game.BattleFields;

internal class BattlefieldWg : BattleField
{
    private readonly List<ObjectGuid>[] _vehicles = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
    private readonly List<BfWgGameObjectBuilding> _buildingsInZone = new();
    private readonly List<ObjectGuid> _canonList = new();

    private readonly List<ObjectGuid>[] _defenderPortalList = new List<ObjectGuid>[SharedConst.PvpTeamsCount];

    private readonly List<WgWorkshop> _workshops = new();
    private bool _isRelicInteractible;
    private uint _tenacityStack;

    private int _tenacityTeam;

    private ObjectGuid _titansRelicGUID;

    public BattlefieldWg(Map map) : base(map) { }

    public override bool SetupBattlefield()
    {
        m_TypeId = (uint)BattleFieldTypes.WinterGrasp; // See enum BattlefieldTypes
        m_BattleId = BattlefieldIds.WG;
        m_ZoneId = (uint)AreaId.Wintergrasp;

        InitStalker(WgNpcs.STALKER, WgConst.WintergraspStalkerPos);

        m_MaxPlayer = GetDefaultValue("Wintergrasp.PlayerMax", 100);
        m_IsEnabled = GetDefaultValue("Wintergrasp.Enable", false);
        m_MinPlayer = GetDefaultValue("Wintergrasp.PlayerMin", 0);
        m_MinLevel = GetDefaultValue("Wintergrasp.PlayerMinLvl", 77);
        m_BattleTime = GetDefaultValue("Wintergrasp.BattleTimer", 30) * Time.MINUTE * Time.IN_MILLISECONDS;
        m_NoWarBattleTime = GetDefaultValue("Wintergrasp.NoBattleTimer", 150) * Time.MINUTE * Time.IN_MILLISECONDS;
        m_RestartAfterCrash = GetDefaultValue("Wintergrasp.CrashRestartTimer", 10) * Time.MINUTE * Time.IN_MILLISECONDS;

        m_TimeForAcceptInvite = 20;
        m_StartGroupingTimer = 15 * Time.MINUTE * Time.IN_MILLISECONDS;
        _tenacityTeam = TeamIds.Neutral;

        KickPosition = new WorldLocation(m_MapId, 5728.117f, 2714.346f, 697.733f, 0);

        RegisterZone(m_ZoneId);

        for (var team = 0; team < SharedConst.PvpTeamsCount; ++team)
        {
            _defenderPortalList[team] = new List<ObjectGuid>();
            _vehicles[team] = new List<ObjectGuid>();
        }

        // Load from db
        if (Global.WorldStateMgr.GetValue(WorldStates.BattlefieldWgShowTimeNextBattle, m_Map) == 0 &&
            Global.WorldStateMgr.GetValue(WgConst.ClockWorldState[0], m_Map) == 0)
        {
            Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgShowTimeNextBattle, 0, false, m_Map);
            Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgDefender, RandomHelper.IRand(0, 1), false, m_Map);
            Global.WorldStateMgr.SetValueAndSaveInDb(WgConst.ClockWorldState[0], (int)(GameTime.GetGameTime() + m_NoWarBattleTime / Time.IN_MILLISECONDS), false, m_Map);
        }

        m_isActive = Global.WorldStateMgr.GetValue(WorldStates.BattlefieldWgShowTimeNextBattle, m_Map) == 0;
        m_DefenderTeam = (uint)Global.WorldStateMgr.GetValue(WorldStates.BattlefieldWgDefender, m_Map);

        m_Timer = (uint)(Global.WorldStateMgr.GetValue(WgConst.ClockWorldState[0], m_Map) - GameTime.GetGameTime());

        if (m_isActive)
        {
            m_isActive = false;
            m_Timer = m_RestartAfterCrash;
        }

        Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgAttacker, (int)GetAttackerTeam(), false, m_Map);
        Global.WorldStateMgr.SetValue(WgConst.ClockWorldState[1], (int)(GameTime.GetGameTime() + m_Timer / Time.IN_MILLISECONDS), false, m_Map);

        foreach (var gy in WgConst.WgGraveYard)
        {
            BfGraveyardWg graveyard = new(this);

            // When between games, the graveyard is controlled by the defending team
            if (gy.StartControl == TeamIds.Neutral)
                graveyard.Initialize(m_DefenderTeam, gy.GraveyardID);
            else
                graveyard.Initialize(gy.StartControl, gy.GraveyardID);

            graveyard.SetTextId(gy.TextId);
            m_GraveyardList.Add(graveyard);
        }


        // Spawn workshop creatures and gameobjects
        for (byte i = 0; i < WgConst.MAX_WORKSHOPS; i++)
        {
            WgWorkshop workshop = new(this, i);

            if (i < WgWorkshopIds.NE)
                workshop.GiveControlTo(GetAttackerTeam(), true);
            else
                workshop.GiveControlTo(GetDefenderTeam(), true);

            // Note: Capture point is added once the gameobject is created.
            _workshops.Add(workshop);
        }

        // Spawn turrets and hide them per default
        foreach (var turret in WgConst.WgTurret)
        {
            var towerCannonPos = turret;
            var creature = SpawnCreature(WgNpcs.TOWER_CANNON, towerCannonPos);

            if (creature)
            {
                _canonList.Add(creature.GUID);
                HideNpc(creature);
            }
        }

        // Spawn all gameobjects
        foreach (var build in WgConst.WgGameObjectBuilding)
        {
            var go = SpawnGameObject(build.Entry, build.Pos, build.Rot);

            if (go)
            {
                BfWgGameObjectBuilding b = new(this, build.BuildingType, build.WorldState);
                b.Init(go);

                if (!m_IsEnabled &&
                    go.Entry == WgGameObjects.VAULT_GATE)
                    go.SetDestructibleState(GameObjectDestructibleState.Destroyed);

                _buildingsInZone.Add(b);
            }
        }

        // Spawning portal defender
        foreach (var teleporter in WgConst.WgPortalDefenderData)
        {
            var go = SpawnGameObject(teleporter.AllianceEntry, teleporter.Pos, teleporter.Rot);

            if (go)
            {
                _defenderPortalList[TeamIds.Alliance].Add(go.GUID);
                go.SetRespawnTime((int)(GetDefenderTeam() == TeamIds.Alliance ? BattlegroundConst.RespawnImmediately : BattlegroundConst.RespawnOneDay));
            }

            go = SpawnGameObject(teleporter.HordeEntry, teleporter.Pos, teleporter.Rot);

            if (go)
            {
                _defenderPortalList[TeamIds.Horde].Add(go.GUID);
                go.SetRespawnTime((int)(GetDefenderTeam() == TeamIds.Horde ? BattlegroundConst.RespawnImmediately : BattlegroundConst.RespawnOneDay));
            }
        }

        UpdateCounterVehicle(true);

        return true;
    }

    public override void OnBattleStart()
    {
        // Spawn titan relic
        var relic = SpawnGameObject(WgGameObjects.TITAN_S_RELIC, WgConst.RelicPos, WgConst.RelicRot);

        if (relic)
        {
            // Update faction of relic, only Attacker can click on
            relic. // Update faction of relic, only Attacker can click on
                Faction = WgConst.WintergraspFaction[GetAttackerTeam()];

            // Set in use (not allow to click on before last door is broken)
            relic.SetFlag(GameObjectFlags.InUse | GameObjectFlags.NotSelectable);
            _titansRelicGUID = relic.GUID;
        }
        else
            Log.Logger.Error("WG: Failed to spawn titan relic.");

        Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgAttacker, (int)GetAttackerTeam(), false, m_Map);
        Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgDefender, (int)GetDefenderTeam(), false, m_Map);
        Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgShowTimeNextBattle, 0, false, m_Map);
        Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgShowTimeBattleEnd, 1, false, m_Map);
        Global.WorldStateMgr.SetValueAndSaveInDb(WgConst.ClockWorldState[0], (int)(GameTime.GetGameTime() + m_Timer / Time.IN_MILLISECONDS), false, m_Map);

        // Update tower visibility and update faction
        foreach (var guid in _canonList)
        {
            var creature = GetCreature(guid);

            if (creature)
            {
                ShowNpc(creature, true);
                creature.Faction = WgConst.WintergraspFaction[GetDefenderTeam()];
            }
        }

        // Rebuild all wall
        foreach (var wall in _buildingsInZone)
            if (wall != null)
            {
                wall.Rebuild();
                wall.UpdateTurretAttack(false);
            }

        SetData(WgData.BROKEN_TOWER_ATT, 0);
        SetData(WgData.BROKEN_TOWER_DEF, 0);
        SetData(WgData.DAMAGED_TOWER_ATT, 0);
        SetData(WgData.DAMAGED_TOWER_DEF, 0);

        // Update graveyard (in no war Time all graveyard is to deffender, in war Time, depend of base)
        foreach (var workShop in _workshops)
            workShop?.UpdateGraveyardAndWorkshop();

        for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
            foreach (var guid in m_players[team])
            {
                // Kick player in orb room, TODO: offline player ?
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    if (5500 > player.Location.X &&
                        player.Location.X > 5392 &&
                        player.Location.Y < 2880 &&
                        player.Location.Y > 2800 &&
                        player.Location.Z < 480)
                        player.TeleportTo(571, 5349.8686f, 2838.481f, 409.240f, 0.046328f);
            }

        // Initialize vehicle counter
        UpdateCounterVehicle(true);
        // Send start warning to all players
        SendWarning(WintergraspText.START_BATTLE);
    }

    public void UpdateCounterVehicle(bool init)
    {
        if (init)
        {
            SetData(WgData.VEHICLE_H, 0);
            SetData(WgData.VEHICLE_A, 0);
        }

        SetData(WgData.MAX_VEHICLE_H, 0);
        SetData(WgData.MAX_VEHICLE_A, 0);

        foreach (var workshop in _workshops)
            if (workshop.GetTeamControl() == TeamIds.Alliance)
                UpdateData(WgData.MAX_VEHICLE_A, 4);
            else if (workshop.GetTeamControl() == TeamIds.Horde)
                UpdateData(WgData.MAX_VEHICLE_H, 4);

        UpdateVehicleCountWg();
    }

    public override void OnBattleEnd(bool endByTimer)
    {
        // Remove relic
        if (!_titansRelicGUID.IsEmpty)
        {
            var relic = GetGameObject(_titansRelicGUID);

            if (relic)
                relic.RemoveFromWorld();
        }

        _titansRelicGUID.Clear();

        // change collision wall State closed
        foreach (var building in _buildingsInZone)
            building.RebuildGate();

        // update win statistics
        {
            WorldStates worldStateId;

            // successful defense
            if (endByTimer)
                worldStateId = GetDefenderTeam() == TeamIds.Horde ? WorldStates.BattlefieldWgDefendedH : WorldStates.BattlefieldWgDefendedA;
            // successful attack (note that teams have already been swapped, so defender team is the one who won)
            else
                worldStateId = GetDefenderTeam() == TeamIds.Horde ? WorldStates.BattlefieldWgAttackedH : WorldStates.BattlefieldWgAttackedA;

            Global.WorldStateMgr.SetValueAndSaveInDb(worldStateId, Global.WorldStateMgr.GetValue((int)worldStateId, m_Map) + 1, false, m_Map);
        }

        Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgDefender, (int)GetDefenderTeam(), false, m_Map);
        Global.WorldStateMgr.SetValueAndSaveInDb(WorldStates.BattlefieldWgShowTimeNextBattle, 1, false, m_Map);
        Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgShowTimeBattleEnd, 0, false, m_Map);
        Global.WorldStateMgr.SetValue(WgConst.ClockWorldState[1], (int)(GameTime.GetGameTime() + m_Timer / Time.IN_MILLISECONDS), false, m_Map);

        // Remove turret
        foreach (var guid in _canonList)
        {
            var creature = GetCreature(guid);

            if (creature)
            {
                if (!endByTimer)
                    creature.Faction = WgConst.WintergraspFaction[GetDefenderTeam()];

                HideNpc(creature);
            }
        }

        // Update all graveyard, control is to defender when no wartime
        for (byte i = 0; i < WgGraveyardId.HORDE; i++)
        {
            var graveyard = GetGraveyardById(i);

            graveyard?.GiveControlTo(GetDefenderTeam());
        }

        // Update portals
        foreach (var guid in _defenderPortalList[GetDefenderTeam()])
        {
            var portal = GetGameObject(guid);

            if (portal)
                portal.SetRespawnTime((int)BattlegroundConst.RespawnImmediately);
        }

        foreach (var guid in _defenderPortalList[GetAttackerTeam()])
        {
            var portal = GetGameObject(guid);

            if (portal)
                portal.SetRespawnTime((int)BattlegroundConst.RespawnOneDay);
        }

        foreach (var guid in m_PlayersInWar[GetDefenderTeam()])
        {
            var player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
            {
                player.SpellFactory.CastSpell(player, WgSpells.ESSENCE_OF_WINTERGRASP, true);
                player.SpellFactory.CastSpell(player, WgSpells.VICTORY_REWARD, true);
                // Complete victory quests
                player.AreaExploredOrEventHappens(WintergraspQuests.VICTORY_ALLIANCE);
                player.AreaExploredOrEventHappens(WintergraspQuests.VICTORY_HORDE);
                // Send Wintergrasp victory Achievement
                DoCompleteOrIncrementAchievement(WgAchievements.WIN_WG, player);

                // Award Achievement for succeeding in Wintergrasp in 10 minutes or less
                if (!endByTimer &&
                    GetTimer() <= 10000)
                    DoCompleteOrIncrementAchievement(WgAchievements.WIN_WG_TIMER10, player);
            }
        }

        foreach (var guid in m_PlayersInWar[GetAttackerTeam()])
        {
            var player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                player.SpellFactory.CastSpell(player, WgSpells.DEFEAT_REWARD, true);
        }

        for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
        {
            foreach (var guid in m_PlayersInWar[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    RemoveAurasFromPlayer(player);
            }

            m_PlayersInWar[team].Clear();

            foreach (var guid in _vehicles[team])
            {
                var creature = GetCreature(guid);

                if (creature)
                    if (creature.IsVehicle)
                        creature.DespawnOrUnsummon();
            }

            _vehicles[team].Clear();
        }

        if (!endByTimer)
            for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
                foreach (var guid in m_players[team])
                {
                    var player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                    {
                        player.RemoveAurasDueToSpell(m_DefenderTeam == TeamIds.Alliance ? WgSpells.HORDE_CONTROL_PHASE_SHIFT : WgSpells.ALLIANCE_CONTROL_PHASE_SHIFT, player.GUID);
                        player.AddAura(m_DefenderTeam == TeamIds.Horde ? WgSpells.HORDE_CONTROL_PHASE_SHIFT : WgSpells.ALLIANCE_CONTROL_PHASE_SHIFT, player);
                    }
                }

        if (!endByTimer) // win alli/horde
            SendWarning((GetDefenderTeam() == TeamIds.Alliance) ? WintergraspText.FORTRESS_CAPTURE_ALLIANCE : WintergraspText.FORTRESS_CAPTURE_HORDE);
        else // defend alli/horde
            SendWarning((GetDefenderTeam() == TeamIds.Alliance) ? WintergraspText.FORTRESS_DEFEND_ALLIANCE : WintergraspText.FORTRESS_DEFEND_HORDE);
    }

    public override void DoCompleteOrIncrementAchievement(uint achievement, Player player, byte incrementNumber = 1)
    {
        var achievementEntry = CliDB.AchievementStorage.LookupByKey(achievement);

        if (achievementEntry == null)
            return;

        switch (achievement)
        {
            //removed by TC
            //case ACHIEVEMENTS_WIN_WG_100:
            //{
            // player.UpdateAchievementCriteria();
            //}
            default:
            {
                if (player)
                    player.CompletedAchievement(achievementEntry);

                break;
            }
        }
    }

    public override void OnStartGrouping()
    {
        SendWarning(WintergraspText.START_GROUPING);
    }

    public override void OnCreatureCreate(Creature creature)
    {
        // Accessing to db spawned creatures
        switch (creature.Entry)
        {
            case WgNpcs.DWARVEN_SPIRIT_GUIDE:
            case WgNpcs.TAUNKA_SPIRIT_GUIDE:
            {
                var teamIndex = (creature.Entry == WgNpcs.DWARVEN_SPIRIT_GUIDE ? TeamIds.Alliance : TeamIds.Horde);
                var graveyardId = (byte)GetSpiritGraveyardId(creature.Area);

                m_GraveyardList[graveyardId]?.SetSpirit(creature, teamIndex);

                break;
            }
        }

        // untested code - not sure if it is valid.
        if (IsWarTime())
            switch (creature.Entry)
            {
                case WgNpcs.SIEGE_ENGINE_ALLIANCE:
                case WgNpcs.SIEGE_ENGINE_HORDE:
                case WgNpcs.CATAPULT:
                case WgNpcs.DEMOLISHER:
                {
                    if (!creature.ToTempSummon() ||
                        creature.ToTempSummon().GetSummonerGUID().IsEmpty ||
                        !Global.ObjAccessor.FindPlayer(creature.ToTempSummon().GetSummonerGUID()))
                    {
                        creature.DespawnOrUnsummon();

                        return;
                    }

                    var creator = Global.ObjAccessor.FindPlayer(creature.ToTempSummon().GetSummonerGUID());
                    var teamIndex = creator.TeamId;

                    if (teamIndex == TeamIds.Horde)
                    {
                        if (GetData(WgData.VEHICLE_H) < GetData(WgData.MAX_VEHICLE_H))
                        {
                            UpdateData(WgData.VEHICLE_H, 1);
                            creature.AddAura(WgSpells.HORDE_FLAG, creature);
                            _vehicles[teamIndex].Add(creature.GUID);
                            UpdateVehicleCountWg();
                        }
                        else
                        {
                            creature.DespawnOrUnsummon();

                            return;
                        }
                    }
                    else
                    {
                        if (GetData(WgData.VEHICLE_A) < GetData(WgData.MAX_VEHICLE_A))
                        {
                            UpdateData(WgData.VEHICLE_A, 1);
                            creature.AddAura(WgSpells.ALLIANCE_FLAG, creature);
                            _vehicles[teamIndex].Add(creature.GUID);
                            UpdateVehicleCountWg();
                        }
                        else
                        {
                            creature.DespawnOrUnsummon();

                            return;
                        }
                    }

                    creature.SpellFactory.CastSpell(creator, WgSpells.GRAB_PASSENGER, true);

                    break;
                }
            }
    }

    public override void OnCreatureRemove(Creature c) { }

    public override void OnGameObjectCreate(GameObject go)
    {
        uint workshopId;

        switch (go.Entry)
        {
            case WgGameObjects.FACTORY_BANNER_NE:
                workshopId = WgWorkshopIds.NE;

                break;
            case WgGameObjects.FACTORY_BANNER_NW:
                workshopId = WgWorkshopIds.NW;

                break;
            case WgGameObjects.FACTORY_BANNER_SE:
                workshopId = WgWorkshopIds.SE;

                break;
            case WgGameObjects.FACTORY_BANNER_SW:
                workshopId = WgWorkshopIds.SW;

                break;
            default:
                return;
        }

        foreach (var workshop in _workshops)
            if (workshop.GetId() == workshopId)
            {
                WintergraspCapturePoint capturePoint = new(this, GetAttackerTeam());

                capturePoint.SetCapturePointData(go);
                capturePoint.LinkToWorkshop(workshop);
                AddCapturePoint(capturePoint);

                break;
            }
    }

    public override void HandleKill(Player killer, Unit victim)
    {
        if (killer == victim)
            return;

        if (victim.IsTypeId(TypeId.Player))
        {
            HandlePromotion(killer, victim);
            // Allow to Skin non-released corpse
            victim.SetUnitFlag(UnitFlags.Skinnable);
        }

        // @todo Recent PvP activity worldstate
    }

    public override void OnUnitDeath(Unit unit)
    {
        if (IsWarTime())
            if (unit.IsVehicle)
                if (FindAndRemoveVehicleFromList(unit))
                    UpdateVehicleCountWg();
    }

    public void HandlePromotion(Player playerKiller, Unit unitKilled)
    {
        var teamId = playerKiller.TeamId;

        foreach (var guid in m_PlayersInWar[teamId])
        {
            var player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                if (player.GetDistance2d(unitKilled) < 40.0f)
                    PromotePlayer(player);
        }
    }

    public override void OnPlayerJoinWar(Player player)
    {
        RemoveAurasFromPlayer(player);

        player.SpellFactory.CastSpell(player, WgSpells.RECRUIT, true);

        if (player.Zone != m_ZoneId)
        {
            if (player.TeamId == GetDefenderTeam())
                player.TeleportTo(571, 5345, 2842, 410, 3.14f);
            else
            {
                if (player.TeamId == TeamIds.Horde)
                    player.TeleportTo(571, 5025.857422f, 3674.628906f, 362.737122f, 4.135169f);
                else
                    player.TeleportTo(571, 5101.284f, 2186.564f, 373.549f, 3.812f);
            }
        }

        UpdateTenacity();

        if (player.TeamId == GetAttackerTeam())
        {
            if (GetData(WgData.BROKEN_TOWER_ATT) < 3)
                player.SetAuraStack(WgSpells.TOWER_CONTROL, player, 3 - GetData(WgData.BROKEN_TOWER_ATT));
        }
        else
        {
            if (GetData(WgData.BROKEN_TOWER_ATT) > 0)
                player.SetAuraStack(WgSpells.TOWER_CONTROL, player, GetData(WgData.BROKEN_TOWER_ATT));
        }
    }

    public override void OnPlayerLeaveWar(Player player)
    {
        // Remove all aura from WG // @todo false we can go out of this zone on retail and keep Rank buff, remove on end of WG
        if (!player.Session.PlayerLogout)
        {
            var vehicle = player.VehicleCreatureBase;

            if (vehicle) // Remove vehicle of player if he go out.
                vehicle.DespawnOrUnsummon();

            RemoveAurasFromPlayer(player);
        }

        player.RemoveAura(WgSpells.HORDE_CONTROLS_FACTORY_PHASE_SHIFT);
        player.RemoveAura(WgSpells.ALLIANCE_CONTROLS_FACTORY_PHASE_SHIFT);
        player.RemoveAura(WgSpells.HORDE_CONTROL_PHASE_SHIFT);
        player.RemoveAura(WgSpells.ALLIANCE_CONTROL_PHASE_SHIFT);
        UpdateTenacity();
    }

    public override void OnPlayerLeaveZone(Player player)
    {
        if (!m_isActive)
            RemoveAurasFromPlayer(player);

        player.RemoveAura(WgSpells.HORDE_CONTROLS_FACTORY_PHASE_SHIFT);
        player.RemoveAura(WgSpells.ALLIANCE_CONTROLS_FACTORY_PHASE_SHIFT);
        player.RemoveAura(WgSpells.HORDE_CONTROL_PHASE_SHIFT);
        player.RemoveAura(WgSpells.ALLIANCE_CONTROL_PHASE_SHIFT);
    }

    public override void OnPlayerEnterZone(Player player)
    {
        if (!m_isActive)
            RemoveAurasFromPlayer(player);

        player.AddAura(m_DefenderTeam == TeamIds.Horde ? WgSpells.HORDE_CONTROL_PHASE_SHIFT : WgSpells.ALLIANCE_CONTROL_PHASE_SHIFT, player);
    }

    public override uint GetData(uint data)
    {
        switch ((AreaId)data)
        {
            // Used to determine when the phasing spells must be cast
            // See: SpellArea.IsFitToRequirements
            case AreaId.TheSunkenRing:
            case AreaId.TheBrokenTemplate:
            case AreaId.WestparkWorkshop:
            case AreaId.EastparkWorkshop:
                // Graveyards and Workshops are controlled by the same team.
                var graveyard = GetGraveyardById((int)GetSpiritGraveyardId(data));

                if (graveyard != null)
                    return graveyard.GetControlTeamId();

                break;
        }

        return base.GetData(data);
    }

    public void BrokenWallOrTower(uint team, BfWgGameObjectBuilding building)
    {
        if (team == GetDefenderTeam())
            foreach (var guid in m_PlayersInWar[GetAttackerTeam()])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    if (player.GetDistance2d(GetGameObject(building.GetGUID())) < 50.0f)
                        player.KilledMonsterCredit(WintergraspQuests.CREDIT_DEFEND_SIEGE);
            }
    }

    // Called when a tower is broke
    public void UpdatedDestroyedTowerCount(uint team)
    {
        // Southern tower
        if (team == GetAttackerTeam())
        {
            // Update counter
            UpdateData(WgData.DAMAGED_TOWER_ATT, -1);
            UpdateData(WgData.BROKEN_TOWER_ATT, 1);

            // Remove buff stack on attackers
            foreach (var guid in m_PlayersInWar[GetAttackerTeam()])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.RemoveAuraFromStack(WgSpells.TOWER_CONTROL);
            }

            // Add buff stack to defenders and give Achievement/quest credit
            foreach (var guid in m_PlayersInWar[GetDefenderTeam()])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                {
                    player.SpellFactory.CastSpell(player, WgSpells.TOWER_CONTROL, true);
                    player.KilledMonsterCredit(WintergraspQuests.CREDIT_TOWERS_DESTROYED);
                    DoCompleteOrIncrementAchievement(WgAchievements.WG_TOWER_DESTROY, player);
                }
            }

            // If all three south towers are destroyed (ie. all attack towers), remove ten minutes from battle Time
            if (GetData(WgData.BROKEN_TOWER_ATT) == 3)
            {
                if ((int)(m_Timer - 600000) < 0)
                    m_Timer = 0;
                else
                    m_Timer -= 600000;

                Global.WorldStateMgr.SetValue(WgConst.ClockWorldState[0], (int)(GameTime.GetGameTime() + m_Timer / Time.IN_MILLISECONDS), false, m_Map);
            }
        }
        else // Keep tower
        {
            UpdateData(WgData.DAMAGED_TOWER_DEF, -1);
            UpdateData(WgData.BROKEN_TOWER_DEF, 1);
        }
    }

    public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker)
    {
        if (!obj ||
            !IsWarTime())
            return;

        // We handle only gameobjects here
        var go = obj.AsGameObject;

        if (!go)
            return;

        // On click on titan relic
        if (go.Entry == WgGameObjects.TITAN_S_RELIC)
        {
            var relic = GetRelic();

            if (CanInteractWithRelic())
                EndBattle(false);
            else if (relic)
                relic.SetRespawnTime(0);
        }

        // if destroy or Damage event, search the wall/tower and update worldstate/send warning message
        foreach (var building in _buildingsInZone)
            if (go.GUID == building.GetGUID())
            {
                var buildingGo = GetGameObject(building.GetGUID());

                if (buildingGo)
                {
                    if (buildingGo.Template.DestructibleBuilding.DamagedEvent == eventId)
                        building.Damaged();

                    if (buildingGo.Template.DestructibleBuilding.DestroyedEvent == eventId)
                        building.Destroyed();

                    break;
                }
            }
    }

    // Called when a tower is damaged, used for honor reward calcul
    public void UpdateDamagedTowerCount(uint team)
    {
        if (team == GetAttackerTeam())
            UpdateData(WgData.DAMAGED_TOWER_ATT, 1);
        else
            UpdateData(WgData.DAMAGED_TOWER_DEF, 1);
    }

    public GameObject GetRelic()
    {
        return GetGameObject(_titansRelicGUID);
    }

    // Define if player can interact with the relic
    public void SetRelicInteractible(bool allow)
    {
        _isRelicInteractible = allow;
    }

    private uint GetSpiritGraveyardId(uint areaId)
    {
        switch ((AreaId)areaId)
        {
            case AreaId.WintergraspFortress:
                return WgGraveyardId.KEEP;
            case AreaId.TheSunkenRing:
                return WgGraveyardId.WORKSHOP_NE;
            case AreaId.TheBrokenTemplate:
                return WgGraveyardId.WORKSHOP_NW;
            case AreaId.WestparkWorkshop:
                return WgGraveyardId.WORKSHOP_SW;
            case AreaId.EastparkWorkshop:
                return WgGraveyardId.WORKSHOP_SE;
            case AreaId.Wintergrasp:
                return WgGraveyardId.ALLIANCE;
            case AreaId.TheChilledQuagmire:
                return WgGraveyardId.HORDE;
            default:
                Log.Logger.Error("BattlefieldWG.GetSpiritGraveyardId: Unexpected Area Id {0}", areaId);

                break;
        }

        return 0;
    }

    private bool FindAndRemoveVehicleFromList(Unit vehicle)
    {
        for (byte i = 0; i < SharedConst.PvpTeamsCount; ++i)
            if (_vehicles[i].Contains(vehicle.GUID))
            {
                _vehicles[i].Remove(vehicle.GUID);

                if (i == TeamIds.Horde)
                    UpdateData(WgData.VEHICLE_H, -1);
                else
                    UpdateData(WgData.VEHICLE_A, -1);

                return true;
            }

        return false;
    }

    // Update rank for player
    private void PromotePlayer(Player killer)
    {
        if (!m_isActive)
            return;

        // Updating rank of player
        var aur = killer.GetAura(WgSpells.RECRUIT);

        if (aur != null)
        {
            if (aur.StackAmount >= 5)
            {
                killer.RemoveAura(WgSpells.RECRUIT);
                killer.SpellFactory.CastSpell(killer, WgSpells.CORPORAL, true);
                var stalker = GetCreature(StalkerGuid);

                if (stalker)
                    Global.CreatureTextMgr.SendChat(stalker, WintergraspText.RANK_CORPORAL, killer, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, SoundKitPlayType.Normal, TeamFaction.Other, false, killer);
            }
            else
                killer.SpellFactory.CastSpell(killer, WgSpells.RECRUIT, true);
        }
        else if ((aur = killer.GetAura(WgSpells.CORPORAL)) != null)
        {
            if (aur.StackAmount >= 5)
            {
                killer.RemoveAura(WgSpells.CORPORAL);
                killer.SpellFactory.CastSpell(killer, WgSpells.LIEUTENANT, true);
                var stalker = GetCreature(StalkerGuid);

                if (stalker)
                    Global.CreatureTextMgr.SendChat(stalker, WintergraspText.RANK_FIRST_LIEUTENANT, killer, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, SoundKitPlayType.Normal, TeamFaction.Other, false, killer);
            }
            else
                killer.SpellFactory.CastSpell(killer, WgSpells.CORPORAL, true);
        }
    }

    private void RemoveAurasFromPlayer(Player player)
    {
        player.RemoveAura(WgSpells.RECRUIT);
        player.RemoveAura(WgSpells.CORPORAL);
        player.RemoveAura(WgSpells.LIEUTENANT);
        player.RemoveAura(WgSpells.TOWER_CONTROL);
        player.RemoveAura(WgSpells.SPIRITUAL_IMMUNITY);
        player.RemoveAura(WgSpells.TENACITY);
        player.RemoveAura(WgSpells.ESSENCE_OF_WINTERGRASP);
        player.RemoveAura(WgSpells.WINTERGRASP_RESTRICTED_FLIGHT_AREA);
    }

    // Update vehicle Count WorldState to player
    private void UpdateVehicleCountWg()
    {
        Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgVehicleH, (int)GetData(WgData.VEHICLE_H), false, m_Map);
        Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgMaxVehicleH, (int)GetData(WgData.MAX_VEHICLE_H), false, m_Map);
        Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgVehicleA, (int)GetData(WgData.VEHICLE_A), false, m_Map);
        Global.WorldStateMgr.SetValue(WorldStates.BattlefieldWgMaxVehicleA, (int)GetData(WgData.MAX_VEHICLE_A), false, m_Map);
    }

    private void UpdateTenacity()
    {
        var alliancePlayers = m_PlayersInWar[TeamIds.Alliance].Count;
        var hordePlayers = m_PlayersInWar[TeamIds.Horde].Count;
        var newStack = 0;

        if (alliancePlayers != 0 &&
            hordePlayers != 0)
        {
            if (alliancePlayers < hordePlayers)
                newStack = (int)((((double)hordePlayers / alliancePlayers) - 1) * 4); // positive, should cast on alliance
            else if (alliancePlayers > hordePlayers)
                newStack = (int)((1 - ((double)alliancePlayers / hordePlayers)) * 4); // negative, should cast on horde
        }

        if (newStack == _tenacityStack)
            return;

        _tenacityStack = (uint)newStack;

        // Remove old buff
        if (_tenacityTeam != TeamIds.Neutral)
        {
            foreach (var guid in m_players[_tenacityTeam])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    if (player.Level >= m_MinLevel)
                        player.RemoveAura(WgSpells.TENACITY);
            }

            foreach (var guid in _vehicles[_tenacityTeam])
            {
                var creature = GetCreature(guid);

                if (creature)
                    creature.RemoveAura(WgSpells.TENACITY_VEHICLE);
            }
        }

        // Apply new buff
        if (newStack != 0)
        {
            _tenacityTeam = newStack > 0 ? TeamIds.Alliance : TeamIds.Horde;

            if (newStack < 0)
                newStack = -newStack;

            if (newStack > 20)
                newStack = 20;

            var buffHonor = WgSpells.GREATEST_HONOR;

            if (newStack < 15)
                buffHonor = WgSpells.GREATER_HONOR;

            if (newStack < 10)
                buffHonor = WgSpells.GREAT_HONOR;

            if (newStack < 5)
                buffHonor = 0;

            foreach (var guid in m_PlayersInWar[_tenacityTeam])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.SetAuraStack(WgSpells.TENACITY, player, (uint)newStack);
            }

            foreach (var guid in _vehicles[_tenacityTeam])
            {
                var creature = GetCreature(guid);

                if (creature)
                    creature.SetAuraStack(WgSpells.TENACITY_VEHICLE, creature, (uint)newStack);
            }

            if (buffHonor != 0)
            {
                foreach (var guid in m_PlayersInWar[_tenacityTeam])
                {
                    var player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        player.SpellFactory.CastSpell(player, buffHonor, true);
                }

                foreach (var guid in _vehicles[_tenacityTeam])
                {
                    var creature = GetCreature(guid);

                    if (creature)
                        creature.SpellFactory.CastSpell(creature, buffHonor, true);
                }
            }
        }
        else
            _tenacityTeam = TeamIds.Neutral;
    }

    // Define relic object
    private void SetRelic(ObjectGuid relicGUID)
    {
        _titansRelicGUID = relicGUID;
    }

    // Check if players can interact with the relic (Only if the last door has been broken)
    private bool CanInteractWithRelic()
    {
        return _isRelicInteractible;
    }
}

internal class BfWgGameObjectBuilding
{
    // Creature associations
    private readonly List<ObjectGuid>[] _creatureBottomList = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
    private readonly List<ObjectGuid>[] _creatureTopList = new List<ObjectGuid>[SharedConst.PvpTeamsCount];

    // GameObject associations
    private readonly List<ObjectGuid>[] _gameObjectList = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
    private readonly List<ObjectGuid> _towerCannonBottomList = new();
    private readonly List<ObjectGuid> _turretTopList = new();

    private readonly WgGameObjectBuildingType _type;

    // WG object
    private readonly BattlefieldWg _wg;

    private readonly uint _worldState;

    // Linked gameobject
    private ObjectGuid _buildGUID;

    private WgGameObjectState _state;

    private StaticWintergraspTowerInfo _staticTowerInfo;

    // the team that controls this point
    private uint _teamControl;

    public BfWgGameObjectBuilding(BattlefieldWg wg, WgGameObjectBuildingType type, uint worldState)
    {
        _wg = wg;
        _teamControl = TeamIds.Neutral;
        _type = type;
        _worldState = worldState;
        _state = WgGameObjectState.None;

        for (var i = 0; i < 2; ++i)
        {
            _gameObjectList[i] = new List<ObjectGuid>();
            _creatureBottomList[i] = new List<ObjectGuid>();
            _creatureTopList[i] = new List<ObjectGuid>();
        }
    }

    public void Rebuild()
    {
        switch (_type)
        {
            case WgGameObjectBuildingType.KeepTower:
            case WgGameObjectBuildingType.DoorLast:
            case WgGameObjectBuildingType.Door:
            case WgGameObjectBuildingType.Wall:
                _teamControl = _wg.GetDefenderTeam(); // Objects that are part of the keep should be the defender's

                break;
            case WgGameObjectBuildingType.Tower:
                _teamControl = _wg.GetAttackerTeam(); // The towers in the south should be the Attacker's

                break;
            default:
                _teamControl = TeamIds.Neutral;

                break;
        }

        var build = _wg.GetGameObject(_buildGUID);

        if (build)
        {
            // Rebuild gameobject
            if (build.IsDestructibleBuilding)
            {
                build.SetDestructibleState(GameObjectDestructibleState.Rebuilding, null, true);

                if (build.Entry == WgGameObjects.VAULT_GATE)
                {
                    var go = build.FindNearestGameObject(WgGameObjects.KEEP_COLLISION_WALL, 50.0f);

                    if (go)
                        go.SetGoState(GameObjectState.Active);
                }

                // Update worldstate
                _state = WgGameObjectState.AllianceIntact - ((int)_teamControl * 3);
                Global.WorldStateMgr.SetValueAndSaveInDb((int)_worldState, (int)_state, false, _wg.GetMap());
            }

            UpdateCreatureAndGo();
            build.Faction = WgConst.WintergraspFaction[_teamControl];
        }
    }

    public void RebuildGate()
    {
        var build = _wg.GetGameObject(_buildGUID);

        if (build != null)
            if (build.IsDestructibleBuilding &&
                build.Entry == WgGameObjects.VAULT_GATE)
            {
                var go = build.FindNearestGameObject(WgGameObjects.KEEP_COLLISION_WALL, 50.0f);

                go?.SetGoState(GameObjectState.Ready); //not GO_STATE_ACTIVE
            }
    }

    // Called when associated gameobject is damaged
    public void Damaged()
    {
        // Update worldstate
        _state = WgGameObjectState.AllianceDamage - ((int)_teamControl * 3);
        Global.WorldStateMgr.SetValueAndSaveInDb((int)_worldState, (int)_state, false, _wg.GetMap());

        // Send warning message
        if (_staticTowerInfo != null) // tower Damage + Name
            _wg.SendWarning(_staticTowerInfo.DamagedTextId);

        foreach (var guid in _creatureTopList[_wg.GetAttackerTeam()])
        {
            var creature = _wg.GetCreature(guid);

            if (creature)
                _wg.HideNpc(creature);
        }

        foreach (var guid in _turretTopList)
        {
            var creature = _wg.GetCreature(guid);

            if (creature)
                _wg.HideNpc(creature);
        }

        if (_type == WgGameObjectBuildingType.KeepTower)
            _wg.UpdateDamagedTowerCount(_wg.GetDefenderTeam());
        else if (_type == WgGameObjectBuildingType.Tower)
            _wg.UpdateDamagedTowerCount(_wg.GetAttackerTeam());
    }

    // Called when associated gameobject is destroyed
    public void Destroyed()
    {
        // Update worldstate
        _state = WgGameObjectState.AllianceDestroy - ((int)_teamControl * 3);
        Global.WorldStateMgr.SetValueAndSaveInDb((int)_worldState, (int)_state, false, _wg.GetMap());

        // Warn players
        if (_staticTowerInfo != null)
            _wg.SendWarning(_staticTowerInfo.DestroyedTextId);

        switch (_type)
        {
            // Inform the global wintergrasp script of the destruction of this object
            case WgGameObjectBuildingType.Tower:
            case WgGameObjectBuildingType.KeepTower:
                _wg.UpdatedDestroyedTowerCount(_teamControl);

                break;
            case WgGameObjectBuildingType.DoorLast:
                var build = _wg.GetGameObject(_buildGUID);

                if (build)
                {
                    var go = build.FindNearestGameObject(WgGameObjects.KEEP_COLLISION_WALL, 50.0f);

                    if (go)
                        go.SetGoState(GameObjectState.Active);
                }

                _wg.SetRelicInteractible(true);

                if (_wg.GetRelic())
                    _wg.GetRelic().RemoveFlag(GameObjectFlags.InUse | GameObjectFlags.NotSelectable);
                else
                    Log.Logger.Error("BattlefieldWG: Titan Relic not found.");

                break;
        }

        _wg.BrokenWallOrTower(_teamControl, this);
    }

    public void Init(GameObject go)
    {
        if (!go)
            return;

        // GameObject associated to object
        _buildGUID = go.GUID;

        switch (_type)
        {
            case WgGameObjectBuildingType.KeepTower:
            case WgGameObjectBuildingType.DoorLast:
            case WgGameObjectBuildingType.Door:
            case WgGameObjectBuildingType.Wall:
                _teamControl = _wg.GetDefenderTeam(); // Objects that are part of the keep should be the defender's

                break;
            case WgGameObjectBuildingType.Tower:
                _teamControl = _wg.GetAttackerTeam(); // The towers in the south should be the Attacker's

                break;
            default:
                _teamControl = TeamIds.Neutral;

                break;
        }

        _state = (WgGameObjectState)Global.WorldStateMgr.GetValue((int)_worldState, _wg.GetMap());

        if (_state == WgGameObjectState.None)
        {
            // set to default State based on Type
            switch (_teamControl)
            {
                case TeamIds.Alliance:
                    _state = WgGameObjectState.AllianceIntact;

                    break;
                case TeamIds.Horde:
                    _state = WgGameObjectState.HordeIntact;

                    break;
                case TeamIds.Neutral:
                    _state = WgGameObjectState.NeutralIntact;

                    break;
            }

            Global.WorldStateMgr.SetValueAndSaveInDb((int)_worldState, (int)_state, false, _wg.GetMap());
        }

        switch (_state)
        {
            case WgGameObjectState.NeutralIntact:
            case WgGameObjectState.AllianceIntact:
            case WgGameObjectState.HordeIntact:
                go.SetDestructibleState(GameObjectDestructibleState.Rebuilding, null, true);

                break;
            case WgGameObjectState.NeutralDestroy:
            case WgGameObjectState.AllianceDestroy:
            case WgGameObjectState.HordeDestroy:
                go.SetDestructibleState(GameObjectDestructibleState.Destroyed);

                break;
            case WgGameObjectState.NeutralDamage:
            case WgGameObjectState.AllianceDamage:
            case WgGameObjectState.HordeDamage:
                go.SetDestructibleState(GameObjectDestructibleState.Damaged);

                break;
        }

        var towerId = -1;

        switch (go.Entry)
        {
            case WgGameObjects.FORTRESS_TOWER1:
                towerId = 0;

                break;
            case WgGameObjects.FORTRESS_TOWER2:
                towerId = 1;

                break;
            case WgGameObjects.FORTRESS_TOWER3:
                towerId = 2;

                break;
            case WgGameObjects.FORTRESS_TOWER4:
                towerId = 3;

                break;
            case WgGameObjects.SHADOWSIGHT_TOWER:
                towerId = 4;

                break;
            case WgGameObjects.WINTER_S_EDGE_TOWER:
                towerId = 5;

                break;
            case WgGameObjects.FLAMEWATCH_TOWER:
                towerId = 6;

                break;
        }

        if (towerId > 3) // Attacker towers
        {
            // Spawn associate gameobjects
            foreach (var gobData in WgConst.AttackTowers[towerId - 4].GameObject)
            {
                var goHorde = _wg.SpawnGameObject(gobData.HordeEntry, gobData.Pos, gobData.Rot);

                if (goHorde)
                    _gameObjectList[TeamIds.Horde].Add(goHorde.GUID);

                var goAlliance = _wg.SpawnGameObject(gobData.AllianceEntry, gobData.Pos, gobData.Rot);

                if (goAlliance)
                    _gameObjectList[TeamIds.Alliance].Add(goAlliance.GUID);
            }

            // Spawn associate npc bottom
            foreach (var creatureData in WgConst.AttackTowers[towerId - 4].CreatureBottom)
            {
                var creature = _wg.SpawnCreature(creatureData.HordeEntry, creatureData.Pos);

                if (creature)
                    _creatureBottomList[TeamIds.Horde].Add(creature.GUID);

                creature = _wg.SpawnCreature(creatureData.AllianceEntry, creatureData.Pos);

                if (creature)
                    _creatureBottomList[TeamIds.Alliance].Add(creature.GUID);
            }
        }

        if (towerId >= 0)
        {
            _staticTowerInfo = WgConst.TowerData[towerId];

            // Spawn Turret bottom
            foreach (var turretPos in WgConst.TowerCannon[towerId].TowerCannonBottom)
            {
                var turret = _wg.SpawnCreature(WgNpcs.TOWER_CANNON, turretPos);

                if (turret)
                {
                    _towerCannonBottomList.Add(turret.GUID);

                    switch (go.Entry)
                    {
                        case WgGameObjects.FORTRESS_TOWER1:
                        case WgGameObjects.FORTRESS_TOWER2:
                        case WgGameObjects.FORTRESS_TOWER3:
                        case WgGameObjects.FORTRESS_TOWER4:
                            turret.Faction = WgConst.WintergraspFaction[_wg.GetDefenderTeam()];

                            break;
                        case WgGameObjects.SHADOWSIGHT_TOWER:
                        case WgGameObjects.WINTER_S_EDGE_TOWER:
                        case WgGameObjects.FLAMEWATCH_TOWER:
                            turret.Faction = WgConst.WintergraspFaction[_wg.GetAttackerTeam()];

                            break;
                    }

                    _wg.HideNpc(turret);
                }
            }

            // Spawn Turret top
            foreach (var towerCannonPos in WgConst.TowerCannon[towerId].TurretTop)
            {
                var turret = _wg.SpawnCreature(WgNpcs.TOWER_CANNON, towerCannonPos);

                if (turret)
                {
                    _turretTopList.Add(turret.GUID);

                    switch (go.Entry)
                    {
                        case WgGameObjects.FORTRESS_TOWER1:
                        case WgGameObjects.FORTRESS_TOWER2:
                        case WgGameObjects.FORTRESS_TOWER3:
                        case WgGameObjects.FORTRESS_TOWER4:
                            turret.Faction = WgConst.WintergraspFaction[_wg.GetDefenderTeam()];

                            break;
                        case WgGameObjects.SHADOWSIGHT_TOWER:
                        case WgGameObjects.WINTER_S_EDGE_TOWER:
                        case WgGameObjects.FLAMEWATCH_TOWER:
                            turret.Faction = WgConst.WintergraspFaction[_wg.GetAttackerTeam()];

                            break;
                    }

                    _wg.HideNpc(turret);
                }
            }

            UpdateCreatureAndGo();
        }
    }

    public void UpdateTurretAttack(bool disable)
    {
        foreach (var guid in _towerCannonBottomList)
        {
            var creature = _wg.GetCreature(guid);

            if (creature)
            {
                var build = _wg.GetGameObject(_buildGUID);

                if (build)
                {
                    if (disable)
                        _wg.HideNpc(creature);
                    else
                        _wg.ShowNpc(creature, true);

                    switch (build.Entry)
                    {
                        case WgGameObjects.FORTRESS_TOWER1:
                        case WgGameObjects.FORTRESS_TOWER2:
                        case WgGameObjects.FORTRESS_TOWER3:
                        case WgGameObjects.FORTRESS_TOWER4:
                        {
                            creature.Faction = WgConst.WintergraspFaction[_wg.GetDefenderTeam()];

                            break;
                        }
                        case WgGameObjects.SHADOWSIGHT_TOWER:
                        case WgGameObjects.WINTER_S_EDGE_TOWER:
                        case WgGameObjects.FLAMEWATCH_TOWER:
                        {
                            creature.Faction = WgConst.WintergraspFaction[_wg.GetAttackerTeam()];

                            break;
                        }
                    }
                }
            }
        }

        foreach (var guid in _turretTopList)
        {
            var creature = _wg.GetCreature(guid);

            if (creature)
            {
                var build = _wg.GetGameObject(_buildGUID);

                if (build)
                {
                    if (disable)
                        _wg.HideNpc(creature);
                    else
                        _wg.ShowNpc(creature, true);

                    switch (build.Entry)
                    {
                        case WgGameObjects.FORTRESS_TOWER1:
                        case WgGameObjects.FORTRESS_TOWER2:
                        case WgGameObjects.FORTRESS_TOWER3:
                        case WgGameObjects.FORTRESS_TOWER4:
                        {
                            creature.Faction = WgConst.WintergraspFaction[_wg.GetDefenderTeam()];

                            break;
                        }
                        case WgGameObjects.SHADOWSIGHT_TOWER:
                        case WgGameObjects.WINTER_S_EDGE_TOWER:
                        case WgGameObjects.FLAMEWATCH_TOWER:
                        {
                            creature.Faction = WgConst.WintergraspFaction[_wg.GetAttackerTeam()];

                            break;
                        }
                    }
                }
            }
        }
    }

    public ObjectGuid GetGUID()
    {
        return _buildGUID;
    }

    private void UpdateCreatureAndGo()
    {
        foreach (var guid in _creatureTopList[_wg.GetDefenderTeam()])
        {
            var creature = _wg.GetCreature(guid);

            if (creature)
                _wg.HideNpc(creature);
        }

        foreach (var guid in _creatureTopList[_wg.GetAttackerTeam()])
        {
            var creature = _wg.GetCreature(guid);

            if (creature)
                _wg.ShowNpc(creature, true);
        }

        foreach (var guid in _creatureBottomList[_wg.GetDefenderTeam()])
        {
            var creature = _wg.GetCreature(guid);

            if (creature)
                _wg.HideNpc(creature);
        }

        foreach (var guid in _creatureBottomList[_wg.GetAttackerTeam()])
        {
            var creature = _wg.GetCreature(guid);

            if (creature)
                _wg.ShowNpc(creature, true);
        }

        foreach (var guid in _gameObjectList[_wg.GetDefenderTeam()])
        {
            var obj = _wg.GetGameObject(guid);

            if (obj)
                obj.SetRespawnTime(Time.DAY);
        }

        foreach (var guid in _gameObjectList[_wg.GetAttackerTeam()])
        {
            var obj = _wg.GetGameObject(guid);

            if (obj)
                obj.SetRespawnTime(0);
        }
    }
}

internal class WgWorkshop
{
    private readonly StaticWintergraspWorkshopInfo _staticInfo;

    private readonly BattlefieldWg _wg; // Pointer to wintergrasp

    //ObjectGuid _buildGUID;
    private WgGameObjectState _state; // For worldstate
    private uint _teamControl;        // Team witch control the workshop

    public WgWorkshop(BattlefieldWg wg, byte type)
    {
        _wg = wg;
        _state = WgGameObjectState.None;
        _teamControl = TeamIds.Neutral;
        _staticInfo = WgConst.WorkshopData[type];
    }

    public byte GetId()
    {
        return _staticInfo.WorkshopId;
    }

    public void GiveControlTo(uint teamId, bool init)
    {
        switch (teamId)
        {
            case TeamIds.Neutral:
            {
                // Send warning message to all player to inform a faction attack to a workshop
                // alliance / horde attacking a workshop
                _wg.SendWarning(_teamControl != 0 ? _staticInfo.HordeAttackTextId : _staticInfo.AllianceAttackTextId);

                break;
            }
            case TeamIds.Alliance:
            {
                // Updating worldstate
                _state = WgGameObjectState.AllianceIntact;
                Global.WorldStateMgr.SetValueAndSaveInDb(_staticInfo.WorldStateId, (int)_state, false, _wg.GetMap());

                // Warning message
                if (!init)
                    _wg.SendWarning(_staticInfo.AllianceCaptureTextId); // workshop taken - alliance

                // Found associate graveyard and update it
                if (_staticInfo.WorkshopId < WgWorkshopIds.KEEP_WEST)
                {
                    var gy = _wg.GetGraveyardById(_staticInfo.WorkshopId);

                    gy?.GiveControlTo(TeamIds.Alliance);
                }

                _teamControl = teamId;

                break;
            }
            case TeamIds.Horde:
            {
                // Update worldstate
                _state = WgGameObjectState.HordeIntact;
                Global.WorldStateMgr.SetValueAndSaveInDb(_staticInfo.WorldStateId, (int)_state, false, _wg.GetMap());

                // Warning message
                if (!init)
                    _wg.SendWarning(_staticInfo.HordeCaptureTextId); // workshop taken - horde

                // Update graveyard control
                if (_staticInfo.WorkshopId < WgWorkshopIds.KEEP_WEST)
                {
                    var gy = _wg.GetGraveyardById(_staticInfo.WorkshopId);

                    gy?.GiveControlTo(TeamIds.Horde);
                }

                _teamControl = teamId;

                break;
            }
        }

        if (!init)
            _wg.UpdateCounterVehicle(false);
    }

    public void UpdateGraveyardAndWorkshop()
    {
        if (_staticInfo.WorkshopId < WgWorkshopIds.NE)
            GiveControlTo(_wg.GetAttackerTeam(), true);
        else
            GiveControlTo(_wg.GetDefenderTeam(), true);
    }

    public uint GetTeamControl()
    {
        return _teamControl;
    }
}

internal class WintergraspCapturePoint : BfCapturePoint
{
    protected WgWorkshop Workshop;

    public WintergraspCapturePoint(BattlefieldWg battlefield, uint teamInControl)
        : base(battlefield)
    {
        m_Bf = battlefield;
        m_team = teamInControl;
    }

    public void LinkToWorkshop(WgWorkshop workshop)
    {
        Workshop = workshop;
    }

    public override void ChangeTeam(uint oldteam)
    {
        Workshop.GiveControlTo(m_team, false);
    }

    private uint GetTeam()
    {
        return m_team;
    }
}

internal class BfGraveyardWg : BfGraveyard
{
    protected int GossipTextId;

    public BfGraveyardWg(BattlefieldWg battlefield)
        : base(battlefield)
    {
        m_Bf = battlefield;
        GossipTextId = 0;
    }

    public void SetTextId(int textid)
    {
        GossipTextId = textid;
    }

    private int GetTextId()
    {
        return GossipTextId;
    }
}

[Script]
internal class BattlefieldWintergrasp : ScriptObjectAutoAddDBBound, IBattlefieldGetBattlefield
{
    public BattlefieldWintergrasp() : base("battlefield_wg") { }

    public BattleField GetBattlefield(Map map)
    {
        return new BattlefieldWg(map);
    }
}

[Script]
internal class NPCWgGivePromotionCredit : ScriptedAI
{
    public NPCWgGivePromotionCredit(Creature creature) : base(creature) { }

    public override void JustDied(Unit killer)
    {
        if (!killer ||
            !killer.IsPlayer)
            return;

        var wintergrasp = (BattlefieldWg)Global.BattleFieldMgr.GetBattlefieldByBattleId(killer.Map, BattlefieldIds.WG);

        if (wintergrasp == null)
            return;

        wintergrasp.HandlePromotion(killer.AsPlayer, Me);
    }
}