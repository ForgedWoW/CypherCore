// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Taxis;
using Forged.MapServer.Globals;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Taxi;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace Forged.MapServer.OpCodeHandlers;

public class TaxiHandler : IWorldSessionHandler
{
    private readonly CliDB _cliDB;
    private readonly ConditionManager _conditionManager;
    private readonly DB2Manager _db2Manager;
    private readonly GameObjectManager _objectManager;
    private readonly WorldSession _session;
    private readonly TaxiPathGraph _taxiPathGraph;

    public TaxiHandler(WorldSession session, GameObjectManager objectManager, CliDB cliDB, TaxiPathGraph taxiPathGraph, DB2Manager db2Manager,
                       ConditionManager conditionManager)
    {
        _session = session;
        _objectManager = objectManager;
        _cliDB = cliDB;
        _taxiPathGraph = taxiPathGraph;
        _db2Manager = db2Manager;
        _conditionManager = conditionManager;
    }

    public void SendActivateTaxiReply(ActivateTaxiReply reply = ActivateTaxiReply.Ok)
    {
        _session.SendPacket(new ActivateTaxiReplyPkt()
        {
            Reply = reply
        });
    }

    public void SendDiscoverNewTaxiNode(uint nodeid)
    {
        if (_session.Player.Taxi.SetTaximaskNode(nodeid))
            _session.SendPacket(new NewTaxiPath());
    }

    public void SendDoFlight(uint mountDisplayId, uint path, uint pathNode = 0)
    {
        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        if (mountDisplayId != 0)
            _session.Player.Mount(mountDisplayId);

        _session.Player.MotionMaster.MoveTaxiFlight(path, pathNode);
    }

    public bool SendLearnNewTaxiNode(Creature unit)
    {
        // find current node
        var curloc = _objectManager.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, _session.Player.Team);

        if (curloc == 0)
            return true;

        if (!_session.Player.Taxi.SetTaximaskNode(curloc))
            return false;

        _session.SendPacket(new NewTaxiPath());
        _session.SendPacket(new TaxiNodeStatusPkt()
        {
            Unit = unit.GUID,
            Status = TaxiNodeStatus.Learned
        });

