// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class CharacterLoginFailed : ServerPacket
{
	readonly LoginFailureReason Code;

	public CharacterLoginFailed(LoginFailureReason code) : base(ServerOpcodes.CharacterLoginFailed)
	{
		Code = code;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Code);
	}
}