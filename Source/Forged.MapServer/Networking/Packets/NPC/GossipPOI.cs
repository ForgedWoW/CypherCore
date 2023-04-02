// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

internal class GossipPOI : ServerPacket
{
    public uint Flags;
    public uint Icon;
    public uint Id;
    public uint Importance;
    public string Name;
    public Vector3 Pos;
    public uint WMOGroupID;
    public GossipPOI() : base(ServerOpcodes.GossipPoi) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(Id);
        WorldPacket.WriteVector3(Pos);
        WorldPacket.WriteUInt32(Icon);
        WorldPacket.WriteUInt32(Importance);
        WorldPacket.WriteUInt32(WMOGroupID);
        WorldPacket.WriteBits(Flags, 14);
        WorldPacket.WriteBits(Name.GetByteCount(), 6);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(Name);
    }
}