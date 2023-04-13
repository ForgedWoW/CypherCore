// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.BattleGrounds.Zones;

public class BgStrandOfAncients : Battleground
{
    private readonly Dictionary<uint /*id*/, uint /*timer*/> _demoliserRespawnList = new();

    // Status of each gate (Destroy/Damage/Intact)
    private readonly SaGateState[] _gateStatus = new SaGateState[SaMiscConst.Gates.Length];

    // Team witch conntrol each graveyard
    private readonly int[] _graveyardStatus = new int[SaGraveyards.MAX];

    // Score of each round
    private readonly SaRoundScore[] _roundScores = new SaRoundScore[2];

    /// Id of attacker team
    private int _attackers;

    // Max time of round
    private uint _endRoundTimer;

    // for know if second round has been init
    private bool _initSecondRound;

    // For know if boats has start moving or not yet
    private bool _shipsStarted;

    // for know if warning about second round start has been sent
    private bool _signaledRoundTwo;

    // for know if warning about second round start has been sent
    private bool _signaledRoundTwoHalfMin;

    // Statu of battle (Start or not, and what round)
    private SaStatus _status;

    // used for know we are in timer phase or not (used for worldstate update)
    private bool _timerEnabled;

    // Totale elapsed time of current round
    private uint _totalTime;

    // 5secs before starting the 1min countdown for second round
    private uint _updateWaitTimer;

    public BgStrandOfAncients(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
    {
        StartMessageIds[BattlegroundConst.EventIdFourth] = 0;

        BgObjects = new ObjectGuid[SaObjectTypes.MAX_OBJ];
        BgCreatures = new ObjectGuid[SaCreatureTypes.MAX + SaGraveyards.MAX];
        _timerEnabled = false;
        _updateWaitTimer = 0;
        _signaledRoundTwo = false;
        _signaledRoundTwoHalfMin = false;
        _initSecondRound = false;
        _attackers = TeamIds.Alliance;
        _totalTime = 0;
        _endRoundTimer = 0;
        _shipsStarted = false;
        _status = SaStatus.NotStarted;

        for (byte i = 0; i < _gateStatus.Length; ++i)
            _gateStatus[i] = SaGateState.HordeGateOk;

        for (byte i = 0; i < 2; i++)
        {
            _roundScores[i].Winner = TeamIds.Alliance;
            _roundScores[i].Time = 0;
        }
    }

    public override void AddPlayer(Player player)
    {
        var isInBattleground = IsPlayerInBattleground(player.GUID);
        base.AddPlayer(player);

        if (!isInBattleground)
            PlayerScores[player.GUID] = new BattlegroundSaScore(player.GUID, player.GetBgTeam());

        SendTransportInit(player);

        if (!isInBattleground)
            TeleportToEntrancePosition(player);
    }

    public override void DestroyGate(Player player, GameObject go) { }

    public override void EndBattleground(TeamFaction winner)
    {
        // honor reward for winning
        if (winner == TeamFaction.Alliance)
            RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Alliance);
        else if (winner == TeamFaction.Horde)
            RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Horde);

        // complete map_end rewards (even if no team wins)
        RewardHonorToTeam(GetBonusHonorFromKill(2), TeamFaction.Alliance);
        RewardHonorToTeam(GetBonusHonorFromKill(2), TeamFaction.Horde);

