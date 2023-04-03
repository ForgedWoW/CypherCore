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
    private readonly Dictionary<uint /*id*/, uint /*timer*/> DemoliserRespawnList = new();

    // Status of each gate (Destroy/Damage/Intact)
    private readonly SAGateState[] GateStatus = new SAGateState[SAMiscConst.Gates.Length];

    // Team witch conntrol each graveyard
    private readonly int[] GraveyardStatus = new int[SAGraveyards.Max];

    // Score of each round
    private readonly SARoundScore[] RoundScores = new SARoundScore[2];

    /// Id of attacker team
    private int Attackers;

    // Max time of round
    private uint EndRoundTimer;

    // for know if second round has been init
    private bool InitSecondRound;

    // For know if boats has start moving or not yet
    private bool ShipsStarted;

    // for know if warning about second round start has been sent
    private bool SignaledRoundTwo;

    // for know if warning about second round start has been sent
    private bool SignaledRoundTwoHalfMin;

    // Statu of battle (Start or not, and what round)
    private SAStatus Status;

    // used for know we are in timer phase or not (used for worldstate update)
    private bool TimerEnabled;

    // Totale elapsed time of current round
    private uint TotalTime;

    // 5secs before starting the 1min countdown for second round
    private uint UpdateWaitTimer;

    public BgStrandOfAncients(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
    {
        StartMessageIds[BattlegroundConst.EventIdFourth] = 0;

        BgObjects = new ObjectGuid[SAObjectTypes.MaxObj];
        BgCreatures = new ObjectGuid[SACreatureTypes.Max + SAGraveyards.Max];
        TimerEnabled = false;
        UpdateWaitTimer = 0;
        SignaledRoundTwo = false;
        SignaledRoundTwoHalfMin = false;
        InitSecondRound = false;
        Attackers = TeamIds.Alliance;
        TotalTime = 0;
        EndRoundTimer = 0;
        ShipsStarted = false;
        Status = SAStatus.NotStarted;

        for (byte i = 0; i < GateStatus.Length; ++i)
            GateStatus[i] = SAGateState.HordeGateOk;

        for (byte i = 0; i < 2; i++)
        {
            RoundScores[i].winner = TeamIds.Alliance;
            RoundScores[i].time = 0;
        }
    }

    public override void AddPlayer(Player player)
    {
        var isInBattleground = IsPlayerInBattleground(player.GUID);
        base.AddPlayer(player);

        if (!isInBattleground)
            PlayerScores[player.GUID] = new BattlegroundSAScore(player.GUID, player.GetBgTeam());

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
                if (CanInteractWithObject(SAObjectTypes.LeftFlag))
                    CaptureGraveyard(SAGraveyards.LeftCapturableGy, source);

                break;
            case 191305:
            case 191306:
                if (CanInteractWithObject(SAObjectTypes.RightFlag))
                    CaptureGraveyard(SAGraveyards.RightCapturableGy, source);

                break;
            case 191310:
            case 191309:
                if (CanInteractWithObject(SAObjectTypes.CentralFlag))
                    CaptureGraveyard(SAGraveyards.CentralCapturableGy, source);

                break;
            default:
                return;
        }
    }

    public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
    {
        uint safeloc;

        var teamId = GetTeamIndexByTeamId(GetPlayerTeam(player.GUID));

        if (teamId == Attackers)
            safeloc = SAMiscConst.GYEntries[SAGraveyards.BeachGy];
        else
            safeloc = SAMiscConst.GYEntries[SAGraveyards.DefenderLastGy];

        var closest = Global.ObjectMgr.GetWorldSafeLoc(safeloc);
        var nearest = player.Location.GetExactDistSq(closest.Loc);

        for (byte i = SAGraveyards.RightCapturableGy; i < SAGraveyards.Max; i++)
        {
            if (GraveyardStatus[i] != teamId)
                continue;

            var ret = Global.ObjectMgr.GetWorldSafeLoc(SAMiscConst.GYEntries[i]);
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
        if (creature.Entry == SACreatureIds.Demolisher)
        {
            UpdatePlayerScore(killer, ScoreType.DestroyedDemolisher, 1);
            var worldStateId = Attackers == TeamIds.Horde ? SAWorldStateIds.DestroyedHordeVehicles : SAWorldStateIds.DestroyedAllianceVehicles;
            var currentDestroyedVehicles = Global.WorldStateMgr.GetValue((int)worldStateId, GetBgMap());
            UpdateWorldState(worldStateId, currentDestroyedVehicles + 1);
        }
    }

    public override bool IsSpellAllowed(uint spellId, Player player)
    {
        switch (spellId)
        {
            case SASpellIds.AllianceControlPhaseShift:
                return Attackers == TeamIds.Horde;
            case SASpellIds.HordeControlPhaseShift:
                return Attackers == TeamIds.Alliance;
            case BattlegroundConst.SpellPreparation:
                return Status == SAStatus.Warmup || Status == SAStatus.SecondWarmup;
            default:
                break;
        }

        return true;
    }

    public override void PostUpdateImpl(uint diff)
    {
        if (InitSecondRound)
        {
            if (UpdateWaitTimer < diff)
            {
                if (!SignaledRoundTwo)
                {
                    SignaledRoundTwo = true;
                    InitSecondRound = false;
                    SendBroadcastText(SABroadcastTexts.RoundTwoStartOneMinute, ChatMsg.BgSystemNeutral);
                }
            }
            else
            {
                UpdateWaitTimer -= diff;

                return;
            }
        }

        TotalTime += diff;

        if (Status == SAStatus.Warmup)
        {
            EndRoundTimer = SATimers.RoundLength;
            UpdateWorldState(SAWorldStateIds.Timer, (int)(GameTime.CurrentTime + EndRoundTimer));

            if (TotalTime >= SATimers.WarmupLength)
            {
                var c = GetBGCreature(SACreatureTypes.Kanrethad);

                if (c)
                    SendChatMessage(c, SATextIds.RoundStarted);

                TotalTime = 0;
                ToggleTimer();
                DemolisherStartState(false);
                Status = SAStatus.RoundOne;
                TriggerGameEvent(Attackers == TeamIds.Alliance ? 23748 : 21702u);
            }

            if (TotalTime >= SATimers.BoatStart)
                StartShips();

            return;
        }
        else if (Status == SAStatus.SecondWarmup)
        {
            if (RoundScores[0].time < SATimers.RoundLength)
                EndRoundTimer = RoundScores[0].time;
            else
                EndRoundTimer = SATimers.RoundLength;

            UpdateWorldState(SAWorldStateIds.Timer, (int)(GameTime.CurrentTime + EndRoundTimer));

            if (TotalTime >= 60000)
            {
                var c = GetBGCreature(SACreatureTypes.Kanrethad);

                if (c)
                    SendChatMessage(c, SATextIds.RoundStarted);

                TotalTime = 0;
                ToggleTimer();
                DemolisherStartState(false);
                Status = SAStatus.RoundTwo;
                TriggerGameEvent(Attackers == TeamIds.Alliance ? 23748 : 21702u);
                // status was set to STATUS_WAIT_JOIN manually for Preparation, set it back now
                SetStatus(BattlegroundStatus.InProgress);

                foreach (var pair in GetPlayers())
                {
                    var p = Global.ObjAccessor.FindPlayer(pair.Key);

                    if (p)
                        p.RemoveAura(BattlegroundConst.SpellPreparation);
                }
            }

            if (TotalTime >= 30000)
                if (!SignaledRoundTwoHalfMin)
                {
                    SignaledRoundTwoHalfMin = true;
                    SendBroadcastText(SABroadcastTexts.RoundTwoStartHalfMinute, ChatMsg.BgSystemNeutral);
                }

            StartShips();

            return;
        }
        else if (GetStatus() == BattlegroundStatus.InProgress)
        {
            if (Status == SAStatus.RoundOne)
            {
                if (TotalTime >= SATimers.RoundLength)
                {
                    CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Alliance);
                    CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Horde);
                    RoundScores[0].winner = (uint)Attackers;
                    RoundScores[0].time = SATimers.RoundLength;
                    TotalTime = 0;
                    Status = SAStatus.SecondWarmup;
                    Attackers = (Attackers == TeamIds.Alliance) ? TeamIds.Horde : TeamIds.Alliance;
                    UpdateWaitTimer = 5000;
                    SignaledRoundTwo = false;
                    SignaledRoundTwoHalfMin = false;
                    InitSecondRound = true;
                    ToggleTimer();
                    ResetObjs();
                    GetBgMap().UpdateAreaDependentAuras();

                    return;
                }
            }
            else if (Status == SAStatus.RoundTwo)
            {
                if (TotalTime >= EndRoundTimer)
                {
                    CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Alliance);
                    CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Horde);
                    RoundScores[1].time = SATimers.RoundLength;
                    RoundScores[1].winner = (uint)((Attackers == TeamIds.Alliance) ? TeamIds.Horde : TeamIds.Alliance);

                    if (RoundScores[0].time == RoundScores[1].time)
                        EndBattleground(0);
                    else if (RoundScores[0].time < RoundScores[1].time)
                        EndBattleground(RoundScores[0].winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
                    else
                        EndBattleground(RoundScores[1].winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);

                    return;
                }
            }

            if (Status == SAStatus.RoundOne || Status == SAStatus.RoundTwo)
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
                        if (eventId == (uint)SAEventIds.BG_SA_EVENT_TITAN_RELIC_ACTIVATED)
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
                            GateStatus[gateId] = Attackers == TeamIds.Horde ? SAGateState.AllianceGateDamaged : SAGateState.HordeGateDamaged;

                            var c = obj.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                            if (c)
                                SendChatMessage(c, (byte)gate.DamagedText, invoker);

                            PlaySoundToAll(Attackers == TeamIds.Alliance ? SASoundIds.WallAttackedAlliance : SASoundIds.WallAttackedHorde);
                        }
                        // destroyed
                        else if (eventId == go.Template.DestructibleBuilding.DestroyedEvent)
                        {
                            GateStatus[gate.GateId] = Attackers == TeamIds.Horde ? SAGateState.AllianceGateDestroyed : SAGateState.HordeGateDestroyed;

                            if (gateId < 5)
                                DelObject((int)gateId + 14);

                            var c = obj.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                            if (c)
                                SendChatMessage(c, (byte)gate.DestroyedText, invoker);

                            PlaySoundToAll(Attackers == TeamIds.Alliance ? SASoundIds.WallDestroyedAlliance : SASoundIds.WallDestroyedHorde);

                            var rewardHonor = true;

                            switch (gateId)
                            {
                                case SAObjectTypes.GreenGate:
                                    if (IsGateDestroyed(SAObjectTypes.BlueGate))
                                        rewardHonor = false;

                                    break;
                                case SAObjectTypes.BlueGate:
                                    if (IsGateDestroyed(SAObjectTypes.GreenGate))
                                        rewardHonor = false;

                                    break;
                                case SAObjectTypes.RedGate:
                                    if (IsGateDestroyed(SAObjectTypes.PurpleGate))
                                        rewardHonor = false;

                                    break;
                                case SAObjectTypes.PurpleGate:
                                    if (IsGateDestroyed(SAObjectTypes.RedGate))
                                        rewardHonor = false;

                                    break;
                                default:
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

                        UpdateWorldState(gate.WorldState, (int)GateStatus[gateId]);
                    }

                    break;
                }
                default:
                    break;
            }
    }

    public override void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team) { }

    public override void Reset()
    {
        TotalTime = 0;
        Attackers = (RandomHelper.URand(0, 1) != 0 ? TeamIds.Alliance : TeamIds.Horde);

        for (byte i = 0; i <= 5; i++)
            GateStatus[i] = SAGateState.HordeGateOk;

        ShipsStarted = false;
        Status = SAStatus.Warmup;
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
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)SAObjectives.DemolishersDestroyed);

                break;
            case ScoreType.DestroyedWall:
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)SAObjectives.GatesDestroyed);

                break;
            default:
                break;
        }

        return true;
    }

    private bool CanInteractWithObject(uint objectId)
    {
        switch (objectId)
        {
            case SAObjectTypes.TitanRelic:
                if (!IsGateDestroyed(SAObjectTypes.AncientGate) || !IsGateDestroyed(SAObjectTypes.YellowGate))
                    return false;

                goto case SAObjectTypes.CentralFlag;
            case SAObjectTypes.CentralFlag:
                if (!IsGateDestroyed(SAObjectTypes.RedGate) && !IsGateDestroyed(SAObjectTypes.PurpleGate))
                    return false;

                goto case SAObjectTypes.LeftFlag;
            case SAObjectTypes.LeftFlag:
            case SAObjectTypes.RightFlag:
                if (!IsGateDestroyed(SAObjectTypes.GreenGate) && !IsGateDestroyed(SAObjectTypes.BlueGate))
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
        if (GraveyardStatus[i] == Attackers)
            return;

        DelCreature(SACreatureTypes.Max + i);
        var teamId = GetTeamIndexByTeamId(GetPlayerTeam(source.GUID));
        GraveyardStatus[i] = teamId;
        var sg = Global.ObjectMgr.GetWorldSafeLoc(SAMiscConst.GYEntries[i]);

        if (sg == null)
        {
            Log.Logger.Error($"CaptureGraveyard: non-existant GY entry: {SAMiscConst.GYEntries[i]}");

            return;
        }

        AddSpiritGuide(i + SACreatureTypes.Max, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, SAMiscConst.GYOrientation[i], GraveyardStatus[i]);

        uint npc;
        int flag;

        switch (i)
        {
            case SAGraveyards.LeftCapturableGy:
            {
                flag = SAObjectTypes.LeftFlag;
                DelObject(flag);

                AddObject(flag,
                          (SAMiscConst.ObjEntries[flag] - (teamId == TeamIds.Alliance ? 0 : 1u)),
                          SAMiscConst.ObjSpawnlocs[flag],
                          0,
                          0,
                          0,
                          0,
                          BattlegroundConst.RespawnOneDay);

                npc = SACreatureTypes.Rigspark;
                var rigspark = AddCreature(SAMiscConst.NpcEntries[npc], (int)npc, SAMiscConst.NpcSpawnlocs[npc], Attackers);

                if (rigspark)
                    rigspark.AI.Talk(SATextIds.SparklightRigsparkSpawn);

                for (byte j = SACreatureTypes.Demolisher7; j <= SACreatureTypes.Demolisher8; j++)
                {
                    AddCreature(SAMiscConst.NpcEntries[j], j, SAMiscConst.NpcSpawnlocs[j], (Attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance), 600);
                    var dem = GetBGCreature(j);

                    if (dem)
                        dem.Faction = SAMiscConst.Factions[Attackers];
                }

                UpdateWorldState(SAWorldStateIds.LeftGyAlliance, GraveyardStatus[i] == TeamIds.Alliance ? 1 : 0);
                UpdateWorldState(SAWorldStateIds.LeftGyHorde, GraveyardStatus[i] == TeamIds.Horde ? 1 : 0);

                var c = source.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                if (c)
                    SendChatMessage(c, teamId == TeamIds.Alliance ? SATextIds.WestGraveyardCapturedA : SATextIds.WestGraveyardCapturedH, source);
            }

                break;
            case SAGraveyards.RightCapturableGy:
            {
                flag = SAObjectTypes.RightFlag;
                DelObject(flag);

                AddObject(flag,
                          (SAMiscConst.ObjEntries[flag] - (teamId == TeamIds.Alliance ? 0 : 1u)),
                          SAMiscConst.ObjSpawnlocs[flag],
                          0,
                          0,
                          0,
                          0,
                          BattlegroundConst.RespawnOneDay);

                npc = SACreatureTypes.Sparklight;
                var sparklight = AddCreature(SAMiscConst.NpcEntries[npc], (int)npc, SAMiscConst.NpcSpawnlocs[npc], Attackers);

                if (sparklight)
                    sparklight.AI.Talk(SATextIds.SparklightRigsparkSpawn);

                for (byte j = SACreatureTypes.Demolisher5; j <= SACreatureTypes.Demolisher6; j++)
                {
                    AddCreature(SAMiscConst.NpcEntries[j], j, SAMiscConst.NpcSpawnlocs[j], Attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance, 600);

                    var dem = GetBGCreature(j);

                    if (dem)
                        dem.Faction = SAMiscConst.Factions[Attackers];
                }

                UpdateWorldState(SAWorldStateIds.RightGyAlliance, GraveyardStatus[i] == TeamIds.Alliance ? 1 : 0);
                UpdateWorldState(SAWorldStateIds.RightGyHorde, GraveyardStatus[i] == TeamIds.Horde ? 1 : 0);

                var c = source.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                if (c)
                    SendChatMessage(c, teamId == TeamIds.Alliance ? SATextIds.EastGraveyardCapturedA : SATextIds.EastGraveyardCapturedH, source);
            }

                break;
            case SAGraveyards.CentralCapturableGy:
            {
                flag = SAObjectTypes.CentralFlag;
                DelObject(flag);

                AddObject(flag,
                          (SAMiscConst.ObjEntries[flag] - (teamId == TeamIds.Alliance ? 0 : 1u)),
                          SAMiscConst.ObjSpawnlocs[flag],
                          0,
                          0,
                          0,
                          0,
                          BattlegroundConst.RespawnOneDay);

                UpdateWorldState(SAWorldStateIds.CenterGyAlliance, GraveyardStatus[i] == TeamIds.Alliance ? 1 : 0);
                UpdateWorldState(SAWorldStateIds.CenterGyHorde, GraveyardStatus[i] == TeamIds.Horde ? 1 : 0);

                var c = source.Location.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                if (c)
                    SendChatMessage(c, teamId == TeamIds.Alliance ? SATextIds.SouthGraveyardCapturedA : SATextIds.SouthGraveyardCapturedH, source);
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
        for (byte i = SACreatureTypes.Demolisher1; i <= SACreatureTypes.Demolisher4; i++)
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

    private SAGateInfo GetGate(uint entry)
    {
        foreach (var gate in SAMiscConst.Gates)
            if (gate.GameObjectId == entry)
                return gate;

        return null;
    }

    private bool IsGateDestroyed(uint gateId)
    {
        return GateStatus[gateId] == SAGateState.AllianceGateDestroyed || GateStatus[gateId] == SAGateState.HordeGateDestroyed;
    }

    private void OverrideGunFaction()
    {
        if (BgCreatures[0].IsEmpty)
            return;

        for (byte i = SACreatureTypes.Gun1; i <= SACreatureTypes.Gun10; i++)
        {
            var gun = GetBGCreature(i);

            if (gun)
                gun.Faction = SAMiscConst.Factions[Attackers != 0 ? TeamIds.Alliance : TeamIds.Horde];
        }

        for (byte i = SACreatureTypes.Demolisher1; i <= SACreatureTypes.Demolisher4; i++)
        {
            var dem = GetBGCreature(i);

            if (dem)
                dem.Faction = SAMiscConst.Factions[Attackers];
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

        var atF = SAMiscConst.Factions[Attackers];
        var defF = SAMiscConst.Factions[Attackers != 0 ? TeamIds.Alliance : TeamIds.Horde];

        for (byte i = 0; i < SAObjectTypes.MaxObj; i++)
            DelObject(i);

        for (byte i = 0; i < SACreatureTypes.Max; i++)
            DelCreature(i);

        for (byte i = SACreatureTypes.Max; i < SACreatureTypes.Max + SAGraveyards.Max; i++)
            DelCreature(i);

        for (byte i = 0; i < GateStatus.Length; ++i)
            GateStatus[i] = Attackers == TeamIds.Horde ? SAGateState.AllianceGateOk : SAGateState.HordeGateOk;

        if (!AddCreature(SAMiscConst.NpcEntries[SACreatureTypes.Kanrethad], SACreatureTypes.Kanrethad, SAMiscConst.NpcSpawnlocs[SACreatureTypes.Kanrethad]))
        {
            Log.Logger.Error($"SOTA: couldn't spawn Kanrethad, aborted. Entry: {SAMiscConst.NpcEntries[SACreatureTypes.Kanrethad]}");

            return false;
        }

        for (byte i = 0; i <= SAObjectTypes.PortalDeffenderRed; i++)
            if (!AddObject(i, SAMiscConst.ObjEntries[i], SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
            {
                Log.Logger.Error($"SOTA: couldn't spawn BG_SA_PORTAL_DEFFENDER_RED, Entry: {SAMiscConst.ObjEntries[i]}");

                continue;
            }

        for (var i = SAObjectTypes.BoatOne; i <= SAObjectTypes.BoatTwo; i++)
        {
            uint boatid = 0;

            switch (i)
            {
                case SAObjectTypes.BoatOne:
                    boatid = Attackers != 0 ? SAGameObjectIds.BoatOneH : SAGameObjectIds.BoatOneA;

                    break;
                case SAObjectTypes.BoatTwo:
                    boatid = Attackers != 0 ? SAGameObjectIds.BoatTwoH : SAGameObjectIds.BoatTwoA;

                    break;
                default:
                    break;
            }

            if (!AddObject(i,
                           boatid,
                           SAMiscConst.ObjSpawnlocs[i].X,
                           SAMiscConst.ObjSpawnlocs[i].Y,
                           SAMiscConst.ObjSpawnlocs[i].Z + (Attackers != 0 ? -3.750f : 0),
                           SAMiscConst.ObjSpawnlocs[i].Orientation,
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

        for (byte i = SAObjectTypes.Sigil1; i <= SAObjectTypes.LeftFlagpole; i++)
            if (!AddObject(i, SAMiscConst.ObjEntries[i], SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
            {
                Log.Logger.Error($"SOTA: couldn't spawn Sigil, Entry: {SAMiscConst.ObjEntries[i]}");

                continue;
            }

        // MAD props for Kiper for discovering those values - 4 hours of his work.
        GetBGObject(SAObjectTypes.BoatOne).SetParentRotation(new Quaternion(0.0f, 0.0f, 1.0f, 0.0002f));
        GetBGObject(SAObjectTypes.BoatTwo).SetParentRotation(new Quaternion(0.0f, 0.0f, 1.0f, 0.00001f));
        SpawnBGObject(SAObjectTypes.BoatOne, BattlegroundConst.RespawnImmediately);
        SpawnBGObject(SAObjectTypes.BoatTwo, BattlegroundConst.RespawnImmediately);

        //Cannons and demolishers - NPCs are spawned
        //By capturing GYs.
        for (byte i = 0; i < SACreatureTypes.Demolisher5; i++)
            if (!AddCreature(SAMiscConst.NpcEntries[i], i, SAMiscConst.NpcSpawnlocs[i], Attackers == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance, 600))
            {
                Log.Logger.Error($"SOTA: couldn't spawn Cannon or demolisher, Entry: {SAMiscConst.NpcEntries[i]}, Attackers: {(Attackers == TeamIds.Alliance ? "Horde(1)" : "Alliance(0)")}");

                continue;
            }

        OverrideGunFaction();
        DemolisherStartState(true);

        for (byte i = 0; i <= SAObjectTypes.PortalDeffenderRed; i++)
        {
            SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
            GetBGObject(i).Faction = defF;
        }

        GetBGObject(SAObjectTypes.TitanRelic).Faction = atF;
        GetBGObject(SAObjectTypes.TitanRelic).Refresh();

        TotalTime = 0;
        ShipsStarted = false;

        //Graveyards
        for (byte i = 0; i < SAGraveyards.Max; i++)
        {
            var sg = Global.ObjectMgr.GetWorldSafeLoc(SAMiscConst.GYEntries[i]);

            if (sg == null)
            {
                Log.Logger.Error($"SOTA: Can't find GY entry {SAMiscConst.GYEntries[i]}");

                return false;
            }

            if (i == SAGraveyards.BeachGy)
            {
                GraveyardStatus[i] = Attackers;
                AddSpiritGuide(i + SACreatureTypes.Max, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, SAMiscConst.GYOrientation[i], Attackers);
            }
            else
            {
                GraveyardStatus[i] = ((Attackers == TeamIds.Horde) ? TeamIds.Alliance : TeamIds.Horde);

                if (!AddSpiritGuide(i + SACreatureTypes.Max, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, SAMiscConst.GYOrientation[i], Attackers == TeamIds.Horde ? TeamIds.Alliance : TeamIds.Horde))
                    Log.Logger.Error($"SOTA: couldn't spawn GY: {i}");
            }
        }

        //GY capture points
        for (byte i = SAObjectTypes.CentralFlag; i <= SAObjectTypes.LeftFlag; i++)
        {
            if (!AddObject(i, (SAMiscConst.ObjEntries[i] - (Attackers == TeamIds.Alliance ? 1u : 0)), SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
            {
                Log.Logger.Error($"SOTA: couldn't spawn Central Flag Entry: {SAMiscConst.ObjEntries[i] - (Attackers == TeamIds.Alliance ? 1 : 0)}");

                continue;
            }

            GetBGObject(i).Faction = atF;
        }

        UpdateObjectInteractionFlags();

        for (byte i = SAObjectTypes.Bomb; i < SAObjectTypes.MaxObj; i++)
        {
            if (!AddObject(i, SAMiscConst.ObjEntries[SAObjectTypes.Bomb], SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
            {
                Log.Logger.Error($"SOTA: couldn't spawn SA Bomb Entry: {SAMiscConst.ObjEntries[SAObjectTypes.Bomb] + i}");

                continue;
            }

            GetBGObject(i).Faction = atF;
        }

        //Player may enter BEFORE we set up BG - lets update his worldstates anyway...
        UpdateWorldState(SAWorldStateIds.RightGyHorde, GraveyardStatus[SAGraveyards.RightCapturableGy] == TeamIds.Horde ? 1 : 0);
        UpdateWorldState(SAWorldStateIds.LeftGyHorde, GraveyardStatus[SAGraveyards.LeftCapturableGy] == TeamIds.Horde ? 1 : 0);
        UpdateWorldState(SAWorldStateIds.CenterGyHorde, GraveyardStatus[SAGraveyards.CentralCapturableGy] == TeamIds.Horde ? 1 : 0);

        UpdateWorldState(SAWorldStateIds.RightGyAlliance, GraveyardStatus[SAGraveyards.RightCapturableGy] == TeamIds.Alliance ? 1 : 0);
        UpdateWorldState(SAWorldStateIds.LeftGyAlliance, GraveyardStatus[SAGraveyards.LeftCapturableGy] == TeamIds.Alliance ? 1 : 0);
        UpdateWorldState(SAWorldStateIds.CenterGyAlliance, GraveyardStatus[SAGraveyards.CentralCapturableGy] == TeamIds.Alliance ? 1 : 0);

        if (Attackers == TeamIds.Alliance)
        {
            UpdateWorldState(SAWorldStateIds.AllyAttacks, 1);
            UpdateWorldState(SAWorldStateIds.HordeAttacks, 0);

            UpdateWorldState(SAWorldStateIds.RightAttTokenAll, 1);
            UpdateWorldState(SAWorldStateIds.LeftAttTokenAll, 1);
            UpdateWorldState(SAWorldStateIds.RightAttTokenHrd, 0);
            UpdateWorldState(SAWorldStateIds.LeftAttTokenHrd, 0);

            UpdateWorldState(SAWorldStateIds.HordeDefenceToken, 1);
            UpdateWorldState(SAWorldStateIds.AllianceDefenceToken, 0);
        }
        else
        {
            UpdateWorldState(SAWorldStateIds.HordeAttacks, 1);
            UpdateWorldState(SAWorldStateIds.AllyAttacks, 0);

            UpdateWorldState(SAWorldStateIds.RightAttTokenAll, 0);
            UpdateWorldState(SAWorldStateIds.LeftAttTokenAll, 0);
            UpdateWorldState(SAWorldStateIds.RightAttTokenHrd, 1);
            UpdateWorldState(SAWorldStateIds.LeftAttTokenHrd, 1);

            UpdateWorldState(SAWorldStateIds.HordeDefenceToken, 0);
            UpdateWorldState(SAWorldStateIds.AllianceDefenceToken, 1);
        }

        UpdateWorldState(SAWorldStateIds.AttackerTeam, Attackers);
        UpdateWorldState(SAWorldStateIds.PurpleGate, 1);
        UpdateWorldState(SAWorldStateIds.RedGate, 1);
        UpdateWorldState(SAWorldStateIds.BlueGate, 1);
        UpdateWorldState(SAWorldStateIds.GreenGate, 1);
        UpdateWorldState(SAWorldStateIds.YellowGate, 1);
        UpdateWorldState(SAWorldStateIds.AncientGate, 1);

        for (var i = SAObjectTypes.BoatOne; i <= SAObjectTypes.BoatTwo; i++)
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
        if (!BgObjects[SAObjectTypes.BoatOne].IsEmpty || !BgObjects[SAObjectTypes.BoatTwo].IsEmpty)
        {
            UpdateData transData = new(player.Location.MapId);

            if (!BgObjects[SAObjectTypes.BoatOne].IsEmpty)
                GetBGObject(SAObjectTypes.BoatOne).BuildCreateUpdateBlockForPlayer(transData, player);

            if (!BgObjects[SAObjectTypes.BoatTwo].IsEmpty)
                GetBGObject(SAObjectTypes.BoatTwo).BuildCreateUpdateBlockForPlayer(transData, player);

            transData.BuildPacket(out var packet);
            player.SendPacket(packet);
        }
    }

    private void SendTransportsRemove(Player player)
    {
        if (!BgObjects[SAObjectTypes.BoatOne].IsEmpty || !BgObjects[SAObjectTypes.BoatTwo].IsEmpty)
        {
            UpdateData transData = new(player.Location.MapId);

            if (!BgObjects[SAObjectTypes.BoatOne].IsEmpty)
                GetBGObject(SAObjectTypes.BoatOne).BuildOutOfRangeUpdateBlock(transData);

            if (!BgObjects[SAObjectTypes.BoatTwo].IsEmpty)
                GetBGObject(SAObjectTypes.BoatTwo).BuildOutOfRangeUpdateBlock(transData);

            transData.BuildPacket(out var packet);
            player.SendPacket(packet);
        }
    }

    private void StartShips()
    {
        if (ShipsStarted)
            return;

        GetBGObject(SAObjectTypes.BoatOne).SetGoState(GameObjectState.TransportStopped);
        GetBGObject(SAObjectTypes.BoatTwo).SetGoState(GameObjectState.TransportStopped);

        for (var i = SAObjectTypes.BoatOne; i <= SAObjectTypes.BoatTwo; i++)
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

        ShipsStarted = true;
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
        if (GetTeamIndexByTeamId(GetPlayerTeam(player.GUID)) == Attackers)
        {
            if (!ShipsStarted)
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

        if (CanInteractWithObject(SAObjectTypes.TitanRelic))
        {
            var clickerTeamId = GetTeamIndexByTeamId(GetPlayerTeam(clicker.GUID));

            if (clickerTeamId == Attackers)
            {
                if (clickerTeamId == TeamIds.Alliance)
                    SendBroadcastText(SABroadcastTexts.AllianceCapturedTitanPortal, ChatMsg.BgSystemNeutral);
                else
                    SendBroadcastText(SABroadcastTexts.HordeCapturedTitanPortal, ChatMsg.BgSystemNeutral);

                if (Status == SAStatus.RoundOne)
                {
                    RoundScores[0].winner = (uint)Attackers;
                    RoundScores[0].time = TotalTime;

                    // Achievement Storm the Beach (1310)
                    foreach (var pair in GetPlayers())
                    {
                        var player = Global.ObjAccessor.FindPlayer(pair.Key);

                        if (player)
                            if (GetTeamIndexByTeamId(GetPlayerTeam(player.GUID)) == Attackers)
                                player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                    }

                    Attackers = (Attackers == TeamIds.Alliance) ? TeamIds.Horde : TeamIds.Alliance;
                    Status = SAStatus.SecondWarmup;
                    TotalTime = 0;
                    ToggleTimer();

                    var c = GetBGCreature(SACreatureTypes.Kanrethad);

                    if (c)
                        SendChatMessage(c, SATextIds.Round1Finished);

                    UpdateWaitTimer = 5000;
                    SignaledRoundTwo = false;
                    SignaledRoundTwoHalfMin = false;
                    InitSecondRound = true;
                    ResetObjs();
                    GetBgMap().UpdateAreaDependentAuras();
                    CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Alliance);
                    CastSpellOnTeam(SASpellIds.EndOfRound, TeamFaction.Horde);
                }
                else if (Status == SAStatus.RoundTwo)
                {
                    RoundScores[1].winner = (uint)Attackers;
                    RoundScores[1].time = TotalTime;
                    ToggleTimer();

                    // Achievement Storm the Beach (1310)
                    foreach (var pair in GetPlayers())
                    {
                        var player = Global.ObjAccessor.FindPlayer(pair.Key);

                        if (player)
                            if (GetTeamIndexByTeamId(GetPlayerTeam(player.GUID)) == Attackers && RoundScores[1].winner == Attackers)
                                player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                    }

                    if (RoundScores[0].time == RoundScores[1].time)
                        EndBattleground(0);
                    else if (RoundScores[0].time < RoundScores[1].time)
                        EndBattleground(RoundScores[0].winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
                    else
                        EndBattleground(RoundScores[1].winner == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
                }
            }
        }
    }

    private void ToggleTimer()
    {
        TimerEnabled = !TimerEnabled;
        UpdateWorldState(SAWorldStateIds.EnableTimer, TimerEnabled ? 1 : 0);
    }

    private void UpdateDemolisherSpawns()
    {
        for (byte i = SACreatureTypes.Demolisher1; i <= SACreatureTypes.Demolisher8; i++)
            if (!BgCreatures[i].IsEmpty)
            {
                var Demolisher = GetBGCreature(i);

                if (Demolisher)
                    if (Demolisher.IsDead)
                    {
                        // Demolisher is not in list
                        if (!DemoliserRespawnList.ContainsKey(i))
                        {
                            DemoliserRespawnList[i] = GameTime.CurrentTimeMS + 30000;
                        }
                        else
                        {
                            if (DemoliserRespawnList[i] < GameTime.CurrentTimeMS)
                            {
                                Demolisher.Location.Relocate(SAMiscConst.NpcSpawnlocs[i]);
                                Demolisher.Respawn();
                                DemoliserRespawnList.Remove(i);
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
        for (byte i = SAObjectTypes.CentralFlag; i <= SAObjectTypes.LeftFlag; ++i)
            UpdateObjectInteractionFlags(i);

        UpdateObjectInteractionFlags(SAObjectTypes.TitanRelic);
    }
}