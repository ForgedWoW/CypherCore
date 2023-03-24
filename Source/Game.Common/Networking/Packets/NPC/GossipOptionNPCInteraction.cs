// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.NPC;

public class GossipOptionNPCInteraction : ServerPacket
{
	public ObjectGuid GossipGUID;
	public int GossipNpcOptionID;
	public int? FriendshipFactionID;
	public GossipOptionNPCInteraction() : base(ServerOpcodes.GossipOptionNpcInteraction) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GossipGUID);
		_worldPacket.WriteInt32(GossipNpcOptionID);
		_worldPacket.WriteBit(FriendshipFactionID.HasValue);
		_worldPacket.FlushBits();

		if (FriendshipFactionID.HasValue)
			_worldPacket.WriteInt32(FriendshipFactionID.Value);
	}
}
