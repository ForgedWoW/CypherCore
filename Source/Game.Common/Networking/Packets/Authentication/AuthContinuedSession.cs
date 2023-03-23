// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Authentication;

public class AuthContinuedSession : ClientPacket
{
	public ulong DosResponse;
	public ulong Key;
	public byte[] LocalChallenge = new byte[16];
	public byte[] Digest = new byte[24];
	public AuthContinuedSession(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		DosResponse = _worldPacket.ReadUInt64();
		Key = _worldPacket.ReadUInt64();
		LocalChallenge = _worldPacket.ReadBytes(16);
		Digest = _worldPacket.ReadBytes(24);
	}
}
