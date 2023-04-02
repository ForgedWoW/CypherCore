// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LFGUpdateStatus : ServerPacket
{
    public bool IsParty;
    public bool Joined;
    public bool LfgJoined;
    public bool NotifyUI;
    public bool Queued;
    public uint QueueMapID;
    public byte Reason;
    public uint RequestedRoles;
    public List<uint> Slots = new();
    public byte SubType;
    public List<ObjectGuid> SuspendedPlayers = new();
    public RideTicket Ticket = new();
    public bool Unused;
    public LFGUpdateStatus() : base(ServerOpcodes.LfgUpdateStatus) { }

    public override void Write()
    {
        Ticket.Write(WorldPacket);

        WorldPacket.WriteUInt8(SubType);
        WorldPacket.WriteUInt8(Reason);
        WorldPacket.WriteInt32(Slots.Count);
        WorldPacket.WriteUInt32(RequestedRoles);
        WorldPacket.WriteInt32(SuspendedPlayers.Count);
        WorldPacket.WriteUInt32(QueueMapID);

        foreach (var slot in Slots)
            WorldPacket.WriteUInt32(slot);

        foreach (var player in SuspendedPlayers)
            WorldPacket.WritePackedGuid(player);

        WorldPacket.WriteBit(IsParty);
        WorldPacket.WriteBit(NotifyUI);
        WorldPacket.WriteBit(Joined);
        WorldPacket.WriteBit(LfgJoined);
        WorldPacket.WriteBit(Queued);
        WorldPacket.WriteBit(Unused);
        WorldPacket.FlushBits();
    }
}