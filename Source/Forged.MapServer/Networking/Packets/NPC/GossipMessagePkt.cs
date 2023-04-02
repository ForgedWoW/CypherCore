// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

public class GossipMessagePkt : ServerPacket
{
    public int FriendshipFactionID;
    public ObjectGuid GossipGUID;
    public uint GossipID;
    public List<ClientGossipOptions> GossipOptions = new();
    public List<ClientGossipText> GossipText = new();
    public int? TextID;
    public int? TextID2;
    public GossipMessagePkt() : base(ServerOpcodes.GossipMessage) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(GossipGUID);
        WorldPacket.WriteUInt32(GossipID);
        WorldPacket.WriteInt32(FriendshipFactionID);
        WorldPacket.WriteInt32(GossipOptions.Count);
        WorldPacket.WriteInt32(GossipText.Count);
        WorldPacket.WriteBit(TextID.HasValue);
        WorldPacket.WriteBit(TextID2.HasValue);
        WorldPacket.FlushBits();

        foreach (var options in GossipOptions)
            options.Write(WorldPacket);

        if (TextID.HasValue)
            WorldPacket.WriteInt32(TextID.Value);

        if (TextID2.HasValue)
            WorldPacket.WriteInt32(TextID2.Value);

        foreach (var text in GossipText)
            text.Write(WorldPacket);
    }
}