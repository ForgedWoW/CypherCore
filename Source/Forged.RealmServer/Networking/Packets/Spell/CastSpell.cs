// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public class CastSpell : ClientPacket
{
	public SpellCastRequest Cast;

	public CastSpell(WorldPacket packet) : base(packet)
	{
		Cast = new SpellCastRequest();
	}

	public override void Read()
	{
		Cast.Read(_worldPacket);
	}
}