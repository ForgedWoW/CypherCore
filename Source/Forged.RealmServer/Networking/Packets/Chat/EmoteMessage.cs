// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class EmoteMessage : ServerPacket
{
	public ObjectGuid Guid;
	public uint EmoteID;
	public List<uint> SpellVisualKitIDs = new();
	public int SequenceVariation;
	public EmoteMessage() : base(ServerOpcodes.Emote, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteUInt32(EmoteID);
		_worldPacket.WriteInt32(SpellVisualKitIDs.Count);
		_worldPacket.WriteInt32(SequenceVariation);

		foreach (var id in SpellVisualKitIDs)
			_worldPacket.WriteUInt32(id);
	}
}