// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class SendRaidTargetUpdateSingle : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid Target;
	public ObjectGuid ChangedBy;
	public sbyte Symbol;
	public SendRaidTargetUpdateSingle() : base(ServerOpcodes.SendRaidTargetUpdateSingle) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WriteInt8(Symbol);
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(ChangedBy);
	}
}