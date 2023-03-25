// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class SummonResponse : ClientPacket
{
	public bool Accept;
	public ObjectGuid SummonerGUID;
	public SummonResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SummonerGUID = _worldPacket.ReadPackedGuid();
		Accept = _worldPacket.HasBit();
	}
}