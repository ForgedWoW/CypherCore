// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Authentication;

class AuthSession : ClientPacket
{
	public uint RegionID;
	public uint BattlegroupID;
	public uint RealmID;
	public Array<byte> LocalChallenge = new(16);
	public byte[] Digest = new byte[24];
	public ulong DosResponse;
	public string RealmJoinTicket;
	public bool UseIPv6;
	public AuthSession(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		DosResponse = _worldPacket.ReadUInt64();
		RegionID = _worldPacket.ReadUInt32();
		BattlegroupID = _worldPacket.ReadUInt32();
		RealmID = _worldPacket.ReadUInt32();

		for (var i = 0; i < LocalChallenge.GetLimit(); ++i)
			LocalChallenge[i] = _worldPacket.ReadUInt8();

		Digest = _worldPacket.ReadBytes(24);

		UseIPv6 = _worldPacket.HasBit();
		var realmJoinTicketSize = _worldPacket.ReadUInt32();

		if (realmJoinTicketSize != 0)
			RealmJoinTicket = _worldPacket.ReadString(realmJoinTicketSize);
	}
}