// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Server;

public class DosProtection
{
    private readonly Dictionary<uint, PacketCounter> _packetThrottlingMap = new();
    private readonly Policy _policy;
    private readonly WorldSession _session;

    public DosProtection(WorldSession s)
    {
        _session = s;
        _policy = (Policy)GetDefaultValue("PacketSpoof:Policy", 1);
    }

    private enum Policy
    {
        Log,
        Kick,
        Ban,
    }

    //todo fix me
    public bool EvaluateOpcode(WorldPacket packet, long time)
    {
        uint maxPacketCounterAllowed = 0; // GetMaxPacketCounterAllowed(p.GetOpcode());

        // Return true if there no limit for the opcode
        if (maxPacketCounterAllowed == 0)
            return true;

        if (!_packetThrottlingMap.ContainsKey(packet.Opcode))
            _packetThrottlingMap[packet.Opcode] = new PacketCounter();

        var packetCounter = _packetThrottlingMap[packet.Opcode];

        if (packetCounter.LastReceiveTime != time)
        {
            packetCounter.LastReceiveTime = time;
            packetCounter.AmountCounter = 0;
        }

        // Check if player is flooding some packets
        if (++packetCounter.AmountCounter <= maxPacketCounterAllowed)
            return true;

        Log.Logger.Warning("AntiDOS: Account {0}, IP: {1}, Ping: {2}, Character: {3}, flooding packet (opc: {4} (0x{4}), count: {5})",
                           _session.AccountId,
                           _session.RemoteAddress,
                           _session.Latency,
                           _session.PlayerName,
                           packet.Opcode,
                           packetCounter.AmountCounter);

        switch (_policy)
        {
            case Policy.Log:
                return true;
            case Policy.Kick:
                Log.Logger.Information("AntiDOS: Player kicked!");

                return false;
            case Policy.Ban:
                var bm = (BanMode)GetDefaultValue("PacketSpoof:BanMode", (int)BanMode.Account);
                var duration = GetDefaultValue("PacketSpoof:BanDuration", 86400); // in seconds
                var nameOrIp = "";

                switch (bm)
                {
                    case BanMode.Character: // not supported, ban account
                    case BanMode.Account:
                        Global.AccountMgr.GetName(_session.AccountId, out nameOrIp);

                        break;
                    case BanMode.IP:
                        nameOrIp = _session.RemoteAddress;

                        break;
                }

                Global.WorldMgr.BanAccount(bm, nameOrIp, duration, "DOS (Packet Flooding/Spoofing", "Server: AutoDOS");
                Log.Logger.Information("AntiDOS: Player automatically banned for {0} seconds.", duration);

                return false;
        }

        return true;
    }
}