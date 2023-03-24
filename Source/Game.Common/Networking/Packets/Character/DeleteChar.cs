// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Character;

public class DeleteChar : ServerPacket
{
	public ResponseCodes Code;
	public DeleteChar() : base(ServerOpcodes.DeleteChar) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Code);
	}
}
