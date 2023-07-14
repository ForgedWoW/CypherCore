// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Cache;
using Forged.MapServer.Calendar;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Guilds;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Calendar;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class CalendarHandler : IWorldSessionHandler
{
    private readonly CalendarManager _calendarManager;
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly GuildManager _guildManager;
    private readonly InstanceLockManager _instanceLockManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldSession _session;

    public CalendarHandler(WorldSession session, CalendarManager calendarManager, InstanceLockManager instanceLockManager,
                           GuildManager guildManager, CharacterDatabase characterDatabase, GameObjectManager gameObjectManager,
                           ObjectAccessor objectAccessor, CharacterCache characterCache, IConfiguration configuration, CliDB cliDB,
                           DB2Manager db2Manager)
    {
        _session = session;
        _calendarManager = calendarManager;
        _instanceLockManager = instanceLockManager;
        _guildManager = guildManager;
        _characterDatabase = characterDatabase;
        _gameObjectManager = gameObjectManager;
        _objectAccessor = objectAccessor;
        _characterCache = characterCache;
        _configuration = configuration;
        _cliDB = cliDB;
        _db2Manager = db2Manager;
    }

    public void SendCalendarRaidLockoutAdded(InstanceLock instanceLock)
    {
        _session.SendPacket(new CalendarRaidLockoutAdded()
        {
            InstanceID = instanceLock.GetInstanceId(),
            ServerTime = (uint)GameTime.CurrentTime,
            MapID = (int)instanceLock.GetMapId(),
            DifficultyID = instanceLock.GetDifficultyId(),
            TimeRemaining = (int)(instanceLock.GetEffectiveExpiryTime() - GameTime.SystemTime).TotalSeconds
        });
    }

    [WorldPacketHandler(ClientOpcodes.CalendarAddEvent)]
    private void HandleCalendarAddEvent(CalendarAddEvent calendarAddEvent)
    {
        var guid = _session.Player.GUID;

        calendarAddEvent.EventInfo.Time = Time.LocalTimeToUtcTime(calendarAddEvent.EventInfo.Time);

        // prevent events in the past
        // To Do: properly handle timezones and remove the "- time_t(86400L)" hack
        if (calendarAddEvent.EventInfo.Time < (GameTime.CurrentTime - 86400L))
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventPassed);

            return;
        }

        // If the event is a guild event, check if the player is in a guild
        if (CalendarEvent.ModifyIsGuildEventFlags(calendarAddEvent.EventInfo.Flags) || CalendarEvent.ModifyIsGuildAnnouncementFlags(calendarAddEvent.EventInfo.Flags))
            if (_session.Player.GuildId == 0)
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.GuildPlayerNotInGuild);

                return;
            }

        // Check if the player reached the max number of events allowed to create
        if (CalendarEvent.ModifyIsGuildEventFlags(calendarAddEvent.EventInfo.Flags) || CalendarEvent.ModifyIsGuildAnnouncementFlags(calendarAddEvent.EventInfo.Flags))
        {
            if (_calendarManager.GetGuildEvents(_session.Player.GuildId).Count >= SharedConst.CalendarMaxGuildEvents)
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.GuildEventsExceeded);

                return;
            }
        }
        else
        {
            if (_calendarManager.GetEventsCreatedBy(guid).Count >= SharedConst.CalendarMaxEvents)
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventsExceeded);

                return;
            }
        }

        if (_session.CalendarEventCreationCooldown > GameTime.CurrentTime)
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.Internal);

            return;
        }

        _session.CalendarEventCreationCooldown = GameTime.CurrentTime + SharedConst.CalendarCreateEventCooldown;

        CalendarEvent calendarEvent = new(_calendarManager.GetFreeEventId(),
                                        guid,
                                        0,
                                        (CalendarEventType)calendarAddEvent.EventInfo.EventType,
                                        calendarAddEvent.EventInfo.TextureID,
                                        calendarAddEvent.EventInfo.Time,
                                        (CalendarFlags)calendarAddEvent.EventInfo.Flags,
                                        calendarAddEvent.EventInfo.Title,
                                        calendarAddEvent.EventInfo.Description,
                                        0);

        if (calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement)
            calendarEvent.GuildId = _session.Player.GuildId;

        if (calendarEvent.IsGuildAnnouncement)
        {
            CalendarInvite invite = new(0, calendarEvent.EventId, ObjectGuid.Empty, guid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.NotSignedUp, CalendarModerationRank.Player, "");
            // WARNING: By passing pointer to a local variable, the underlying method(s) must NOT perform any kind
            // of storage of the pointer as it will lead to memory corruption
            _calendarManager.AddInvite(calendarEvent, invite);
        }
        else
        {
            SQLTransaction trans = null;

            if (calendarAddEvent.EventInfo.Invites.Length > 1)
                trans = new SQLTransaction();

            for (var i = 0; i < calendarAddEvent.EventInfo.Invites.Length; ++i)
            {
                CalendarInvite invite = new(_calendarManager.GetFreeInviteId(),
                                            calendarEvent.EventId,
                                            calendarAddEvent.EventInfo.Invites[i].Guid,
                                            guid,
                                            SharedConst.CalendarDefaultResponseTime,
                                            (CalendarInviteStatus)calendarAddEvent.EventInfo.Invites[i].Status,
                                            (CalendarModerationRank)calendarAddEvent.EventInfo.Invites[i].Moderator,
                                            "");

                _calendarManager.AddInvite(calendarEvent, invite, trans);
            }

            if (calendarAddEvent.EventInfo.Invites.Length > 1)
                _characterDatabase.CommitTransaction(trans);
        }

        _calendarManager.AddEvent(calendarEvent, CalendarSendEventType.Add);
    }

    [WorldPacketHandler(ClientOpcodes.CalendarCommunityInvite)]
    private void HandleCalendarCommunityInvite(CalendarCommunityInviteRequest calendarCommunityInvite)
    {
        _guildManager.GetGuildById(_session.Player.GuildId)?.MassInviteToEvent(_session, calendarCommunityInvite.MinLevel, calendarCommunityInvite.MaxLevel, (GuildRankOrder)calendarCommunityInvite.MaxRankOrder);
    }

    [WorldPacketHandler(ClientOpcodes.CalendarComplain)]
    private void HandleCalendarComplain(CalendarComplain calendarComplain)
    {
        if (calendarComplain != null)
        {
        }
        // what to do with complains?
    }

    [WorldPacketHandler(ClientOpcodes.CalendarCopyEvent)]
    private void HandleCalendarCopyEvent(CalendarCopyEvent calendarCopyEvent)
    {
        var guid = _session.Player.GUID;

        calendarCopyEvent.Date = Time.LocalTimeToUtcTime(calendarCopyEvent.Date);

        // prevent events in the past
        // To Do: properly handle timezones and remove the "- time_t(86400L)" hack
        if (calendarCopyEvent.Date < (GameTime.CurrentTime - 86400L))
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventPassed);

            return;
        }

        var oldEvent = _calendarManager.GetEvent(calendarCopyEvent.EventID);

        if (oldEvent != null)
        {
            // Ensure that the player has access to the event
            if (oldEvent.IsGuildEvent || oldEvent.IsGuildAnnouncement)
            {
                if (oldEvent.GuildId != _session.Player.GuildId)
                {
                    _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);

                    return;
                }
            }
            else
            {
                if (oldEvent.OwnerGuid != guid)
                {
                    _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);

                    return;
                }
            }

            // Check if the player reached the max number of events allowed to create
            if (oldEvent.IsGuildEvent || oldEvent.IsGuildAnnouncement)
            {
                if (_calendarManager.GetGuildEvents(_session.Player.GuildId).Count >= SharedConst.CalendarMaxGuildEvents)
                {
                    _calendarManager.SendCalendarCommandResult(guid, CalendarError.GuildEventsExceeded);

                    return;
                }
            }
            else
            {
                if (_calendarManager.GetEventsCreatedBy(guid).Count >= SharedConst.CalendarMaxEvents)
                {
                    _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventsExceeded);

                    return;
                }
            }

            if (_session.CalendarEventCreationCooldown > GameTime.CurrentTime)
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.Internal);

                return;
            }

            _session.CalendarEventCreationCooldown = GameTime.CurrentTime + SharedConst.CalendarCreateEventCooldown;

            CalendarEvent newEvent = new(oldEvent, _calendarManager.GetFreeEventId())
            {
                Date = calendarCopyEvent.Date
            };

            _calendarManager.AddEvent(newEvent, CalendarSendEventType.Copy);

            var invites = _calendarManager.GetEventInvites(calendarCopyEvent.EventID);
            SQLTransaction trans = null;

            if (invites.Count > 1)
                trans = new SQLTransaction();

            foreach (var invite in invites)
                _calendarManager.AddInvite(newEvent, new CalendarInvite(invite, _calendarManager.GetFreeInviteId(), newEvent.EventId), trans);

            if (invites.Count > 1)
                _characterDatabase.CommitTransaction(trans);
            // should we change owner when somebody makes a copy of event owned by another person?
        }
        else
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }
    }

    [WorldPacketHandler(ClientOpcodes.CalendarRemoveInvite)]
    private void HandleCalendarEventRemoveInvite(CalendarRemoveInvite calendarRemoveInvite)
    {
        var guid = _session.Player.GUID;

        var calendarEvent = _calendarManager.GetEvent(calendarRemoveInvite.EventID);

        if (calendarEvent != null)
        {
            if (calendarEvent.OwnerGuid == calendarRemoveInvite.Guid)
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.DeleteCreatorFailed);

                return;
            }

            _calendarManager.RemoveInvite(calendarRemoveInvite.InviteID, calendarRemoveInvite.EventID, guid);
        }
        else
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.NoInvite);
        }
    }

    [WorldPacketHandler(ClientOpcodes.CalendarEventSignUp)]
    private void HandleCalendarEventSignup(CalendarEventSignUp calendarEventSignUp)
    {
        var guid = _session.Player.GUID;

        var calendarEvent = _calendarManager.GetEvent(calendarEventSignUp.EventID);

        if (calendarEvent != null)
        {
            if (calendarEvent.IsGuildEvent && calendarEvent.GuildId != _session.Player.GuildId)
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.GuildPlayerNotInGuild);

                return;
            }

            var status = calendarEventSignUp.Tentative ? CalendarInviteStatus.Tentative : CalendarInviteStatus.SignedUp;
            CalendarInvite invite = new(_calendarManager.GetFreeInviteId(), calendarEventSignUp.EventID, guid, guid, GameTime.CurrentTime, status, CalendarModerationRank.Player, "");
            _calendarManager.AddInvite(calendarEvent, invite);
            _calendarManager.SendCalendarClearPendingAction(guid);
        }
        else
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }
    }

    [WorldPacketHandler(ClientOpcodes.CalendarGet)]
    private void HandleCalendarGetCalendar(CalendarGetCalendar calendarGetCalendar)
    {
        if (calendarGetCalendar == null)
            return;

        var guid = _session.Player.GUID;

        var currTime = GameTime.CurrentTime;

        CalendarSendCalendar packet = new()
        {
            ServerTime = currTime
        };

        var invites = _calendarManager.GetPlayerInvites(guid);

        foreach (var invite in invites)
        {
            CalendarSendCalendarInviteInfo inviteInfo = new()
            {
                EventID = invite.EventId,
                InviteID = invite.InviteId,
                InviterGuid = invite.SenderGuid,
                Status = invite.Status,
                Moderator = invite.Rank
            };

            var calendarEvent = _calendarManager.GetEvent(invite.EventId);

            if (calendarEvent != null)
                inviteInfo.InviteType = (byte)(calendarEvent.IsGuildEvent && calendarEvent.GuildId == _session.Player.GuildId ? 1 : 0);

            packet.Invites.Add(inviteInfo);
        }

        var playerEvents = _calendarManager.GetPlayerEvents(guid);

        foreach (var calendarEvent in playerEvents)
        {
            CalendarSendCalendarEventInfo eventInfo;
            eventInfo.EventID = calendarEvent.EventId;
            eventInfo.Date = calendarEvent.Date;
            eventInfo.EventClubID = calendarEvent.GuildId;
            eventInfo.EventName = calendarEvent.Title;
            eventInfo.EventType = calendarEvent.EventType;
            eventInfo.Flags = calendarEvent.Flags;
            eventInfo.OwnerGuid = calendarEvent.OwnerGuid;
            eventInfo.TextureID = calendarEvent.TextureId;

            packet.Events.Add(eventInfo);
        }

        foreach (var instanceLock in _instanceLockManager.GetInstanceLocksForPlayer(_session.Player.GUID))
        {
            packet.RaidLockouts.Add(new CalendarSendCalendarRaidLockoutInfo()
            {
                MapID = (int)instanceLock.GetMapId(),
                DifficultyID = (uint)instanceLock.GetDifficultyId(),
                ExpireTime = (int)Math.Max((instanceLock.GetEffectiveExpiryTime() - GameTime.SystemTime).TotalSeconds, 0),
                InstanceID = instanceLock.GetInstanceId()
            });
        }

        _session.SendPacket(packet);
    }

    [WorldPacketHandler(ClientOpcodes.CalendarGetEvent)]
    private void HandleCalendarGetEvent(CalendarGetEvent calendarGetEvent)
    {
        var calendarEvent = _calendarManager.GetEvent(calendarGetEvent.EventID);

        if (calendarEvent != null)
            _calendarManager.SendCalendarEvent(_session.Player.GUID, calendarEvent, CalendarSendEventType.Get);
        else
            _calendarManager.SendCalendarCommandResult(_session.Player.GUID, CalendarError.EventInvalid);
    }

    [WorldPacketHandler(ClientOpcodes.CalendarGetNumPending)]
    private void HandleCalendarGetNumPending(CalendarGetNumPending calendarGetNumPending)
    {
        if (calendarGetNumPending == null)
            return;

        var guid = _session.Player.GUID;
        var pending = _calendarManager.GetPlayerNumPending(guid);

        _session.SendPacket(new CalendarSendNumPending(pending));
    }

    [WorldPacketHandler(ClientOpcodes.CalendarInvite)]
    private void HandleCalendarInvite(CalendarInvitePkt calendarInvite)
    {
        var playerGuid = _session.Player.GUID;

        var inviteeGuid = ObjectGuid.Empty;
        TeamFaction inviteeTeam = 0;
        ulong inviteeGuildId = 0;

        if (!_gameObjectManager.NormalizePlayerName(ref calendarInvite.Name))
            return;

        var player = _objectAccessor.FindPlayerByName(calendarInvite.Name);

        if (player != null)
        {
            // Invitee is online
            inviteeGuid = player.GUID;
            inviteeTeam = player.Team;
            inviteeGuildId = player.GuildId;
        }
        else
        {
            // Invitee offline, get data from database
            var guid = _characterCache.GetCharacterGuidByName(calendarInvite.Name);

            if (!guid.IsEmpty)
            {
                var characterInfo = _characterCache.GetCharacterCacheByGuid(guid);

                if (characterInfo != null)
                {
                    inviteeGuid = guid;
                    inviteeTeam = _session.Player.TeamForRace(characterInfo.RaceId);
                    inviteeGuildId = characterInfo.GuildId;
                }
            }
        }

        if (inviteeGuid.IsEmpty)
        {
            _calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.PlayerNotFound);

            return;
        }

        if (_session.Player.Team != inviteeTeam && !_configuration.GetDefaultValue("AllowTwoSide:Interaction:Calendar", false))
        {
            _calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.NotAllied);

            return;
        }

        var result1 = _characterDatabase.Query("SELECT flags FROM character_social WHERE guid = {0} AND friend = {1}", inviteeGuid, playerGuid);

        if (!result1.IsEmpty())
            if (Convert.ToBoolean(result1.Read<byte>(0) & (byte)SocialFlag.Ignored))
            {
                _calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.IgnoringYouS, calendarInvite.Name);

                return;
            }

        if (!calendarInvite.Creating)
        {
            var calendarEvent = _calendarManager.GetEvent(calendarInvite.EventID);

            if (calendarEvent != null)
            {
                if (calendarEvent.IsGuildEvent && calendarEvent.GuildId == inviteeGuildId)
                {
                    // we can't invite guild members to guild events
                    _calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);

                    return;
                }

                CalendarInvite invite = new(_calendarManager.GetFreeInviteId(), calendarInvite.EventID, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
                _calendarManager.AddInvite(calendarEvent, invite);
            }
            else
            {
                _calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.EventInvalid);
            }
        }
        else
        {
            if (calendarInvite.IsSignUp && inviteeGuildId == _session.Player.GuildId)
            {
                _calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);

                return;
            }

            CalendarInvite invite = new(calendarInvite.EventID, 0, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
            _calendarManager.SendCalendarEventInvite(invite);
        }
    }

    [WorldPacketHandler(ClientOpcodes.CalendarModeratorStatus)]
    private void HandleCalendarModeratorStatus(CalendarModeratorStatusQuery calendarModeratorStatus)
    {
        var guid = _session.Player.GUID;

        var calendarEvent = _calendarManager.GetEvent(calendarModeratorStatus.EventID);

        if (calendarEvent != null)
        {
            var invite = _calendarManager.GetInvite(calendarModeratorStatus.InviteID);

            if (invite != null)
            {
                invite.Rank = (CalendarModerationRank)calendarModeratorStatus.Status;
                _calendarManager.UpdateInvite(invite);
                _calendarManager.SendCalendarEventModeratorStatusAlert(calendarEvent, invite);
            }
            else
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
            }
        }
        else
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }
    }

    [WorldPacketHandler(ClientOpcodes.CalendarRemoveEvent)]
    private void HandleCalendarRemoveEvent(CalendarRemoveEvent calendarRemoveEvent)
    {
        var guid = _session.Player.GUID;
        _calendarManager.RemoveEvent(calendarRemoveEvent.EventID, guid);
    }

    [WorldPacketHandler(ClientOpcodes.CalendarRsvp)]
    private void HandleCalendarRsvp(HandleCalendarRsvp calendarRSVP)
    {
        var guid = _session.Player.GUID;

        var calendarEvent = _calendarManager.GetEvent(calendarRSVP.EventID);

        if (calendarEvent != null)
        {
            // i think we still should be able to remove self from locked events
            if (calendarRSVP.Status != CalendarInviteStatus.Removed && calendarEvent.IsLocked)
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventLocked);

                return;
            }

            var invite = _calendarManager.GetInvite(calendarRSVP.InviteID);

            if (invite != null)
            {
                invite.Status = calendarRSVP.Status;
                invite.ResponseTime = GameTime.CurrentTime;

                _calendarManager.UpdateInvite(invite);
                _calendarManager.SendCalendarEventStatus(calendarEvent, invite);
                _calendarManager.SendCalendarClearPendingAction(guid);
            }
            else
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
            }
        }
        else
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }
    }

    [WorldPacketHandler(ClientOpcodes.CalendarStatus)]
    private void HandleCalendarStatus(CalendarStatus calendarStatus)
    {
        var guid = _session.Player.GUID;

        var calendarEvent = _calendarManager.GetEvent(calendarStatus.EventID);

        if (calendarEvent != null)
        {
            var invite = _calendarManager.GetInvite(calendarStatus.InviteID);

            if (invite != null)
            {
                invite.Status = (CalendarInviteStatus)calendarStatus.Status;

                _calendarManager.UpdateInvite(invite);
                _calendarManager.SendCalendarEventStatus(calendarEvent, invite);
                _calendarManager.SendCalendarClearPendingAction(calendarStatus.Guid);
            }
            else
            {
                _calendarManager.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
            }
        }
        else
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }
    }

    [WorldPacketHandler(ClientOpcodes.CalendarUpdateEvent)]
    private void HandleCalendarUpdateEvent(CalendarUpdateEvent calendarUpdateEvent)
    {
        var guid = _session.Player.GUID;

        calendarUpdateEvent.EventInfo.Time = Time.LocalTimeToUtcTime(calendarUpdateEvent.EventInfo.Time);

        // prevent events in the past
        // To Do: properly handle timezones and remove the "- time_t(86400L)" hack
        if (calendarUpdateEvent.EventInfo.Time < (GameTime.CurrentTime - 86400L))
            return;

        var calendarEvent = _calendarManager.GetEvent(calendarUpdateEvent.EventInfo.EventID);

        if (calendarEvent != null)
        {
            var oldEventTime = calendarEvent.Date;

            calendarEvent.EventType = (CalendarEventType)calendarUpdateEvent.EventInfo.EventType;
            calendarEvent.Flags = (CalendarFlags)calendarUpdateEvent.EventInfo.Flags;
            calendarEvent.Date = calendarUpdateEvent.EventInfo.Time;
            calendarEvent.TextureId = (int)calendarUpdateEvent.EventInfo.TextureID;
            calendarEvent.Title = calendarUpdateEvent.EventInfo.Title;
            calendarEvent.Description = calendarUpdateEvent.EventInfo.Description;

            _calendarManager.UpdateEvent(calendarEvent);
            _calendarManager.SendCalendarEventUpdateAlert(calendarEvent, oldEventTime);
        }
        else
        {
            _calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }
    }

    [WorldPacketHandler(ClientOpcodes.SetSavedInstanceExtend)]
    private void HandleSetSavedInstanceExtend(SetSavedInstanceExtend setSavedInstanceExtend)
    {
        // cannot modify locks currently in use
        if (_session.Player.Location.MapId == setSavedInstanceExtend.MapID)
            return;

        var expiryTimes = _instanceLockManager.UpdateInstanceLockExtensionForPlayer(_session.Player.GUID, new MapDb2Entries((uint)setSavedInstanceExtend.MapID, (Difficulty)setSavedInstanceExtend.DifficultyID, _cliDB, _db2Manager), setSavedInstanceExtend.Extend);

        if (expiryTimes.Item1 == DateTime.MinValue)
            return;

        CalendarRaidLockoutUpdated calendarRaidLockoutUpdated = new()
        {
            ServerTime = GameTime.CurrentTime,
            MapID = setSavedInstanceExtend.MapID,
            DifficultyID = setSavedInstanceExtend.DifficultyID,
            OldTimeRemaining = (int)Math.Max((expiryTimes.Item1 - GameTime.SystemTime).TotalSeconds, 0),
            NewTimeRemaining = (int)Math.Max((expiryTimes.Item2 - GameTime.SystemTime).TotalSeconds, 0)
        };

        _session.SendPacket(calendarRaidLockoutUpdated);
    }

    private void SendCalendarRaidLockoutRemoved(InstanceLock instanceLock)
    {
        CalendarRaidLockoutRemoved calendarRaidLockoutRemoved = new()
        {
            InstanceID = instanceLock.GetInstanceId(),
            MapID = (int)instanceLock.GetMapId(),
            DifficultyID = instanceLock.GetDifficultyId()
        };

        _session.SendPacket(calendarRaidLockoutRemoved);
    }
}