        return true;
    }

    public void SendTaxiMenu(Creature unit)
    {
        // find current node
        var curloc = _objectManager.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, _session.Player.Team);

        if (curloc == 0)
            return;

        var lastTaxiCheaterState = _session.Player.IsTaxiCheater;

        if (unit.Entry == 29480)
            _session.Player.SetTaxiCheater(true); // Grimwing in Ebon Hold, special case. NOTE: Not perfect, Zul'Aman should not be included according to WoWhead, and I think taxicheat includes it.

        ShowTaxiNodes data = new();
        ShowTaxiNodesWindowInfo windowInfo = new()
        {
            UnitGUID = unit.GUID,
            CurrentNode = (int)curloc
        };

        data.WindowInfo = windowInfo;

        _session.Player.Taxi.AppendTaximaskTo(data, lastTaxiCheaterState);

        var reachableNodes = new byte[_cliDB.TaxiNodesMask.Length];
        _taxiPathGraph.GetReachableNodesMask(_cliDB.TaxiNodesStorage.LookupByKey(curloc), reachableNodes);

        for (var i = 0; i < reachableNodes.Length; ++i)
        {
            data.CanLandNodes[i] &= reachableNodes[i];
            data.CanUseNodes[i] &= reachableNodes[i];
        }

        _session.SendPacket(data);

        _session.Player.SetTaxiCheater(lastTaxiCheaterState);
    }

    public void SendTaxiStatus(ObjectGuid guid)
    {
        // cheating checks
        var player = _session.Player;
        var unit = ObjectAccessor.GetCreature(player, guid);

        if (unit == null || unit.WorldObjectCombat.IsHostileTo(player) || !unit.HasNpcFlag(NPCFlags.FlightMaster))
        {
            Log.Logger.Debug("WorldSession.SendTaxiStatus - {0} not found.", guid.ToString());

            return;
        }

        // find taxi node
        var nearest = _objectManager.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, player.Team);

        TaxiNodeStatusPkt data = new()
        {
            Unit = guid
        };

        if (nearest == 0)
            data.Status = TaxiNodeStatus.None;
        else if (unit.WorldObjectCombat.GetReactionTo(player) >= ReputationRank.Neutral)
            data.Status = player.Taxi.IsTaximaskNodeKnown(nearest) ? TaxiNodeStatus.Learned : TaxiNodeStatus.Unlearned;
        else
            data.Status = TaxiNodeStatus.NotEligible;

        _session.SendPacket(data);
    }

    [WorldPacketHandler(ClientOpcodes.ActivateTaxi, Processing = PacketProcessing.ThreadSafe)]
    private void HandleActivateTaxi(ActivateTaxi activateTaxi)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(activateTaxi.Vendor, NPCFlags.FlightMaster, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleActivateTaxiOpcode - {0} not found or you can't interact with it.", activateTaxi.Vendor.ToString());
            SendActivateTaxiReply(ActivateTaxiReply.TooFarAway);

            return;
        }

        var curloc = _objectManager.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, _session.Player.Team);

        if (curloc == 0)
            return;

        var from = _cliDB.TaxiNodesStorage.LookupByKey(curloc);

        if (!_cliDB.TaxiNodesStorage.TryGetValue(activateTaxi.Node, out var to))
            return;

        if (!_session.Player.IsTaxiCheater)
            if (!_session.Player.Taxi.IsTaximaskNodeKnown(curloc) || !_session.Player.Taxi.IsTaximaskNodeKnown(activateTaxi.Node))
            {
                SendActivateTaxiReply(ActivateTaxiReply.NotVisited);

                return;
            }

        uint preferredMountDisplay = 0;

        if (_cliDB.MountStorage.TryGetValue(activateTaxi.FlyingMountID, out var mount))
            if (_session.Player.HasSpell(mount.SourceSpellID))
            {
                var mountDisplays = _db2Manager.GetMountDisplays(mount.Id);

                if (mountDisplays != null)
                {
                    var usableDisplays = mountDisplays.Where(mountDisplay => !_cliDB.PlayerConditionStorage.TryGetValue(mountDisplay.PlayerConditionID, out var playerCondition) || _conditionManager.IsPlayerMeetingCondition(_session.Player, playerCondition))
                                                      .ToList();

                    if (!usableDisplays.Empty())
                        preferredMountDisplay = usableDisplays.SelectRandom().CreatureDisplayInfoID;
                }
            }

        List<uint> nodes = new();
        _taxiPathGraph.GetCompleteNodeRoute(from, to, _session.Player, nodes);
        _session.Player.ActivateTaxiPathTo(nodes, unit, 0, preferredMountDisplay);
    }

    [WorldPacketHandler(ClientOpcodes.EnableTaxiNode, Processing = PacketProcessing.ThreadSafe)]
    private void HandleEnableTaxiNodeOpcode(EnableTaxiNode enableTaxiNode)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(enableTaxiNode.Unit, NPCFlags.FlightMaster, NPCFlags2.None);

        if (unit != null)
            SendLearnNewTaxiNode(unit);
    }

    [WorldPacketHandler(ClientOpcodes.TaxiNodeStatusQuery, Processing = PacketProcessing.ThreadSafe)]
    private void HandleTaxiNodeStatusQuery(TaxiNodeStatusQuery taxiNodeStatusQuery)
    {
        SendTaxiStatus(taxiNodeStatusQuery.UnitGUID);
    }

    [WorldPacketHandler(ClientOpcodes.TaxiQueryAvailableNodes, Processing = PacketProcessing.ThreadSafe)]
    private void HandleTaxiQueryAvailableNodes(TaxiQueryAvailableNodes taxiQueryAvailableNodes)
    {
        // cheating checks
        var unit = _session.Player.GetNPCIfCanInteractWith(taxiQueryAvailableNodes.Unit, NPCFlags.FlightMaster, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleTaxiQueryAvailableNodes - {0} not found or you can't interact with him.", taxiQueryAvailableNodes.Unit.ToString());

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        // unknown taxi node case
        if (SendLearnNewTaxiNode(unit))
            return;

        // known taxi node case
        SendTaxiMenu(unit);
    }

    [WorldPacketHandler(ClientOpcodes.TaxiRequestEarlyLanding, Processing = PacketProcessing.ThreadSafe)]
    private void HandleTaxiRequestEarlyLanding(TaxiRequestEarlyLanding taxiRequestEarlyLanding)
    {
        if (taxiRequestEarlyLanding == null ||
            _session.Player.MotionMaster.GetCurrentMovementGenerator() is not FlightPathMovementGenerator flight ||
            !_session.Player.Taxi.RequestEarlyLanding())
            return;

        flight.LoadPath(_session.Player, (uint)flight.Path[(int)flight.GetCurrentNode()].NodeIndex);
        flight.Reset(_session.Player);
    }
}