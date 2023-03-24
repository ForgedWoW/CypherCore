// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Authentication;

public class AuthChallenge : ServerPacket
{
	public byte[] Challenge = new byte[16];
	public byte[] DosChallenge = new byte[32]; // Encryption seeds
	public byte DosZeroBits;
	public AuthChallenge() : base(ServerOpcodes.AuthChallenge) { }

	public override void Write()
	{
		_worldPacket.WriteBytes(DosChallenge);
		_worldPacket.WriteBytes(Challenge);
		_worldPacket.WriteUInt8(DosZeroBits);
	}
}
