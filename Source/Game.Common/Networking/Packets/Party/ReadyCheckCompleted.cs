// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class ReadyCheckCompleted : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid PartyGUID;
	public ReadyCheckCompleted() : base(ServerOpcodes.ReadyCheckCompleted) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WritePackedGuid(PartyGUID);
	}
}