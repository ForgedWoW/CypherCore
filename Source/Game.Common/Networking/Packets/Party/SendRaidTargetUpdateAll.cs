// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class SendRaidTargetUpdateAll : ServerPacket
{
	public sbyte PartyIndex;
	public Dictionary<byte, ObjectGuid> TargetIcons = new();
	public SendRaidTargetUpdateAll() : base(ServerOpcodes.SendRaidTargetUpdateAll) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);

		_worldPacket.WriteInt32(TargetIcons.Count);

		foreach (var pair in TargetIcons)
		{
			_worldPacket.WritePackedGuid(pair.Value);
			_worldPacket.WriteUInt8(pair.Key);
		}
	}
}