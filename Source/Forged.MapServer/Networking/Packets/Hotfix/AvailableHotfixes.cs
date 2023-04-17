// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Hotfix;

internal class AvailableHotfixes : ServerPacket
{
    public MultiMap<int, HotfixRecord> Hotfixes;
    public uint VirtualRealmAddress;

    public AvailableHotfixes(uint virtualRealmAddress, MultiMap<int, HotfixRecord> hotfixes) : base(ServerOpcodes.AvailableHotfixes)
    {
        VirtualRealmAddress = virtualRealmAddress;
        Hotfixes = hotfixes;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32(VirtualRealmAddress);
        WorldPacket.WriteInt32(Hotfixes.Keys.Count);

        foreach (var key in Hotfixes.Keys)
            Hotfixes[key][0].ID.Write(WorldPacket);
    }
}