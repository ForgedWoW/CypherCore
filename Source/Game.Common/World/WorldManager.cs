// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Game.Common.Entities.Objects;
using Game.Common.Handlers;
using Game.Common.Networking;
using Game.Common.Scripting;
using Game.Common.Scripting.Interfaces.ISession;
using Game.Common.Server;

namespace Game.Common.World;

public class WorldManager
{
    private readonly AuthenticationHandler _authenticationHandler;
    private readonly ScriptManager _scriptManager;
    readonly ConcurrentDictionary<uint, WorldSession> _sessions = new();
    readonly MultiMap<ObjectGuid, WorldSession> _sessionsByBnetGuid = new();
    readonly Dictionary<uint, long> _disconnects = new();
    readonly List<WorldSession> _queuedPlayer = new();
    readonly ConcurrentQueue<WorldSession> _addSessQueue = new();

    public List<WorldSession> AllSessions => _sessions.Values.ToList();

    public int ActiveAndQueuedSessionCount => _sessions.Count;

    public int ActiveSessionCount => _sessions.Count - _queuedPlayer.Count;

    public int QueuedSessionCount => _queuedPlayer.Count;

    // Get the maximum number of parallel sessions on the server since last reboot
    public uint MaxQueuedSessionCount { get; private set; }

    public uint MaxActiveSessionCount { get; private set; }


    public uint PlayerAmountLimit { get; set; }

    public Realm Realm { get; } =  new();

    public RealmId RealmId => Realm.Id;


    public WorldManager(AuthenticationHandler authenticationHandler, ScriptManager scriptManager)
    {
        _authenticationHandler = authenticationHandler;
        _scriptManager = scriptManager;
    }

    public WorldSession FindSession(uint id)
    {
        return _sessions.LookupByKey(id);
    }

    public void AddSession(WorldSession s)
    {
        _addSessQueue.Enqueue(s);
    }

    public bool LoadRealmInfo()
    {
        var result = DB.Login.Query("SELECT id, name, address, localAddress, localSubnetMask, port, icon, flag, timezone, allowedSecurityLevel, population, gamebuild, Region, Battlegroup FROM realmlist WHERE id = {0}", Realm.Id.Index);

        if (result.IsEmpty())
            return false;

        Realm.SetName(result.Read<string>(1));
        Realm.ExternalAddress = System.Net.IPAddress.Parse(result.Read<string>(2));
        Realm.LocalAddress = System.Net.IPAddress.Parse(result.Read<string>(3));
        Realm.LocalSubnetMask = System.Net.IPAddress.Parse(result.Read<string>(4));
        Realm.Port = result.Read<ushort>(5);
        Realm.Type = result.Read<byte>(6);
        Realm.Flags = (RealmFlags)result.Read<byte>(7);
        Realm.Timezone = result.Read<byte>(8);
        Realm.AllowedSecurityLevel = (AccountTypes)result.Read<byte>(9);
        Realm.PopulationLevel = result.Read<float>(10);
        Realm.Id.Region = result.Read<byte>(12);
        Realm.Id.Site = result.Read<byte>(13);
        Realm.Build = result.Read<uint>(11);

        return true;
    }
    
    public void SendGlobalMessage(ServerPacket packet, WorldSession self = null, TeamFaction team = 0)
    {
        foreach (var session in _sessions.Values)
            if (session.Player != null &&
                session.Player.IsInWorld &&
                session != self &&
                (team == 0 || session.Player.Team == team))
                session.SendPacket(packet);
    }

    public void SendGlobalGMMessage(ServerPacket packet, WorldSession self = null, TeamFaction team = 0)
    {
        foreach (var session in _sessions.Values)
        {
            // check if session and can receive global GM Messages and its not self
            if (session == null || session == self || !session.HasPermission(RBACPermissions.ReceiveGlobalGmTextmessage))
                continue;

            // Player should be in world
            var player = session.Player;

            if (player == null || !player.IsInWorld)
                continue;

            // Send only to same team, if team is given
            if (team == 0 || player.Team == team)
                session.SendPacket(packet);
        }
    }

    public void KickAll()
    {
        _queuedPlayer.Clear(); // prevent send queue update packet and login queued sessions

        // session not removed at kick and will removed in next update tick
        foreach (var session in _sessions.Values)
            session.KickPlayer("World::KickAll");
    }

    public void Update(uint diff)
    {
        // Add new sessions
        while (_addSessQueue.TryDequeue(out var sess))
            AddSession_(sess);

        // Then send an update signal to remaining ones
        foreach (var pair in _sessions)
        {
            var session = pair.Value;

            if (!session.UpdateWorld(diff)) // As interval = 0
            {
                if (!RemoveQueuedPlayer(session) && WorldConfig.GetIntValue(WorldCfg.IntervalDisconnectTolerance) != 0)
                    _disconnects[session.AccountId] = GameTime.GetGameTime();

                RemoveQueuedPlayer(session);
                _sessions.TryRemove(pair.Key, out _);
                _sessionsByBnetGuid.Remove(session.BattlenetAccountGUID, session);
                session.Dispose();
            }
        }
    }

