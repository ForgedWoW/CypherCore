// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.NPC;

namespace Game.Common.Networking.Packets.NPC;

public class GossipMessagePkt : ServerPacket
{
	public List<ClientGossipOptions> GossipOptions = new();
	public int FriendshipFactionID;
	public ObjectGuid GossipGUID;
	public List<ClientGossipText> GossipText = new();
	public int? TextID;
	public int? TextID2;
	public uint GossipID;
	public GossipMessagePkt() : base(ServerOpcodes.GossipMessage) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GossipGUID);
		_worldPacket.WriteUInt32(GossipID);
		_worldPacket.WriteInt32(FriendshipFactionID);
		_worldPacket.WriteInt32(GossipOptions.Count);
		_worldPacket.WriteInt32(GossipText.Count);
		_worldPacket.WriteBit(TextID.HasValue);
		_worldPacket.WriteBit(TextID2.HasValue);
		_worldPacket.FlushBits();

		foreach (var options in GossipOptions)
			options.Write(_worldPacket);

		if (TextID.HasValue)
			_worldPacket.WriteInt32(TextID.Value);

		if (TextID2.HasValue)
			_worldPacket.WriteInt32(TextID2.Value);

		foreach (var text in GossipText)
			text.Write(_worldPacket);
	}
}
