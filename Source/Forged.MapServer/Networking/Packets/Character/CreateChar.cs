// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class CreateChar : ServerPacket
{
	public ResponseCodes Code;
	public ObjectGuid Guid;
	public CreateChar() : base(ServerOpcodes.CreateChar) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Code);
		_worldPacket.WritePackedGuid(Guid);
	}
}