    bool RemoveSession(uint id)
    {
        // Find the session, kick the user, but we can't delete session at this moment to prevent iterator invalidation
        var session = _sessions.LookupByKey(id);

        if (session != null)
        {
            if (session.IsPlayerLoading)
                return false;

            session.KickPlayer("World::RemoveSession");
        }

        return true;
    }

    void AddSession_(WorldSession s)
    {
        //NOTE - Still there is race condition in WorldSession* being used in the Sockets

        // kick already loaded player with same account (if any) and remove session
        // if player is in loading and want to load again, return
        if (!RemoveSession(s.AccountId))
        {
            s.KickPlayer("World::AddSession_ Couldn't remove the other session while on loading screen");

            return;
        }

        // decrease session counts only at not reconnection case
        var decreaseSession = true;

        // if session already exist, prepare to it deleting at next world update
        // NOTE - KickPlayer() should be called on "old" in RemoveSession()
        {
            var old = _sessions.LookupByKey(s.AccountId);

            if (old != null)
            {
                // prevent decrease sessions count if session queued
                if (RemoveQueuedPlayer(old))
                    decreaseSession = false;

                _sessionsByBnetGuid.Remove(old.BattlenetAccountGUID, old);
                old.Dispose();
            }
        }

        _sessions[s.AccountId] = s;
        _sessionsByBnetGuid.Add(s.BattlenetAccountGUID, s);

        var sessions = ActiveAndQueuedSessionCount;
        var pLimit = PlayerAmountLimit;
        var queueSize = QueuedSessionCount; //number of players in the queue

        //so we don't count the user trying to
        //login as a session and queue the socket that we are using
        if (decreaseSession)
            --sessions;

        if (pLimit > 0 && sessions >= pLimit && !s.HasPermission(RBACPermissions.SkipQueue) && !HasRecentlyDisconnected(s))
        {
            AddQueuedPlayer(s);
            UpdateMaxSessionCounters();
            Log.outInfo(LogFilter.Server, "PlayerQueue: Account id {0} is in Queue Position ({1}).", s.AccountId, ++queueSize);

            return;
        }

        UpdateMaxSessionCounters();

        _scriptManager.ForEach<ISessionInitialize>(si => si.Initialize(this, s));

        // Updates the population
        if (pLimit > 0)
        {
            float popu = ActiveSessionCount; // updated number of users on the server
            popu /= pLimit;
            popu *= 2;
            Log.outInfo(LogFilter.Server, "Server Population ({0}).", popu);
        }
    }

    bool HasRecentlyDisconnected(WorldSession session)
    {
        if (session == null)
            return false;

        foreach (var disconnect in _disconnects)
            _disconnects.Remove(disconnect.Key);
 
        return false;
    }

    uint GetQueuePos(WorldSession sess)
    {
        uint position = 1;

        foreach (var iter in _queuedPlayer)
            if (iter != sess)
                ++position;
            else
                return position;

        return 0;
    }

    void AddQueuedPlayer(WorldSession sess)
    {
        sess.SetInQueue(true);
        _queuedPlayer.Add(sess);

        // The 1st SMSG_AUTH_RESPONSE needs to contain other info too.
        _authenticationHandler.SendAuthResponse(BattlenetRpcErrorCode.Ok, true, GetQueuePos(sess));
    }

    bool RemoveQueuedPlayer(WorldSession sess)
    {
        // sessions count including queued to remove (if removed_session set)
        var sessions = ActiveSessionCount;

        uint position = 1;

        // search to remove and count skipped positions
        var found = false;

        foreach (var iter in _queuedPlayer)
            if (iter != sess)
            {
                ++position;
            }
            else
            {
                sess.SetInQueue(false);
                sess.ResetTimeOutTime(false);
                _queuedPlayer.Remove(iter);
                found = true; // removing queued session

                break;
            }

        // iter point to next socked after removed or end()
        // position store position of removed socket and then new position next socket after removed

        // if session not queued then we need decrease sessions count
        if (!found && sessions != 0)
            --sessions;

        // accept first in queue
        if ((PlayerAmountLimit == 0 || sessions < PlayerAmountLimit) && !_queuedPlayer.Empty())
        {
            var popSess = _queuedPlayer.First();
            _scriptManager.ForEach<ISessionInitialize>(si => si.Initialize(this, popSess));

            _queuedPlayer.RemoveAt(0);

            // update iter to point first queued socket or end() if queue is empty now
            position = 1;
        }

        // update position from iter to end()
        // iter point to first not updated socket, position store new position
        foreach (var iter in _queuedPlayer)
            _authenticationHandler.SendAuthWaitQueue(++position);

        return found;
    }

    void KickAllLess(AccountTypes sec)
    {
        // session not removed at kick and will removed in next update tick
        foreach (var session in _sessions.Values)
            if (session.Security < sec)
                session.KickPlayer("World::KickAllLess");
    }
    
    void UpdateMaxSessionCounters()
    {
        MaxActiveSessionCount = Math.Max(MaxActiveSessionCount, (uint)(_sessions.Count - _queuedPlayer.Count));
        MaxQueuedSessionCount = Math.Max(MaxQueuedSessionCount, (uint)_queuedPlayer.Count);
    }
}