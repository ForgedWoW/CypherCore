// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

internal class GossipOptionNPCInteraction : ServerPacket
{
    public int? FriendshipFactionID;
    public ObjectGuid GossipGUID;
    public int GossipNpcOptionID;
    public GossipOptionNPCInteraction() : base(ServerOpcodes.GossipOptionNpcInteraction) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(GossipGUID);
        WorldPacket.WriteInt32(GossipNpcOptionID);
        WorldPacket.WriteBit(FriendshipFactionID.HasValue);
        WorldPacket.FlushBits();

        if (FriendshipFactionID.HasValue)
            WorldPacket.WriteInt32(FriendshipFactionID.Value);
    }
}