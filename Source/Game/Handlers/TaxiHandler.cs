// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Movement;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.EnableTaxiNode, Processing = PacketProcessing.ThreadSafe)]
        void HandleEnableTaxiNodeOpcode(EnableTaxiNode enableTaxiNode)
        {
            Creature unit = Player.GetNPCIfCanInteractWith(enableTaxiNode.Unit, NPCFlags.FlightMaster, NPCFlags2.None);
            if (unit)
                SendLearnNewTaxiNode(unit);
        }

        [WorldPacketHandler(ClientOpcodes.TaxiNodeStatusQuery, Processing = PacketProcessing.ThreadSafe)]
        void HandleTaxiNodeStatusQuery(TaxiNodeStatusQuery taxiNodeStatusQuery)
        {
            SendTaxiStatus(taxiNodeStatusQuery.UnitGUID);
        }

        public void SendTaxiStatus(ObjectGuid guid)
        {
            // cheating checks
            Player player = Player;
            Creature unit = ObjectAccessor.GetCreature(player, guid);
            if (!unit || unit.IsHostileTo(player) || !unit.HasNpcFlag(NPCFlags.FlightMaster))
            {
                Log.outDebug(LogFilter.Network, "WorldSession.SendTaxiStatus - {0} not found.", guid.ToString());
                return;
            }

            // find taxi node
            uint nearest = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, player.Team);

            TaxiNodeStatusPkt data = new();
            data.Unit = guid;

            if (nearest == 0)
                data.Status = TaxiNodeStatus.None;
            else if (unit.GetReactionTo(player) >= ReputationRank.Neutral)
                data.Status = player.Taxi.IsTaximaskNodeKnown(nearest) ? TaxiNodeStatus.Learned : TaxiNodeStatus.Unlearned;
            else
                data.Status = TaxiNodeStatus.NotEligible;

            SendPacket(data);
        }

        [WorldPacketHandler(ClientOpcodes.TaxiQueryAvailableNodes, Processing = PacketProcessing.ThreadSafe)]
        void HandleTaxiQueryAvailableNodes(TaxiQueryAvailableNodes taxiQueryAvailableNodes)
        {
            // cheating checks
            Creature unit = Player.GetNPCIfCanInteractWith(taxiQueryAvailableNodes.Unit, NPCFlags.FlightMaster, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleTaxiQueryAvailableNodes - {0} not found or you can't interact with him.", taxiQueryAvailableNodes.Unit.ToString());
                return;
            }

            // remove fake death
            if (Player.HasUnitState(UnitState.Died))
                Player.RemoveAurasByType(AuraType.FeignDeath);

            // unknown taxi node case
            if (SendLearnNewTaxiNode(unit))
                return;

            // known taxi node case
            SendTaxiMenu(unit);
        }

        public void SendTaxiMenu(Creature unit)
        {
            // find current node
            uint curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, Player.Team);
            if (curloc == 0)
                return;

            bool lastTaxiCheaterState = Player.IsTaxiCheater;
            if (unit.Entry == 29480)
                Player.SetTaxiCheater(true); // Grimwing in Ebon Hold, special case. NOTE: Not perfect, Zul'Aman should not be included according to WoWhead, and I think taxicheat includes it.

            ShowTaxiNodes data = new();
            ShowTaxiNodesWindowInfo windowInfo = new();
            windowInfo.UnitGUID = unit.GUID;
            windowInfo.CurrentNode = (int)curloc;

            data.WindowInfo = windowInfo;

            Player.Taxi.AppendTaximaskTo(data, lastTaxiCheaterState);

            byte[] reachableNodes = new byte[CliDB.TaxiNodesMask.Length];
            TaxiPathGraph.GetReachableNodesMask(CliDB.TaxiNodesStorage.LookupByKey(curloc), reachableNodes);
            for (var i = 0; i < reachableNodes.Length; ++i)
            {
                data.CanLandNodes[i] &= reachableNodes[i];
                data.CanUseNodes[i] &= reachableNodes[i];
            }


            SendPacket(data);

            Player.SetTaxiCheater(lastTaxiCheaterState);
        }

        public void SendDoFlight(uint mountDisplayId, uint path, uint pathNode = 0)
        {
            // remove fake death
            if (Player.HasUnitState(UnitState.Died))
                Player.RemoveAurasByType(AuraType.FeignDeath);

            if (mountDisplayId != 0)
                Player.Mount(mountDisplayId);

            Player.
            MotionMaster.MoveTaxiFlight(path, pathNode);
        }

        public bool SendLearnNewTaxiNode(Creature unit)
        {
            // find current node
            uint curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, Player.Team);

            if (curloc == 0)
                return true;

            if (Player.Taxi.SetTaximaskNode(curloc))
            {
                SendPacket(new NewTaxiPath());

                TaxiNodeStatusPkt data = new();
                data.Unit = unit.GUID;
                data.Status = TaxiNodeStatus.Learned;
                SendPacket(data);

                return true;
            }
            else
                return false;
        }

        public void SendDiscoverNewTaxiNode(uint nodeid)
        {
            if (Player.Taxi.SetTaximaskNode(nodeid))
                SendPacket(new NewTaxiPath());
        }

        [WorldPacketHandler(ClientOpcodes.ActivateTaxi, Processing = PacketProcessing.ThreadSafe)]
        void HandleActivateTaxi(ActivateTaxi activateTaxi)
        {
            Creature unit = Player.GetNPCIfCanInteractWith(activateTaxi.Vendor, NPCFlags.FlightMaster, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleActivateTaxiOpcode - {0} not found or you can't interact with it.", activateTaxi.Vendor.ToString());
                SendActivateTaxiReply(ActivateTaxiReply.TooFarAway);
                return;
            }

            uint curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, Player.Team);
            if (curloc == 0)
                return;

            TaxiNodesRecord from = CliDB.TaxiNodesStorage.LookupByKey(curloc);
            TaxiNodesRecord to = CliDB.TaxiNodesStorage.LookupByKey(activateTaxi.Node);
            if (to == null)
                return;

            if (!Player.IsTaxiCheater)
            {
                if (!Player.Taxi.IsTaximaskNodeKnown(curloc) || !Player.Taxi.IsTaximaskNodeKnown(activateTaxi.Node))
                {
                    SendActivateTaxiReply(ActivateTaxiReply.NotVisited);
                    return;
                }
            }

            uint preferredMountDisplay = 0;
            MountRecord mount = CliDB.MountStorage.LookupByKey(activateTaxi.FlyingMountID);
            if (mount != null)
            {
                if (Player.HasSpell(mount.SourceSpellID))
                {
                    var mountDisplays = Global.DB2Mgr.GetMountDisplays(mount.Id);
                    if (mountDisplays != null)
                    {
                        List<MountXDisplayRecord> usableDisplays = mountDisplays.Where(mountDisplay =>
                        {
                            PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mountDisplay.PlayerConditionID);
                            if (playerCondition != null)
                                return ConditionManager.IsPlayerMeetingCondition(Player, playerCondition);

                            return true;
                        }).ToList();

                        if (!usableDisplays.Empty())
                            preferredMountDisplay = usableDisplays.SelectRandom().CreatureDisplayInfoID;
                    }
                }
            }

            List<uint> nodes = new();
            TaxiPathGraph.GetCompleteNodeRoute(from, to, Player, nodes);
            Player.ActivateTaxiPathTo(nodes, unit, 0, preferredMountDisplay);
        }

        public void SendActivateTaxiReply(ActivateTaxiReply reply = ActivateTaxiReply.Ok)
        {
            ActivateTaxiReplyPkt data = new();
            data.Reply = reply;
            SendPacket(data);
        }

        [WorldPacketHandler(ClientOpcodes.TaxiRequestEarlyLanding, Processing = PacketProcessing.ThreadSafe)]
        void HandleTaxiRequestEarlyLanding(TaxiRequestEarlyLanding taxiRequestEarlyLanding)
        {
            FlightPathMovementGenerator flight = Player.MotionMaster.GetCurrentMovementGenerator() as FlightPathMovementGenerator;
            if (flight != null)
            {
                if (Player.Taxi.RequestEarlyLanding())
                {
                    flight.LoadPath(Player, (uint)flight.GetPath()[(int)flight.GetCurrentNode()].NodeIndex);
                    flight.Reset(Player);
                }
            }
        }
    }
}
