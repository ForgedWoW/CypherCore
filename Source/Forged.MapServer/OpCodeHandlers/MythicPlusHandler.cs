// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.MythicPlus;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class MythicPlusHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public MythicPlusHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.RequestMythicPlusSeasonData)]
    private void RequestMythicPlusSeasonData(ClientPacket packet)
    {
        if (packet == null)
            return;

        _session.SendPacket(new MythicPlusSeasonData());
    }
}