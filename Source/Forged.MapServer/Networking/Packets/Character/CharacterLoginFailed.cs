// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharacterLoginFailed : ServerPacket
{
    private readonly LoginFailureReason Code;

	public CharacterLoginFailed(LoginFailureReason code) : base(ServerOpcodes.CharacterLoginFailed)
	{
		Code = code;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Code);
	}
}