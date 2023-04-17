// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Accounts;
using Forged.MapServer.Networking;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Server;

public class DosProtection
{
    private readonly Dictionary<uint, PacketCounter> _packetThrottlingMap = new();
    private readonly Policy _policy;
    private readonly WorldSession _session;
    private readonly IConfiguration _configuration;
    private readonly AccountManager _accountManager;
    private readonly WorldManager _worldManager;

    public DosProtection(WorldSession s, IConfiguration configuration, AccountManager accountManager, WorldManager worldManager)
    {
        _session = s;
        _configuration = configuration;
        _accountManager = accountManager;
        _worldManager = worldManager;
        _policy = (Policy)_configuration.GetDefaultValue("PacketSpoof:Policy", 1);
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
                var bm = (BanMode)_configuration.GetDefaultValue("PacketSpoof:BanMode", (int)BanMode.Account);
                var duration = _configuration.GetDefaultValue("PacketSpoof:BanDuration", 86400u); // in seconds
                var nameOrIp = "";

                switch (bm)
                {
                    case BanMode.Character: // not supported, ban account
                    case BanMode.Account:
                        _accountManager.GetName(_session.AccountId, out nameOrIp);

                        break;
                    case BanMode.IP:
                        nameOrIp = _session.RemoteAddress;

                        break;
                }

                _worldManager.BanAccount(bm, nameOrIp, duration, "DOS (Packet Flooding/Spoofing", "Server: AutoDOS");
                Log.Logger.Information("AntiDOS: Player automatically banned for {0} seconds.", duration);

                return false;
        }

        return true;
    }
}