        base.EndBattleground(winner);
    }

    public override void EventPlayerClickedOnFlag(Player source, GameObject go)
    {
        switch (go.Entry)
        {
            case 191307:
            case 191308:
                if (CanInteractWithObject(SaObjectTypes.LEFT_FLAG))
                    CaptureGraveyard(SaGraveyards.LEFT_CAPTURABLE_GY, source);

                break;
            case 191305:
            case 191306:
                if (CanInteractWithObject(SaObjectTypes.RIGHT_FLAG))
                    CaptureGraveyard(SaGraveyards.RIGHT_CAPTURABLE_GY, source);

                break;
            case 191310:
            case 191309:
                if (CanInteractWithObject(SaObjectTypes.CENTRAL_FLAG))
                    CaptureGraveyard(SaGraveyards.CENTRAL_CAPTURABLE_GY, source);

                break;
            default:
                return;
        }
    }

    public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
    {
        uint safeloc;

        var teamId = GetTeamIndexByTeamId(GetPlayerTeam(player.GUID));

        if (teamId == _attackers)
            safeloc = SaMiscConst.GYEntries[SaGraveyards.BEACH_GY];
        else
            safeloc = SaMiscConst.GYEntries[SaGraveyards.DEFENDER_LAST_GY];

        var closest = Global.ObjectMgr.GetWorldSafeLoc(safeloc);
        var nearest = player.Location.GetExactDistSq(closest.Loc);

        for (byte i = SaGraveyards.RIGHT_CAPTURABLE_GY; i < SaGraveyards.MAX; i++)
        {
            if (_graveyardStatus[i] != teamId)
                continue;

            var ret = Global.ObjectMgr.GetWorldSafeLoc(SaMiscConst.GYEntries[i]);
            var dist = player.Location.GetExactDistSq(ret.Loc);

            if (dist < nearest)
            {
                closest = ret;
                nearest = dist;
            }
        }

        return closest;
    }

    public override void HandleAreaTrigger(Player source, uint trigger, bool entered)
    {
        // this is wrong way to implement these things. On official it done by gameobject spell cast.
        if (GetStatus() != BattlegroundStatus.InProgress)
            return;
    }

    public override void HandleKillUnit(Creature creature, Player killer)
    {
        if (creature.Entry == SaCreatureIds.DEMOLISHER)
        {
            UpdatePlayerScore(killer, ScoreType.DestroyedDemolisher, 1);
            var worldStateId = _attackers == TeamIds.Horde ? SaWorldStateIds.DESTROYED_HORDE_VEHICLES : SaWorldStateIds.DESTROYED_ALLIANCE_VEHICLES;
            var currentDestroyedVehicles = Global.WorldStateMgr.GetValue((int)worldStateId, GetBgMap());
            UpdateWorldState(worldStateId, currentDestroyedVehicles + 1);
        }
    }

    public override bool IsSpellAllowed(uint spellId, Player player)
    {
        switch (spellId)
        {
            case SaSpellIds.ALLIANCE_CONTROL_PHASE_SHIFT:
                return _attackers == TeamIds.Horde;
            case SaSpellIds.HORDE_CONTROL_PHASE_SHIFT:
                return _attackers == TeamIds.Alliance;
            case BattlegroundConst.SpellPreparation:
                return _status is SaStatus.Warmup or SaStatus.SecondWarmup;
            
        }

        return true;
    }

    public override void PostUpdateImpl(uint diff)
    {
        if (_initSecondRound)
        {
            if (_updateWaitTimer < diff)
            {
                if (!_signaledRoundTwo)
                {
                    _signaledRoundTwo = true;
                    _initSecondRound = false;
                    SendBroadcastText(SaBroadcastTexts.ROUND_TWO_START_ONE_MINUTE, ChatMsg.BgSystemNeutral);
                }
            }
            else
            {
                _updateWaitTimer -= diff;

                return;
            }
        }

        _totalTime += diff;

        if (_status == SaStatus.Warmup)
        {
            _endRoundTimer = SaTimers.ROUND_LENGTH;
            UpdateWorldState(SaWorldStateIds.TIMER, (int)(GameTime.CurrentTime + _endRoundTimer));

            if (_totalTime >= SaTimers.WARMUP_LENGTH)
            {
                var c = GetBGCreature(SaCreatureTypes.KANRETHAD);

                if (c)
                    SendChatMessage(c, SaTextIds.ROUND_STARTED);

                _totalTime = 0;
                ToggleTimer();
                DemolisherStartState(false);
                _status = SaStatus.RoundOne;
                TriggerGameEvent(_attackers == TeamIds.Alliance ? 23748 : 21702u);
            }

            if (_totalTime >= SaTimers.BOAT_START)
                StartShips();

            return;
        }
        else if (_status == SaStatus.SecondWarmup)
        {
            if (_roundScores[0].Time < SaTimers.ROUND_LENGTH)
                _endRoundTimer = _roundScores[0].Time;
            else
                _endRoundTimer = SaTimers.ROUND_LENGTH;

            UpdateWorldState(SaWorldStateIds.TIMER, (int)(GameTime.CurrentTime + _endRoundTimer));

            if (_totalTime >= 60000)
            {
                var c = GetBGCreature(SaCreatureTypes.KANRETHAD);

                if (c)
                    SendChatMessage(c, SaTextIds.ROUND_STARTED);

                _totalTime = 0;
                ToggleTimer();
                DemolisherStartState(false);
                _status = SaStatus.RoundTwo;
                TriggerGameEvent(_attackers == TeamIds.Alliance ? 23748 : 21702u);
                // status was set to STATUS_WAIT_JOIN manually for Preparation, set it back now
                SetStatus(BattlegroundStatus.InProgress);

                foreach (var pair in GetPlayers())
                {
                    var p = Global.ObjAccessor.FindPlayer(pair.Key);

                    if (p)
                        p.RemoveAura(BattlegroundConst.SpellPreparation);
                }
            }

            if (_totalTime >= 30000)
                if (!_signaledRoundTwoHalfMin)
                {
                    _signaledRoundTwoHalfMin = true;
                    SendBroadcastText(SaBroadcastTexts.ROUND_TWO_START_HALF_MINUTE, ChatMsg.BgSystemNeutral);
                }

            StartShips();

            return;
        }
        else if (GetStatus() == BattlegroundStatus.InProgress)
        {
            if (_status == SaStatus.RoundOne)
            {
                if (_totalTime >= SaTimers.ROUND_LENGTH)
                {
                    CastSpellOnTeam(SaSpellIds.END_OF_ROUND, TeamFaction.Alliance);
                    CastSpellOnTeam(SaSpellIds.END_OF_ROUND, TeamFaction.Horde);
                    _roundScores[0].Winner = (uint)_attackers;
                    _roundScores[0].Time = SaTimers.ROUND_LENGTH;
                    _totalTime = 0;
                    _status = SaStatus.SecondWarmup;
                    _attackers = _attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance;
                    _updateWaitTimer = 5000;
                    _signaledRoundTwo = false;
                    _signaledRoundTwoHalfMin = false;
                    _initSecondRound = true;
                    ToggleTimer();
                    ResetObjs();
                    GetBgMap().UpdateAreaDependentAuras();

                    return;
                }
            }
            else if (_status == SaStatus.RoundTwo)
            {
                if (_totalTime >= _endRoundTimer)
                {
                    CastSpellOnTeam(SaSpellIds.END_OF_ROUND, TeamFaction.Alliance);
                    CastSpellOnTeam(SaSpellIds.END_OF_ROUND, TeamFaction.Horde);
                    _roundScores[1].Time = SaTimers.ROUND_LENGTH;
                    _roundScores[1].Winner = (uint)(_attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance);

                    if (_roundScores[0].Time == _roundScores[1].Time)
                        EndBattleground(0);
                    else if (_roundScores[0].Time < _roundScores[1].Time)
                        EndBattleground(_roundScores[0].Winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
                    else
                        EndBattleground(_roundScores[1].Winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);

                    return;
                }
            }

            if (_status is SaStatus.RoundOne or SaStatus.RoundTwo)
                UpdateDemolisherSpawns();
        }
    }

    public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker = null)
    {
        var go = obj.AsGameObject;

        if (go)
            switch (go.GoType)
            {
                case GameObjectTypes.Goober:
                    if (invoker)
                        if (eventId == (uint)SaEventIds.BGSaEventTitanRelicActivated)
                            TitanRelicActivated(invoker.AsPlayer);

                    break;
                case GameObjectTypes.DestructibleBuilding:
                {
                    var gate = GetGate(obj.Entry);

                    if (gate != null)
                    {
                        var gateId = gate.GateId;

                        // damaged
                        if (eventId == go.Template.DestructibleBuilding.DamagedEvent)
                        {
                            _gateStatus[gateId] = _attackers == TeamIds.Horde ? SaGateState.AllianceGateDamaged : SaGateState.HordeGateDamaged;

                            var c = obj.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                            if (c)
                                SendChatMessage(c, (byte)gate.DamagedText, invoker);

                            PlaySoundToAll(_attackers == TeamIds.Alliance ? SaSoundIds.WALL_ATTACKED_ALLIANCE : SaSoundIds.WALL_ATTACKED_HORDE);
                        }
                        // destroyed
                        else if (eventId == go.Template.DestructibleBuilding.DestroyedEvent)
                        {
                            _gateStatus[gate.GateId] = _attackers == TeamIds.Horde ? SaGateState.AllianceGateDestroyed : SaGateState.HordeGateDestroyed;

                            if (gateId < 5)
                                DelObject((int)gateId + 14);

                            var c = obj.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                            if (c)
                                SendChatMessage(c, (byte)gate.DestroyedText, invoker);

                            PlaySoundToAll(_attackers == TeamIds.Alliance ? SaSoundIds.WALL_DESTROYED_ALLIANCE : SaSoundIds.WALL_DESTROYED_HORDE);

                            var rewardHonor = true;

                            switch (gateId)
                            {
                                case SaObjectTypes.GREEN_GATE:
                                    if (IsGateDestroyed(SaObjectTypes.BLUE_GATE))
                                        rewardHonor = false;

                                    break;
                                case SaObjectTypes.BLUE_GATE:
                                    if (IsGateDestroyed(SaObjectTypes.GREEN_GATE))
                                        rewardHonor = false;

                                    break;
                                case SaObjectTypes.RED_GATE:
                                    if (IsGateDestroyed(SaObjectTypes.PURPLE_GATE))
                                        rewardHonor = false;

                                    break;
                                case SaObjectTypes.PURPLE_GATE:
                                    if (IsGateDestroyed(SaObjectTypes.RED_GATE))
                                        rewardHonor = false;

                                    break;
                                
                            }

                            if (invoker)
                            {
                                var unit = invoker.AsUnit;

                                if (unit)
                                {
                                    var player = unit.CharmerOrOwnerPlayerOrPlayerItself;

                                    if (player)
                                    {
                                        UpdatePlayerScore(player, ScoreType.DestroyedWall, 1);

                                        if (rewardHonor)
                                            UpdatePlayerScore(player, ScoreType.BonusHonor, GetBonusHonorFromKill(1));
                                    }
                                }
                            }

                            UpdateObjectInteractionFlags();
                        }
                        else
                        {
                            break;
                        }

                        UpdateWorldState(gate.WorldState, (int)_gateStatus[gateId]);
                    }

                    break;
                }
                
            }
    }

    public override void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team) { }

    public override void Reset()
    {
        _totalTime = 0;
        _attackers = RandomHelper.URand(0, 1) != 0 ? TeamIds.Alliance : TeamIds.Horde;

        for (byte i = 0; i <= 5; i++)
            _gateStatus[i] = SaGateState.HordeGateOk;

        _shipsStarted = false;
        _status = SaStatus.Warmup;
    }

    public override bool SetupBattleground()
    {
        return ResetObjs();
    }

    public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
    {
        if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
            return false;

        switch (type)
        {
            case ScoreType.DestroyedDemolisher:
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)SaObjectives.DemolishersDestroyed);

                break;
            case ScoreType.DestroyedWall:
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)SaObjectives.GatesDestroyed);

                break;
            
        }

        return true;
    }

    private bool CanInteractWithObject(uint objectId)
    {
        switch (objectId)
        {
            case SaObjectTypes.TITAN_RELIC:
                if (!IsGateDestroyed(SaObjectTypes.ANCIENT_GATE) || !IsGateDestroyed(SaObjectTypes.YELLOW_GATE))
                    return false;

                goto case SaObjectTypes.CENTRAL_FLAG;
            case SaObjectTypes.CENTRAL_FLAG:
                if (!IsGateDestroyed(SaObjectTypes.RED_GATE) && !IsGateDestroyed(SaObjectTypes.PURPLE_GATE))
                    return false;

                goto case SaObjectTypes.LEFT_FLAG;
            case SaObjectTypes.LEFT_FLAG:
            case SaObjectTypes.RIGHT_FLAG:
                if (!IsGateDestroyed(SaObjectTypes.GREEN_GATE) && !IsGateDestroyed(SaObjectTypes.BLUE_GATE))
                    return false;

                break;
            default:
                //ABORT();
                break;
        }

        return true;
    }

    private void CaptureGraveyard(int i, Player source)
    {
        if (_graveyardStatus[i] == _attackers)
            return;

        DelCreature(SaCreatureTypes.MAX + i);
        var teamId = GetTeamIndexByTeamId(GetPlayerTeam(source.GUID));
        _graveyardStatus[i] = teamId;
        var sg = Global.ObjectMgr.GetWorldSafeLoc(SaMiscConst.GYEntries[i]);

        if (sg == null)
        {
            Log.Logger.Error($"CaptureGraveyard: non-existant GY entry: {SaMiscConst.GYEntries[i]}");

            return;
        }

        AddSpiritGuide(i + SaCreatureTypes.MAX, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, SaMiscConst.GYOrientation[i], _graveyardStatus[i]);

        uint npc;
        int flag;

        switch (i)
        {
            case SaGraveyards.LEFT_CAPTURABLE_GY:
            {
                flag = SaObjectTypes.LEFT_FLAG;
                DelObject(flag);

                AddObject(flag,
                          SaMiscConst.ObjEntries[flag] - (teamId == TeamIds.Alliance ? 0 : 1u),
                          SaMiscConst.ObjSpawnlocs[flag],
                          0,
                          0,
                          0,
                          0,
                          BattlegroundConst.RespawnOneDay);

                npc = SaCreatureTypes.RIGSPARK;
                var rigspark = AddCreature(SaMiscConst.NpcEntries[npc], (int)npc, SaMiscConst.NpcSpawnlocs[npc], _attackers);

                if (rigspark)
                    rigspark.AI.Talk(SaTextIds.SPARKLIGHT_RIGSPARK_SPAWN);

                for (byte j = SaCreatureTypes.DEMOLISHER7; j <= SaCreatureTypes.DEMOLISHER8; j++)
                {
                    AddCreature(SaMiscConst.NpcEntries[j], j, SaMiscConst.NpcSpawnlocs[j], _attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance, 600);
                    var dem = GetBGCreature(j);

                    if (dem)
                        dem.Faction = SaMiscConst.Factions[_attackers];
                }

                UpdateWorldState(SaWorldStateIds.LEFT_GY_ALLIANCE, _graveyardStatus[i] == TeamIds.Alliance ? 1 : 0);
                UpdateWorldState(SaWorldStateIds.LEFT_GY_HORDE, _graveyardStatus[i] == TeamIds.Horde ? 1 : 0);

                var c = source.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                if (c)
                    SendChatMessage(c, teamId == TeamIds.Alliance ? SaTextIds.WEST_GRAVEYARD_CAPTURED_A : SaTextIds.WEST_GRAVEYARD_CAPTURED_H, source);
            }

                break;
            case SaGraveyards.RIGHT_CAPTURABLE_GY:
            {
                flag = SaObjectTypes.RIGHT_FLAG;
                DelObject(flag);

                AddObject(flag,
                          SaMiscConst.ObjEntries[flag] - (teamId == TeamIds.Alliance ? 0 : 1u),
                          SaMiscConst.ObjSpawnlocs[flag],
                          0,
                          0,
                          0,
                          0,
                          BattlegroundConst.RespawnOneDay);

                npc = SaCreatureTypes.SPARKLIGHT;
                var sparklight = AddCreature(SaMiscConst.NpcEntries[npc], (int)npc, SaMiscConst.NpcSpawnlocs[npc], _attackers);

                if (sparklight)
                    sparklight.AI.Talk(SaTextIds.SPARKLIGHT_RIGSPARK_SPAWN);

                for (byte j = SaCreatureTypes.DEMOLISHER5; j <= SaCreatureTypes.DEMOLISHER6; j++)
                {
                    AddCreature(SaMiscConst.NpcEntries[j], j, SaMiscConst.NpcSpawnlocs[j], _attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance, 600);

                    var dem = GetBGCreature(j);

                    if (dem)
                        dem.Faction = SaMiscConst.Factions[_attackers];
                }

                UpdateWorldState(SaWorldStateIds.RIGHT_GY_ALLIANCE, _graveyardStatus[i] == TeamIds.Alliance ? 1 : 0);
                UpdateWorldState(SaWorldStateIds.RIGHT_GY_HORDE, _graveyardStatus[i] == TeamIds.Horde ? 1 : 0);

                var c = source.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                if (c)
                    SendChatMessage(c, teamId == TeamIds.Alliance ? SaTextIds.EAST_GRAVEYARD_CAPTURED_A : SaTextIds.EAST_GRAVEYARD_CAPTURED_H, source);
            }

                break;
            case SaGraveyards.CENTRAL_CAPTURABLE_GY:
            {
                flag = SaObjectTypes.CENTRAL_FLAG;
                DelObject(flag);

                AddObject(flag,
                          SaMiscConst.ObjEntries[flag] - (teamId == TeamIds.Alliance ? 0 : 1u),
                          SaMiscConst.ObjSpawnlocs[flag],
                          0,
                          0,
                          0,
                          0,
                          BattlegroundConst.RespawnOneDay);

                UpdateWorldState(SaWorldStateIds.CENTER_GY_ALLIANCE, _graveyardStatus[i] == TeamIds.Alliance ? 1 : 0);
                UpdateWorldState(SaWorldStateIds.CENTER_GY_HORDE, _graveyardStatus[i] == TeamIds.Horde ? 1 : 0);

                var c = source.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                if (c)
                    SendChatMessage(c, teamId == TeamIds.Alliance ? SaTextIds.SOUTH_GRAVEYARD_CAPTURED_A : SaTextIds.SOUTH_GRAVEYARD_CAPTURED_H, source);
            }

                break;
            default:
                //ABORT();
                break;
        }
    }

    private void DemolisherStartState(bool start)
    {
        if (BgCreatures[0].IsEmpty)
            return;

        // set flags only for the demolishers on the beach, factory ones dont need it
        for (byte i = SaCreatureTypes.DEMOLISHER1; i <= SaCreatureTypes.DEMOLISHER4; i++)
        {
            var dem = GetBGCreature(i);

            if (dem)
            {
                if (start)
                    dem.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);
                else
                    dem.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);
            }
        }
    }

    private SaGateInfo GetGate(uint entry)
    {
        foreach (var gate in SaMiscConst.Gates)
            if (gate.GameObjectId == entry)
                return gate;

        return null;
    }

    private bool IsGateDestroyed(uint gateId)
    {
        return _gateStatus[gateId] == SaGateState.AllianceGateDestroyed || _gateStatus[gateId] == SaGateState.HordeGateDestroyed;
    }

    private void OverrideGunFaction()
    {
        if (BgCreatures[0].IsEmpty)
            return;

        for (byte i = SaCreatureTypes.GUN1; i <= SaCreatureTypes.GUN10; i++)
        {
            var gun = GetBGCreature(i);

            if (gun)
                gun.Faction = SaMiscConst.Factions[_attackers != 0 ? TeamIds.Alliance : TeamIds.Horde];
        }

        for (byte i = SaCreatureTypes.DEMOLISHER1; i <= SaCreatureTypes.DEMOLISHER4; i++)
        {
            var dem = GetBGCreature(i);

            if (dem)
                dem.Faction = SaMiscConst.Factions[_attackers];
        }
    }

    private bool ResetObjs()
    {
        foreach (var pair in GetPlayers())
        {
            var player = Global.ObjAccessor.FindPlayer(pair.Key);

            if (player)
                SendTransportsRemove(player);
        }

        var atF = SaMiscConst.Factions[_attackers];
        var defF = SaMiscConst.Factions[_attackers != 0 ? TeamIds.Alliance : TeamIds.Horde];

        for (byte i = 0; i < SaObjectTypes.MAX_OBJ; i++)
            DelObject(i);

        for (byte i = 0; i < SaCreatureTypes.MAX; i++)
            DelCreature(i);

        for (byte i = SaCreatureTypes.MAX; i < SaCreatureTypes.MAX + SaGraveyards.MAX; i++)
            DelCreature(i);

        for (byte i = 0; i < _gateStatus.Length; ++i)
            _gateStatus[i] = _attackers == TeamIds.Horde ? SaGateState.AllianceGateOk : SaGateState.HordeGateOk;

        if (!AddCreature(SaMiscConst.NpcEntries[SaCreatureTypes.KANRETHAD], SaCreatureTypes.KANRETHAD, SaMiscConst.NpcSpawnlocs[SaCreatureTypes.KANRETHAD]))
        {
            Log.Logger.Error($"SOTA: couldn't spawn Kanrethad, aborted. Entry: {SaMiscConst.NpcEntries[SaCreatureTypes.KANRETHAD]}");

            return false;
        }

        for (byte i = 0; i <= SaObjectTypes.PORTAL_DEFFENDER_RED; i++)
            if (!AddObject(i, SaMiscConst.ObjEntries[i], SaMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
            {
                Log.Logger.Error($"SOTA: couldn't spawn BG_SA_PORTAL_DEFFENDER_RED, Entry: {SaMiscConst.ObjEntries[i]}");

                continue;
            }

        for (var i = SaObjectTypes.BOAT_ONE; i <= SaObjectTypes.BOAT_TWO; i++)
        {
            uint boatid = 0;

            switch (i)
            {
                case SaObjectTypes.BOAT_ONE:
                    boatid = _attackers != 0 ? SaGameObjectIds.BOAT_ONE_H : SaGameObjectIds.BOAT_ONE_A;

                    break;
                case SaObjectTypes.BOAT_TWO:
                    boatid = _attackers != 0 ? SaGameObjectIds.BOAT_TWO_H : SaGameObjectIds.BOAT_TWO_A;

                    break;
                
            }

            if (!AddObject(i,
                           boatid,
                           SaMiscConst.ObjSpawnlocs[i].X,
                           SaMiscConst.ObjSpawnlocs[i].Y,
                           SaMiscConst.ObjSpawnlocs[i].Z + (_attackers != 0 ? -3.750f : 0),
                           SaMiscConst.ObjSpawnlocs[i].Orientation,
                           0,
                           0,
                           0,
                           0,
                           BattlegroundConst.RespawnOneDay))
            {
                Log.Logger.Error($"SOTA: couldn't spawn one of the BG_SA_BOAT, Entry: {boatid}");

                continue;
            }
        }

        for (byte i = SaObjectTypes.SIGIL1; i <= SaObjectTypes.LEFT_FLAGPOLE; i++)
            if (!AddObject(i, SaMiscConst.ObjEntries[i], SaMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
            {
                Log.Logger.Error($"SOTA: couldn't spawn Sigil, Entry: {SaMiscConst.ObjEntries[i]}");

                continue;
            }

        // MAD props for Kiper for discovering those values - 4 hours of his work.
        GetBGObject(SaObjectTypes.BOAT_ONE).SetParentRotation(new Quaternion(0.0f, 0.0f, 1.0f, 0.0002f));
        GetBGObject(SaObjectTypes.BOAT_TWO).SetParentRotation(new Quaternion(0.0f, 0.0f, 1.0f, 0.00001f));
        SpawnBGObject(SaObjectTypes.BOAT_ONE, BattlegroundConst.RespawnImmediately);
        SpawnBGObject(SaObjectTypes.BOAT_TWO, BattlegroundConst.RespawnImmediately);

        //Cannons and demolishers - NPCs are spawned
        //By capturing GYs.
        for (byte i = 0; i < SaCreatureTypes.DEMOLISHER5; i++)
            if (!AddCreature(SaMiscConst.NpcEntries[i], i, SaMiscConst.NpcSpawnlocs[i], _attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance, 600))
            {
                Log.Logger.Error($"SOTA: couldn't spawn Cannon or demolisher, Entry: {SaMiscConst.NpcEntries[i]}, Attackers: {(_attackers == TeamIds.Alliance ? "Horde(1)" : "Alliance(0)")}");

                continue;
            }

        OverrideGunFaction();
        DemolisherStartState(true);

        for (byte i = 0; i <= SaObjectTypes.PORTAL_DEFFENDER_RED; i++)
        {
            SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
            GetBGObject(i).Faction = defF;
        }

        GetBGObject(SaObjectTypes.TITAN_RELIC).Faction = atF;
        GetBGObject(SaObjectTypes.TITAN_RELIC).Refresh();

        _totalTime = 0;
        _shipsStarted = false;

        //Graveyards
        for (byte i = 0; i < SaGraveyards.MAX; i++)
        {
            var sg = Global.ObjectMgr.GetWorldSafeLoc(SaMiscConst.GYEntries[i]);

            if (sg == null)
            {
                Log.Logger.Error($"SOTA: Can't find GY entry {SaMiscConst.GYEntries[i]}");

                return false;
            }

            if (i == SaGraveyards.BEACH_GY)
            {
                _graveyardStatus[i] = _attackers;
                AddSpiritGuide(i + SaCreatureTypes.MAX, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, SaMiscConst.GYOrientation[i], _attackers);
            }
            else
            {
                _graveyardStatus[i] = _attackers == TeamIds.Horde ? TeamIds.Alliance : TeamIds.Horde;

                if (!AddSpiritGuide(i + SaCreatureTypes.MAX, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, SaMiscConst.GYOrientation[i], _attackers == TeamIds.Horde ? TeamIds.Alliance : TeamIds.Horde))
                    Log.Logger.Error($"SOTA: couldn't spawn GY: {i}");
            }
        }

        //GY capture points
        for (byte i = SaObjectTypes.CENTRAL_FLAG; i <= SaObjectTypes.LEFT_FLAG; i++)
        {
            if (!AddObject(i, SaMiscConst.ObjEntries[i] - (_attackers == TeamIds.Alliance ? 1u : 0), SaMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
            {
                Log.Logger.Error($"SOTA: couldn't spawn Central Flag Entry: {SaMiscConst.ObjEntries[i] - (_attackers == TeamIds.Alliance ? 1 : 0)}");

                continue;
            }

            GetBGObject(i).Faction = atF;
        }

        UpdateObjectInteractionFlags();

        for (byte i = SaObjectTypes.BOMB; i < SaObjectTypes.MAX_OBJ; i++)
        {
            if (!AddObject(i, SaMiscConst.ObjEntries[SaObjectTypes.BOMB], SaMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
            {
                Log.Logger.Error($"SOTA: couldn't spawn SA Bomb Entry: {SaMiscConst.ObjEntries[SaObjectTypes.BOMB] + i}");

                continue;
            }

            GetBGObject(i).Faction = atF;
        }

        //Player may enter BEFORE we set up BG - lets update his worldstates anyway...
        UpdateWorldState(SaWorldStateIds.RIGHT_GY_HORDE, _graveyardStatus[SaGraveyards.RIGHT_CAPTURABLE_GY] == TeamIds.Horde ? 1 : 0);
        UpdateWorldState(SaWorldStateIds.LEFT_GY_HORDE, _graveyardStatus[SaGraveyards.LEFT_CAPTURABLE_GY] == TeamIds.Horde ? 1 : 0);
        UpdateWorldState(SaWorldStateIds.CENTER_GY_HORDE, _graveyardStatus[SaGraveyards.CENTRAL_CAPTURABLE_GY] == TeamIds.Horde ? 1 : 0);

        UpdateWorldState(SaWorldStateIds.RIGHT_GY_ALLIANCE, _graveyardStatus[SaGraveyards.RIGHT_CAPTURABLE_GY] == TeamIds.Alliance ? 1 : 0);
        UpdateWorldState(SaWorldStateIds.LEFT_GY_ALLIANCE, _graveyardStatus[SaGraveyards.LEFT_CAPTURABLE_GY] == TeamIds.Alliance ? 1 : 0);
        UpdateWorldState(SaWorldStateIds.CENTER_GY_ALLIANCE, _graveyardStatus[SaGraveyards.CENTRAL_CAPTURABLE_GY] == TeamIds.Alliance ? 1 : 0);

        if (_attackers == TeamIds.Alliance)
        {
            UpdateWorldState(SaWorldStateIds.ALLY_ATTACKS, 1);
            UpdateWorldState(SaWorldStateIds.HORDE_ATTACKS, 0);

            UpdateWorldState(SaWorldStateIds.RIGHT_ATT_TOKEN_ALL, 1);
            UpdateWorldState(SaWorldStateIds.LEFT_ATT_TOKEN_ALL, 1);
            UpdateWorldState(SaWorldStateIds.RIGHT_ATT_TOKEN_HRD, 0);
            UpdateWorldState(SaWorldStateIds.LEFT_ATT_TOKEN_HRD, 0);

            UpdateWorldState(SaWorldStateIds.HORDE_DEFENCE_TOKEN, 1);
            UpdateWorldState(SaWorldStateIds.ALLIANCE_DEFENCE_TOKEN, 0);
        }
        else
        {
            UpdateWorldState(SaWorldStateIds.HORDE_ATTACKS, 1);
            UpdateWorldState(SaWorldStateIds.ALLY_ATTACKS, 0);

            UpdateWorldState(SaWorldStateIds.RIGHT_ATT_TOKEN_ALL, 0);
            UpdateWorldState(SaWorldStateIds.LEFT_ATT_TOKEN_ALL, 0);
            UpdateWorldState(SaWorldStateIds.RIGHT_ATT_TOKEN_HRD, 1);
            UpdateWorldState(SaWorldStateIds.LEFT_ATT_TOKEN_HRD, 1);

            UpdateWorldState(SaWorldStateIds.HORDE_DEFENCE_TOKEN, 0);
            UpdateWorldState(SaWorldStateIds.ALLIANCE_DEFENCE_TOKEN, 1);
        }

        UpdateWorldState(SaWorldStateIds.ATTACKER_TEAM, _attackers);
        UpdateWorldState(SaWorldStateIds.PURPLE_GATE, 1);
        UpdateWorldState(SaWorldStateIds.RED_GATE, 1);
        UpdateWorldState(SaWorldStateIds.BLUE_GATE, 1);
        UpdateWorldState(SaWorldStateIds.GREEN_GATE, 1);
        UpdateWorldState(SaWorldStateIds.YELLOW_GATE, 1);
        UpdateWorldState(SaWorldStateIds.ANCIENT_GATE, 1);

        for (var i = SaObjectTypes.BOAT_ONE; i <= SaObjectTypes.BOAT_TWO; i++)
            foreach (var pair in GetPlayers())
            {
                var player = Global.ObjAccessor.FindPlayer(pair.Key);

                if (player)
                    SendTransportInit(player);
            }

        // set status manually so preparation is cast correctly in 2nd round too
        SetStatus(BattlegroundStatus.WaitJoin);

        TeleportPlayers();

        return true;
    }

    private void SendTransportInit(Player player)
    {
        if (!BgObjects[SaObjectTypes.BOAT_ONE].IsEmpty || !BgObjects[SaObjectTypes.BOAT_TWO].IsEmpty)
        {
            UpdateData transData = new(player.Location.MapId);

            if (!BgObjects[SaObjectTypes.BOAT_ONE].IsEmpty)
                GetBGObject(SaObjectTypes.BOAT_ONE).BuildCreateUpdateBlockForPlayer(transData, player);

            if (!BgObjects[SaObjectTypes.BOAT_TWO].IsEmpty)
                GetBGObject(SaObjectTypes.BOAT_TWO).BuildCreateUpdateBlockForPlayer(transData, player);

            transData.BuildPacket(out var packet);
            player.SendPacket(packet);
        }
    }

    private void SendTransportsRemove(Player player)
    {
        if (!BgObjects[SaObjectTypes.BOAT_ONE].IsEmpty || !BgObjects[SaObjectTypes.BOAT_TWO].IsEmpty)
        {
            UpdateData transData = new(player.Location.MapId);

            if (!BgObjects[SaObjectTypes.BOAT_ONE].IsEmpty)
                GetBGObject(SaObjectTypes.BOAT_ONE).BuildOutOfRangeUpdateBlock(transData);

            if (!BgObjects[SaObjectTypes.BOAT_TWO].IsEmpty)
                GetBGObject(SaObjectTypes.BOAT_TWO).BuildOutOfRangeUpdateBlock(transData);

            transData.BuildPacket(out var packet);
            player.SendPacket(packet);
        }
    }

    private void StartShips()
    {
        if (_shipsStarted)
            return;

        GetBGObject(SaObjectTypes.BOAT_ONE).SetGoState(GameObjectState.TransportStopped);
        GetBGObject(SaObjectTypes.BOAT_TWO).SetGoState(GameObjectState.TransportStopped);

        for (var i = SaObjectTypes.BOAT_ONE; i <= SaObjectTypes.BOAT_TWO; i++)
            foreach (var pair in GetPlayers())
            {
                var p = Global.ObjAccessor.FindPlayer(pair.Key);

                if (p)
                {
                    UpdateData data = new(p.Location.MapId);
                    GetBGObject(i).BuildValuesUpdateBlockForPlayer(data, p);

                    data.BuildPacket(out var pkt);
                    p.SendPacket(pkt);
                }
            }

        _shipsStarted = true;
    }

    private void TeleportPlayers()
    {
        foreach (var pair in GetPlayers())
        {
            var player = Global.ObjAccessor.FindPlayer(pair.Key);

            if (player)
            {
                // should remove spirit of redemption
                if (player.HasAuraType(AuraType.SpiritOfRedemption))
                    player.RemoveAurasByType(AuraType.ModShapeshift);

                if (!player.IsAlive)
                {
                    player.ResurrectPlayer(1.0f);
                    player.SpawnCorpseBones();
                }

                player.ResetAllPowers();
                player.CombatStopWithPets(true);

                player.CastSpell(player, BattlegroundConst.SpellPreparation, true);

                TeleportToEntrancePosition(player);
            }
        }
    }

    private void TeleportToEntrancePosition(Player player)
    {
        if (GetTeamIndexByTeamId(GetPlayerTeam(player.GUID)) == _attackers)
        {
            if (!_shipsStarted)
            {
                // player.AddUnitMovementFlag(MOVEMENTFLAG_ONTRANSPORT);

                if (RandomHelper.URand(0, 1) != 0)
                    player.TeleportTo(607, 2682.936f, -830.368f, 15.0f, 2.895f);
                else
                    player.TeleportTo(607, 2577.003f, 980.261f, 15.0f, 0.807f);
            }
            else
            {
                player.TeleportTo(607, 1600.381f, -106.263f, 8.8745f, 3.78f);
            }
        }
        else
        {
            player.TeleportTo(607, 1209.7f, -65.16f, 70.1f, 0.0f);
        }
    }

    /*
    You may ask what the fuck does it do?
    Prevents owner overwriting guns faction with own.
    */
    private void TitanRelicActivated(Player clicker)
    {
        if (!clicker)
            return;

        if (CanInteractWithObject(SaObjectTypes.TITAN_RELIC))
        {
            var clickerTeamId = GetTeamIndexByTeamId(GetPlayerTeam(clicker.GUID));

            if (clickerTeamId == _attackers)
            {
                if (clickerTeamId == TeamIds.Alliance)
                    SendBroadcastText(SaBroadcastTexts.ALLIANCE_CAPTURED_TITAN_PORTAL, ChatMsg.BgSystemNeutral);
                else
                    SendBroadcastText(SaBroadcastTexts.HORDE_CAPTURED_TITAN_PORTAL, ChatMsg.BgSystemNeutral);

                if (_status == SaStatus.RoundOne)
                {
                    _roundScores[0].Winner = (uint)_attackers;
                    _roundScores[0].Time = _totalTime;

                    // Achievement Storm the Beach (1310)
                    foreach (var pair in GetPlayers())
                    {
                        var player = Global.ObjAccessor.FindPlayer(pair.Key);

                        if (player)
                            if (GetTeamIndexByTeamId(GetPlayerTeam(player.GUID)) == _attackers)
                                player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                    }

                    _attackers = _attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance;
                    _status = SaStatus.SecondWarmup;
                    _totalTime = 0;
                    ToggleTimer();

                    var c = GetBGCreature(SaCreatureTypes.KANRETHAD);

                    if (c)
                        SendChatMessage(c, SaTextIds.ROUND1_FINISHED);

                    _updateWaitTimer = 5000;
                    _signaledRoundTwo = false;
                    _signaledRoundTwoHalfMin = false;
                    _initSecondRound = true;
                    ResetObjs();
                    GetBgMap().UpdateAreaDependentAuras();
                    CastSpellOnTeam(SaSpellIds.END_OF_ROUND, TeamFaction.Alliance);
                    CastSpellOnTeam(SaSpellIds.END_OF_ROUND, TeamFaction.Horde);
                }
                else if (_status == SaStatus.RoundTwo)
                {
                    _roundScores[1].Winner = (uint)_attackers;
                    _roundScores[1].Time = _totalTime;
                    ToggleTimer();

                    // Achievement Storm the Beach (1310)
                    foreach (var pair in GetPlayers())
                    {
                        var player = Global.ObjAccessor.FindPlayer(pair.Key);

                        if (player)
                            if (GetTeamIndexByTeamId(GetPlayerTeam(player.GUID)) == _attackers && _roundScores[1].Winner == _attackers)
                                player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                    }

                    if (_roundScores[0].Time == _roundScores[1].Time)
                        EndBattleground(0);
                    else if (_roundScores[0].Time < _roundScores[1].Time)
                        EndBattleground(_roundScores[0].Winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
                    else
                        EndBattleground(_roundScores[1].Winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
                }
            }
        }
    }

    private void ToggleTimer()
    {
        _timerEnabled = !_timerEnabled;
        UpdateWorldState(SaWorldStateIds.ENABLE_TIMER, _timerEnabled ? 1 : 0);
    }

    private void UpdateDemolisherSpawns()
    {
        for (byte i = SaCreatureTypes.DEMOLISHER1; i <= SaCreatureTypes.DEMOLISHER8; i++)
            if (!BgCreatures[i].IsEmpty)
            {
                var demolisher = GetBGCreature(i);

                if (demolisher)
                    if (demolisher.IsDead)
                    {
                        // Demolisher is not in list
                        if (!_demoliserRespawnList.ContainsKey(i))
                        {
                            _demoliserRespawnList[i] = GameTime.CurrentTimeMS + 30000;
                        }
                        else
                        {
                            if (_demoliserRespawnList[i] < GameTime.CurrentTimeMS)
                            {
                                demolisher.Location.Relocate(SaMiscConst.NpcSpawnlocs[i]);
                                demolisher.Respawn();
                                _demoliserRespawnList.Remove(i);
                            }
                        }
                    }
            }
    }

    private void UpdateObjectInteractionFlags(uint objectId)
    {
        var go = GetBGObject((int)objectId);

        if (go)
        {
            if (CanInteractWithObject(objectId))
                go.RemoveFlag(GameObjectFlags.NotSelectable);
            else
                go.SetFlag(GameObjectFlags.NotSelectable);
        }
    }

    private void UpdateObjectInteractionFlags()
    {
        for (byte i = SaObjectTypes.CENTRAL_FLAG; i <= SaObjectTypes.LEFT_FLAG; ++i)
            UpdateObjectInteractionFlags(i);

        UpdateObjectInteractionFlags(SaObjectTypes.TITAN_RELIC);
    }
}