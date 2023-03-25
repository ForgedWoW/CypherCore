// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class EnableBarberShop : ServerPacket
{
	public EnableBarberShop() : base(ServerOpcodes.EnableBarberShop) { }

	public override void Write() { }
}