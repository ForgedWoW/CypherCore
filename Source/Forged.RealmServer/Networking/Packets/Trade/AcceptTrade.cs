// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public class AcceptTrade : ClientPacket
{
	public uint StateIndex;
	public AcceptTrade(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		StateIndex = _worldPacket.ReadUInt32();
	}
}