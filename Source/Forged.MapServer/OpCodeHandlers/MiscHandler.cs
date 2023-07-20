// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.DataStorage.Structs.U;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Guilds;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Achievements;
using Forged.MapServer.Networking.Packets.AreaTrigger;
using Forged.MapServer.Networking.Packets.Character;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Networking.Packets.ClientConfig;
using Forged.MapServer.Networking.Packets.Instance;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Warden;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IConversation;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.IO;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class MiscHandler : IWorldSessionHandler
{
    private readonly DB6Storage<AreaTriggerRecord> _areaTriggerData;
    private readonly ConditionManager _conditionManager;
    private readonly DB2Manager _db2Manager;
    private readonly GuildManager _guildManager;
    private readonly InstanceLockManager _instanceLockManager;
    private readonly MapManager _mapManager;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly GameObjectManager _objectManager;
    private readonly DB6Storage<PlayerConditionRecord> _playerConditionRecords;
    private readonly ScriptManager _scriptManager;
    private readonly WorldSession _session;
    private readonly DB6Storage<UISplashScreenRecord> _splashScreenRecords;
    private readonly Warden.Warden _warden;
    private readonly WorldManager _worldManager;

    public MiscHandler(WorldSession session, Warden.Warden warden, ScriptManager scriptManager, DB6Storage<AreaTriggerRecord> areaTriggerData, ConditionManager conditionManager,
                       GameObjectManager objectManager, WorldManager worldManager, MapManager mapManager, DB6Storage<MapRecord> mapRecords, DB2Manager db2Manager,
                       InstanceLockManager instanceLockManager, GuildManager guildManager, DB6Storage<UISplashScreenRecord> splashScreenRecords, DB6Storage<PlayerConditionRecord> playerConditionRecords)
    {
        _session = session;
        _warden = warden;
        _scriptManager = scriptManager;
        _areaTriggerData = areaTriggerData;
        _conditionManager = conditionManager;
        _objectManager = objectManager;
        _worldManager = worldManager;
        _mapManager = mapManager;
        _mapRecords = mapRecords;
        _db2Manager = db2Manager;
        _instanceLockManager = instanceLockManager;
        _guildManager = guildManager;
        _splashScreenRecords = splashScreenRecords;
        _playerConditionRecords = playerConditionRecords;
    }

    public void SendLoadCUFProfiles()
    {
        LoadCUFProfiles loadCUFProfiles = new();

        for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
        {
            var cufProfile = _session.Player.GetCufProfile(i);

            if (cufProfile != null)
                loadCUFProfiles.CUFProfiles.Add(cufProfile);
        }

        _session.SendPacket(loadCUFProfiles);
    }

    [WorldPacketHandler(ClientOpcodes.AreaTrigger, Processing = PacketProcessing.Inplace)]
    private void HandleAreaTrigger(AreaTriggerPkt packet)
    {
        if (_session.Player.IsInFlight)
        {
            Log.Logger.Debug("HandleAreaTrigger: Player '{0}' (GUID: {1}) in flight, ignore Area Trigger ID:{2}",
                             _session.Player.GetName(),
                             _session.Player.GUID.ToString(),
                             packet.AreaTriggerID);

            return;
        }

        if (!_areaTriggerData.TryGetValue(packet.AreaTriggerID, out var atEntry))
        {
            Log.Logger.Debug("HandleAreaTrigger: Player '{0}' (GUID: {1}) send unknown (by DBC) Area Trigger ID:{2}",
                             _session.Player.GetName(),
                             _session.Player.GUID.ToString(),
                             packet.AreaTriggerID);

            return;
        }

        if (packet.Entered && !_session.Player.IsInAreaTriggerRadius(atEntry))
        {
            Log.Logger.Debug("HandleAreaTrigger: Player '{0}' ({1}) too far, ignore Area Trigger ID: {2}",
                             _session.Player.GetName(),
                             _session.Player.GUID.ToString(),
                             packet.AreaTriggerID);

            return;
        }

        if (_session.Player.IsDebugAreaTriggers)
            _session.Player.SendSysMessage(packet.Entered ? CypherStrings.DebugAreatriggerEntered : CypherStrings.DebugAreatriggerLeft, packet.AreaTriggerID);

        if (!_conditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.AreatriggerClientTriggered, atEntry.Id, _session.Player))
            return;

        if (_scriptManager.OnAreaTrigger(_session.Player, atEntry, packet.Entered))
            return;

        if (_session.Player.IsAlive)
        {
            // not using Player.UpdateQuestObjectiveProgress, ObjectID in quest_objectives can be set to -1, areatrigger_involvedrelation then holds correct id
            var quests = _objectManager.GetQuestsForAreaTrigger(packet.AreaTriggerID);

            if (quests != null)
            {
                var anyObjectiveChangedCompletionState = false;

                foreach (var questId in quests)
                {
                    var qInfo = _objectManager.GetQuestTemplate(questId);
                    var slot = _session.Player.FindQuestSlot(questId);

                    if (qInfo == null || slot >= SharedConst.MaxQuestLogSize || _session.Player.GetQuestStatus(questId) != QuestStatus.Incomplete)
                        continue;

                    foreach (var obj in qInfo.Objectives)
                    {
                        if (obj.Type != QuestObjectiveType.AreaTrigger)
                            continue;

                        if (!_session.Player.IsQuestObjectiveCompletable(slot, qInfo, obj))
                            continue;

                        if (_session.Player.IsQuestObjectiveComplete(slot, qInfo, obj))
                            continue;

                        if (obj.ObjectID != -1 && obj.ObjectID != packet.AreaTriggerID)
                            continue;

                        _session.Player.SetQuestObjectiveData(obj, 1);
                        _session.Player.SendQuestUpdateAddCreditSimple(obj);
                        anyObjectiveChangedCompletionState = true;

                        break;
                    }

                    _session.Player.AreaExploredOrEventHappens(questId);

                    if (_session.Player.CanCompleteQuest(questId))
                        _session.Player.CompleteQuest(questId);
                }

                if (anyObjectiveChangedCompletionState)
                    _session.Player.UpdateVisibleGameobjectsOrSpellClicks();
            }
        }

        if (_objectManager.IsTavernAreaTrigger(packet.AreaTriggerID))
        {
            // set resting Id we are in the inn
            _session.Player. // set resting Id we are in the inn
                RestMgr.SetRestFlag(RestFlag.Tavern, atEntry.Id);

            if (_worldManager.IsFFAPvPRealm)
                _session.Player.RemovePvpFlag(UnitPVPStateFlags.FFAPvp);

            return;
        }

        _session.Player.Battleground?.HandleAreaTrigger(_session.Player, packet.AreaTriggerID, packet.Entered);

        var pvp = _session.Player.GetOutdoorPvP();

        if (pvp != null)
            if (pvp.HandleAreaTrigger(_session.Player, packet.AreaTriggerID, packet.Entered))
                return;

        var at = _objectManager.GetAreaTrigger(packet.AreaTriggerID);

        if (at == null)
            return;

        var teleported = false;

        if (_session.Player.Location.MapId != at.TargetMapId)
        {
            if (!_session.Player.IsAlive)
            {
                if (_session.Player.HasCorpse)
                {
                    // let enter in ghost mode in instance that connected to inner instance with corpse
                    var corpseMap = _session.Player.CorpseLocation.MapId;

                    do
                    {
                        if (corpseMap == at.TargetMapId)
                            break;

                        var corpseInstance = _objectManager.GetInstanceTemplate(corpseMap);
                        corpseMap = corpseInstance?.Parent ?? 0;
                    } while (corpseMap != 0);

                    if (corpseMap == 0)
                    {
                        _session.SendPacket(new AreaTriggerNoCorpse());

                        return;
                    }

                    Log.Logger.Debug($"MAP: Player '{_session.Player.GetName()}' has corpse in instance {at.TargetMapId} and can enter.");
                }
                else
                    Log.Logger.Debug($"Map::CanPlayerEnter - player '{_session.Player.GetName()}' is dead but does not have a corpse!");
            }

            var denyReason = _session.Player.Location.PlayerCannotEnter(at.TargetMapId, _session.Player);

            if (denyReason != null)
            {
                switch (denyReason.Reason)
                {
                    case TransferAbortReason.MapNotAllowed:
                        Log.Logger.Debug($"MAP: Player '{_session.Player.GetName()}' attempted to enter map with id {at.TargetMapId} which has no entry");

                        break;

                    case TransferAbortReason.Difficulty:
                        Log.Logger.Debug($"MAP: Player '{_session.Player.GetName()}' attempted to enter instance map {at.TargetMapId} but the requested difficulty was not found");

                        break;

                    case TransferAbortReason.NeedGroup:
                        Log.Logger.Debug($"MAP: Player '{_session.Player.GetName()}' must be in a raid group to enter map {at.TargetMapId}");
                        _session.Player.SendRaidGroupOnlyMessage(RaidGroupReason.Only, 0);

                        break;

                    case TransferAbortReason.LockedToDifferentInstance:
                        Log.Logger.Debug($"MAP: Player '{_session.Player.GetName()}' cannot enter instance map {at.TargetMapId} because their permanent bind is incompatible with their group's");

                        break;

                    case TransferAbortReason.AlreadyCompletedEncounter:
                        Log.Logger.Debug($"MAP: Player '{_session.Player.GetName()}' cannot enter instance map {at.TargetMapId} because their permanent bind is incompatible with their group's");

                        break;

                    case TransferAbortReason.TooManyInstances:
                        Log.Logger.Debug("MAP: Player '{0}' cannot enter instance map {1} because he has exceeded the maximum number of instances per hour.", _session.Player.GetName(), at.TargetMapId);

                        break;

                    case TransferAbortReason.MaxPlayers:
                    case TransferAbortReason.ZoneInCombat:
                        break;

                    case TransferAbortReason.NotFound:
                        Log.Logger.Debug($"MAP: Player '{_session.Player.GetName()}' cannot enter instance map {at.TargetMapId} because instance is resetting.");

                        break;
                }

                if (denyReason.Reason != TransferAbortReason.NeedGroup)
                    _session.Player.SendTransferAborted(at.TargetMapId, denyReason.Reason, denyReason.Arg, denyReason.MapDifficultyXConditionId);

                if (_session.Player.IsAlive || !_session.Player.HasCorpse)
                    return;

                if (_session.Player.CorpseLocation.MapId != at.TargetMapId)
                    return;

                _session.Player.ResurrectPlayer(0.5f);
                _session.Player.SpawnCorpseBones();

                return;
            }

            var group = _session.Player.Group;

            if (group != null)
                if (group.IsLFGGroup && _session.Player.Location.Map.IsDungeon)
                    teleported = _session.Player.TeleportToBGEntryPoint();
        }

        if (teleported)
            return;

        {
            WorldSafeLocsEntry entranceLocation = null;
            var mapEntry = _mapRecords.LookupByKey(at.TargetMapId);

            if (mapEntry.Instanceable())
            {
                // Check if we can contact the instancescript of the instance for an updated entrance location
                var targetInstanceId = _mapManager.FindInstanceIdForPlayer(at.TargetMapId, _session.Player);

                if (targetInstanceId != 0)
                {
                    var map = _mapManager.FindMap(at.TargetMapId, targetInstanceId);

                    var instanceScript = map?.ToInstanceMap?.InstanceScript;

                    if (instanceScript != null)
                        entranceLocation = _objectManager.GetWorldSafeLoc(instanceScript.GetEntranceLocation());
                }

                // Finally check with the instancesave for an entrance location if we did not get a valid one from the instancescript
                if (entranceLocation == null)
                {
                    var group = _session.Player.Group;
                    var difficulty = group?.GetDifficultyID(mapEntry) ?? _session.Player.GetDifficultyId(mapEntry);
                    var instanceOwnerGuid = group?.GetRecentInstanceOwner(at.TargetMapId) ?? _session.Player.GUID;
                    var instanceLock = _instanceLockManager.FindActiveInstanceLock(instanceOwnerGuid, new MapDb2Entries(mapEntry, _db2Manager.GetDownscaledMapDifficultyData(at.TargetMapId, ref difficulty)));

                    if (instanceLock != null)
                        entranceLocation = _objectManager.GetWorldSafeLoc(instanceLock.Data.EntranceWorldSafeLocId);
                }
            }

            if (entranceLocation != null)
                _session.Player.TeleportTo(entranceLocation.Location, TeleportToOptions.NotLeaveTransport);
            else
                _session.Player.TeleportTo(at.TargetMapId, at.TargetX, at.TargetY, at.TargetZ, at.TargetOrientation, TeleportToOptions.NotLeaveTransport);
        }
    }

    [WorldPacketHandler(ClientOpcodes.CloseInteraction)]
    private void HandleCloseInteraction(CloseInteraction closeInteraction)
    {
        if (_session.Player.PlayerTalkClass.InteractionData.SourceGuid == closeInteraction.SourceGuid)
            _session.Player.PlayerTalkClass.InteractionData.Reset();
    }

    [WorldPacketHandler(ClientOpcodes.CompleteCinematic)]
    private void HandleCompleteCinematic(CompleteCinematic packet)
    {
        if (packet == null)
            return;

        // If player has sight bound to visual waypoint NPC we should remove it
        _session.Player.CinematicMgr.EndCinematic();
    }

    [WorldPacketHandler(ClientOpcodes.CompleteMovie)]
    private void HandleCompleteMovie(CompleteMovie packet)
    {
        if (packet == null)
            return;

        var movie = _session.Player.Movie;

        if (movie == 0)
            return;

        _session.Player.Movie = 0;
        _scriptManager.ForEach<IPlayerOnMovieComplete>(p => p.OnMovieComplete(_session.Player, movie));
    }

    [WorldPacketHandler(ClientOpcodes.ConversationLineStarted)]
    private void HandleConversationLineStarted(ConversationLineStarted conversationLineStarted)
    {
        var convo = ObjectAccessor.GetConversation(_session.Player, conversationLineStarted.ConversationGUID);

        if (convo != null)
            _scriptManager.RunScript<IConversationOnConversationLineStarted>(script => script.OnConversationLineStarted(convo, conversationLineStarted.LineID, _session.Player), convo.GetScriptId());
    }

    [WorldPacketHandler(ClientOpcodes.FarSight)]
    private void HandleFarSight(FarSight farSight)
    {
        if (farSight.Enable)
        {
            Log.Logger.Debug("Added FarSight {0} to player {1}", _session.Player.ActivePlayerData.FarsightObject.ToString(), _session.Player.GUID.ToString());
            var target = _session.Player.Viewpoint;

            if (target != null)
                _session.Player.SetSeer(target);
            else
                Log.Logger.Debug("Player {0} (GUID: {1}) requests non-existing seer {2}", _session.Player.GetName(), _session.Player.GUID.ToString(), _session.Player.ActivePlayerData.FarsightObject.ToString());
        }
        else
        {
            Log.Logger.Debug("Player {0} set vision to self", _session.Player.GUID.ToString());
            _session.Player.SetSeer(_session.Player);
        }

        _session.Player.UpdateVisibilityForPlayer();
    }

    [WorldPacketHandler(ClientOpcodes.GuildSetFocusedAchievement)]
    private void HandleGuildSetFocusedAchievement(GuildSetFocusedAchievement setFocusedAchievement)
    {
        _guildManager.GetGuildById(_session.Player.GuildId)?.GetAchievementMgr().SendAchievementInfo(_session.Player, setFocusedAchievement.AchievementID);
    }

    [WorldPacketHandler(ClientOpcodes.InstanceLockResponse)]
    private void HandleInstanceLockResponse(InstanceLockResponse packet)
    {
        if (!_session.Player.HasPendingBind)
        {
            Log.Logger.Information("InstanceLockResponse: Player {0} (guid {1}) tried to bind himself/teleport to graveyard without a pending bind!",
                                   _session.Player.GetName(),
                                   _session.Player.GUID.ToString());

            return;
        }

        if (packet.AcceptLock)
            _session.Player.ConfirmPendingBind();
        else
            _session.Player.RepopAtGraveyard();

        _session.Player.SetPendingBind(0, 0);
    }

    [WorldPacketHandler(ClientOpcodes.MountSpecialAnim)]
    private void HandleMountSpecialAnim(MountSpecial mountSpecial)
    {
        SpecialMountAnim specialMountAnim = new()
        {
            UnitGUID = _session.Player.GUID
        };

        specialMountAnim.SpellVisualKitIDs.AddRange(mountSpecial.SpellVisualKitIDs);
        specialMountAnim.SequenceVariation = mountSpecial.SequenceVariation;
        _session.Player.SendMessageToSet(specialMountAnim, false);
    }

    [WorldPacketHandler(ClientOpcodes.NextCinematicCamera)]
    private void HandleNextCinematicCamera(NextCinematicCamera packet)
    {
        if (packet == null)
            return;

        // Sent by client when cinematic actually begun. So we begin the server side process
        _session.Player.CinematicMgr.NextCinematicCamera();
    }

    [WorldPacketHandler(ClientOpcodes.ObjectUpdateFailed, Processing = PacketProcessing.Inplace)]
    private void HandleObjectUpdateFailed(ObjectUpdateFailed objectUpdateFailed)
    {
        Log.Logger.Error("Object update failed for {0} for player {1} ({2})", objectUpdateFailed.ObjectGUID.ToString(), _session.PlayerName, _session.Player.GUID.ToString());

        // If create object failed for current player then client will be stuck on loading screen
        if (_session.Player.GUID == objectUpdateFailed.ObjectGUID)
        {
            _session.LogoutPlayer(true);

            return;
        }

        // Pretend we've never seen this object
        _session.Player.ClientGuiDs.Remove(objectUpdateFailed.ObjectGUID);
    }

    [WorldPacketHandler(ClientOpcodes.ObjectUpdateRescued, Processing = PacketProcessing.Inplace)]
    private void HandleObjectUpdateRescued(ObjectUpdateRescued objectUpdateRescued)
    {
        Log.Logger.Error("Object update rescued for {0} for player {1} ({2})", objectUpdateRescued.ObjectGUID.ToString(), _session.PlayerName, _session.Player.GUID.ToString());

        // Client received values update after destroying object
        // re-register object in m_clientGUIDs to send DestroyObject on next visibility update
        lock (_session.Player.ClientGuiDs)
            _session.Player.ClientGuiDs.Add(objectUpdateRescued.ObjectGUID);
    }

    [WorldPacketHandler(ClientOpcodes.RequestAccountData, Status = SessionStatus.Authed)]
    private void HandleRequestAccountData(RequestAccountData request)
    {
        if (request.DataType > AccountDataTypes.Max)
            return;

        var adata = _session.GetAccountData(request.DataType);

        UpdateAccountData data = new()
        {
            Player = _session.Player?.GUID ?? ObjectGuid.Empty,
            Time = (uint)adata.Time,
            DataType = request.DataType
        };

        if (!adata.Data.IsEmpty())
        {
            data.Size = (uint)adata.Data.Length;
            data.CompressedData = new ByteBuffer(ZLib.Compress(Encoding.UTF8.GetBytes(adata.Data)));
        }

        _session.SendPacket(data);
    }

    [WorldPacketHandler(ClientOpcodes.RequestLatestSplashScreen)]
    private void HandleRequestLatestSplashScreen(RequestLatestSplashScreen requestLatestSplashScreen)
    {
        if (requestLatestSplashScreen == null)
            return;

        UISplashScreenRecord splashScreen = null;

        foreach (var itr in _splashScreenRecords.Values)
        {
            if (_playerConditionRecords.TryGetValue((uint)itr.CharLevelConditionID, out var playerCondition))
                if (!_conditionManager.IsPlayerMeetingCondition(_session.Player, playerCondition))
                    continue;

            splashScreen = itr;
        }

        _session.SendPacket(new SplashScreenShowLatest()
        {
            UISplashScreenID = splashScreen?.Id ?? 0
        });
    }

    [WorldPacketHandler(ClientOpcodes.ResetInstances)]
    private void HandleResetInstances(ResetInstances packet)
    {
        if (packet == null)
            return;

        var map = _session.Player.Location.Map;

        if (map is { Instanceable: true })
            return;

        if (_session.Player.Group != null)
        {
            if (!_session.Player.Group.IsLeader(_session.Player.GUID))
                return;

            if (_session.Player.Group.IsLFGGroup)
                return;

            _session.Player.Group.ResetInstances(InstanceResetMethod.Manual, _session.Player);
        }
        else
            _session.Player.ResetInstances(InstanceResetMethod.Manual);
    }

    [WorldPacketHandler(ClientOpcodes.SaveCufProfiles, Processing = PacketProcessing.Inplace)]
    private void HandleSaveCufProfiles(SaveCUFProfiles packet)
    {
        if (packet.CUFProfiles.Count > PlayerConst.MaxCUFProfiles)
        {
            Log.Logger.Error("HandleSaveCUFProfiles - {0} tried to save more than {1} CUF profiles. Hacking attempt?", _session.PlayerName, PlayerConst.MaxCUFProfiles);

            return;
        }

        for (byte i = 0; i < packet.CUFProfiles.Count; ++i)
            _session.Player.SaveCufProfile(i, packet.CUFProfiles[i]);

        for (var i = (byte)packet.CUFProfiles.Count; i < PlayerConst.MaxCUFProfiles; ++i)
            _session.Player.SaveCufProfile(i, null);
    }

    [WorldPacketHandler(ClientOpcodes.SetActionBarToggles)]
    private void HandleSetActionBarToggles(SetActionBarToggles packet)
    {
        if (_session.Player == null) // ignore until not logged (check needed because STATUS_AUTHED)
        {
            if (packet.Mask != 0)
                Log.Logger.Error("WorldSession.HandleSetActionBarToggles in not logged state with value: {0}, ignored", packet.Mask);

            return;
        }

        _session.Player.SetMultiActionBars(packet.Mask);
    }

    [WorldPacketHandler(ClientOpcodes.SetAdvancedCombatLogging, Processing = PacketProcessing.Inplace)]
    private void HandleSetAdvancedCombatLogging(SetAdvancedCombatLogging setAdvancedCombatLogging)
    {
        _session.Player.SetAdvancedCombatLogging(setAdvancedCombatLogging.Enable);
    }

    [WorldPacketHandler(ClientOpcodes.SetPvp)]
    private void HandleSetPvP(SetPvP packet)
    {
        if (packet.EnablePVP)
        {
            _session.Player.SetPlayerFlag(PlayerFlags.InPVP);
            _session.Player.RemovePlayerFlag(PlayerFlags.PVPTimer);

            if (!_session.Player.IsPvP || _session.Player.PvpInfo.EndTimer != 0)
                _session.Player.UpdatePvP(true, true);
        }
        else if (!_session.Player.IsWarModeLocalActive)
        {
            _session.Player.RemovePlayerFlag(PlayerFlags.InPVP);
            _session.Player.SetPlayerFlag(PlayerFlags.PVPTimer);

            if (!_session.Player.PvpInfo.IsHostile && _session.Player.IsPvP)
                _session.Player.PvpInfo.EndTimer = GameTime.CurrentTime; // start toggle-off
        }
    }

    [WorldPacketHandler(ClientOpcodes.SetSelection)]
    private void HandleSetSelection(SetSelection packet)
    {
        _session.Player.SetSelection(packet.Selection);
    }

    [WorldPacketHandler(ClientOpcodes.SetTaxiBenchmarkMode, Processing = PacketProcessing.Inplace)]
    private void HandleSetTaxiBenchmark(SetTaxiBenchmarkMode packet)
    {
        if (packet.Enable)
            _session.Player.SetPlayerFlag(PlayerFlags.TaxiBenchmark);
        else
            _session.Player.RemovePlayerFlag(PlayerFlags.TaxiBenchmark);
    }

    [WorldPacketHandler(ClientOpcodes.SetTitle, Processing = PacketProcessing.Inplace)]
    private void HandleSetTitle(SetTitle packet)
    {
        // -1 at none
        if (packet.TitleID > 0)
        {
            if (!_session.Player.HasTitle((uint)packet.TitleID))
                return;
        }
        else
            packet.TitleID = 0;

        _session.Player.SetChosenTitle((uint)packet.TitleID);
    }

    [WorldPacketHandler(ClientOpcodes.SetWarMode)]
    private void HandleSetWarMode(SetWarMode packet)
    {
        _session.Player.SetWarModeDesired(packet.Enable);
    }

    [WorldPacketHandler(ClientOpcodes.TogglePvp)]
    private void HandleTogglePvP(TogglePvP packet)
    {
        if (packet == null)
            return;

        if (!_session.Player.HasPlayerFlag(PlayerFlags.InPVP))
        {
            _session.Player.SetPlayerFlag(PlayerFlags.InPVP);
            _session.Player.RemovePlayerFlag(PlayerFlags.PVPTimer);

            if (!_session.Player.IsPvP || _session.Player.PvpInfo.EndTimer != 0)
                _session.Player.UpdatePvP(true, true);
        }
        else if (!_session.Player.IsWarModeLocalActive)
        {
            _session.Player.RemovePlayerFlag(PlayerFlags.InPVP);
            _session.Player.SetPlayerFlag(PlayerFlags.PVPTimer);

            if (!_session.Player.PvpInfo.IsHostile && _session.Player.IsPvP)
                _session.Player.PvpInfo.EndTimer = GameTime.CurrentTime; // start toggle-off
        }
    }

    [WorldPacketHandler(ClientOpcodes.ChatUnregisterAllAddonPrefixes)]
    private void HandleUnregisterAllAddonPrefixes(ChatUnregisterAllAddonPrefixes packet)
    {
        if (packet == null)
            return;

        _session.ClearRegisteredAddons();
    }

    [WorldPacketHandler(ClientOpcodes.ViolenceLevel, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
    private void HandleViolenceLevel(ViolenceLevel violenceLevel)
    {
        if (violenceLevel == null)
        {
        }
        // do something?
    }

    [WorldPacketHandler(ClientOpcodes.Warden3Data)]
    private void HandleWarden3Data(WardenData packet)
    {
        if (_warden == null || packet.Data.GetSize() == 0)
            return;

        _warden.DecryptData(packet.Data.GetData());
        var opcode = (WardenOpcodes)packet.Data.ReadUInt8();

        switch (opcode)
        {
            case WardenOpcodes.CmsgModuleMissing:
                _warden.SendModuleToClient();

                break;

            case WardenOpcodes.CmsgModuleOk:
                _warden.RequestHash();

                break;

            case WardenOpcodes.SmsgCheatChecksRequest:
                _warden.HandleData(packet.Data);

                break;

            case WardenOpcodes.CmsgMemChecksResult:
                Log.Logger.Debug("NYI WARDEN_CMSG_MEM_CHECKS_RESULT received!");

                break;

            case WardenOpcodes.CmsgHashResult:
                _warden.HandleHashResult(packet.Data);
                _warden.InitializeModule();

                break;

            case WardenOpcodes.CmsgModuleFailed:
                Log.Logger.Debug("NYI WARDEN_CMSG_MODULE_FAILED received!");

                break;

            default:
                Log.Logger.Debug("Got unknown warden opcode {0} of size {1}.", opcode, packet.Data.GetSize() - 1);

                break;
        }
    }
}