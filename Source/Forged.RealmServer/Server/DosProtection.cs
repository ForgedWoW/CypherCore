// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Networking;

namespace Forged.RealmServer;

public class DosProtection
{
	readonly Policy _policy;
	readonly WorldSession Session;
	readonly Dictionary<uint, PacketCounter> _PacketThrottlingMap = new();

	public DosProtection(WorldSession s)
	{
		Session = s;
		_policy = (Policy)_worldConfig.GetIntValue(WorldCfg.PacketSpoofPolicy);
	}

	//todo fix me
	public bool EvaluateOpcode(WorldPacket packet, long time)
	{
		uint maxPacketCounterAllowed = 0; // GetMaxPacketCounterAllowed(p.GetOpcode());

		// Return true if there no limit for the opcode
		if (maxPacketCounterAllowed == 0)
			return true;

		if (!_PacketThrottlingMap.ContainsKey(packet.GetOpcode()))
			_PacketThrottlingMap[packet.GetOpcode()] = new PacketCounter();

		var packetCounter = _PacketThrottlingMap[packet.GetOpcode()];

		if (packetCounter.LastReceiveTime != time)
		{
			packetCounter.LastReceiveTime = time;
			packetCounter.AmountCounter = 0;
		}

		// Check if player is flooding some packets
		if (++packetCounter.AmountCounter <= maxPacketCounterAllowed)
			return true;

		Log.Logger.Warning(
					"AntiDOS: Account {0}, IP: {1}, Ping: {2}, Character: {3}, flooding packet (opc: {4} (0x{4}), count: {5})",
					Session.AccountId,
					Session.RemoteAddress,
					Session.Latency,
					Session.PlayerName,
					packet.GetOpcode(),
					packetCounter.AmountCounter);

		switch (_policy)
		{
			case Policy.Log:
				return true;
			case Policy.Kick:
				Log.Logger.Information("AntiDOS: Player kicked!");

				return false;
			case Policy.Ban:
				var bm = (BanMode)_worldConfig.GetIntValue(WorldCfg.PacketSpoofBanmode);
				var duration = _worldConfig.GetUIntValue(WorldCfg.PacketSpoofBanduration); // in seconds
				var nameOrIp = "";

				switch (bm)
				{
					case BanMode.Character: // not supported, ban account
					case BanMode.Account:
						Global.AccountMgr.GetName(Session.AccountId, out nameOrIp);

						break;
					case BanMode.IP:
						nameOrIp = Session.RemoteAddress;

						break;
				}

				Global.WorldMgr.BanAccount(bm, nameOrIp, duration, "DOS (Packet Flooding/Spoofing", "Server: AutoDOS");
				Log.Logger.Information("AntiDOS: Player automatically banned for {0} seconds.", duration);

				return false;
		}

		return true;
	}

	enum Policy
	{
		Log,
		Kick,
		Ban,
	}
}