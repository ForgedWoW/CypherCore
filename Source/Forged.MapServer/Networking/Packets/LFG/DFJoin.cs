// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class DFJoin : ClientPacket
{
    public byte PartyIndex;
    public bool QueueAsGroup;
    public LfgRoles Roles;
    public List<uint> Slots = new();
    private bool Unknown; // Always false in 7.2.5
    public DFJoin(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        QueueAsGroup = WorldPacket.HasBit();
        Unknown = WorldPacket.HasBit();
        PartyIndex = WorldPacket.ReadUInt8();
        Roles = (LfgRoles)WorldPacket.ReadUInt32();

        var slotsCount = WorldPacket.ReadInt32();

        for (var i = 0; i < slotsCount; ++i) // Slots
            Slots.Add(WorldPacket.ReadUInt32());
    }
}

//Structs