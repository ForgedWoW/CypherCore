// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Movement;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	public void SendTaxiMenu(Creature unit)
	{
		// find current node
		var curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, Player.Team);

		if (curloc == 0)
			return;

		var lastTaxiCheaterState = Player.IsTaxiCheater;

		if (unit.Entry == 29480)
			Player.SetTaxiCheater(true); // Grimwing in Ebon Hold, special case. NOTE: Not perfect, Zul'Aman should not be included according to WoWhead, and I think taxicheat includes it.

		ShowTaxiNodes data = new();
		ShowTaxiNodesWindowInfo windowInfo = new();
		windowInfo.UnitGUID = unit.GUID;
		windowInfo.CurrentNode = (int)curloc;

		data.WindowInfo = windowInfo;

		Player.Taxi.AppendTaximaskTo(data, lastTaxiCheaterState);

		var reachableNodes = new byte[CliDB.TaxiNodesMask.Length];
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

		Player.MotionMaster.MoveTaxiFlight(path, pathNode);
	}

	public bool SendLearnNewTaxiNode(Creature unit)
	{
		// find current node
		var curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, Player.Team);

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
		{
			return false;
		}
	}

	public void SendDiscoverNewTaxiNode(uint nodeid)
	{
		if (Player.Taxi.SetTaximaskNode(nodeid))
			SendPacket(new NewTaxiPath());
	}

	public void SendActivateTaxiReply(ActivateTaxiReply reply = ActivateTaxiReply.Ok)
	{
		ActivateTaxiReplyPkt data = new();
		data.Reply = reply;
		SendPacket(data);
	}
}