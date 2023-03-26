// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.GameObject;

internal class PageTextPkt : ServerPacket
{
	public ObjectGuid GameObjectGUID;
	public PageTextPkt() : base(ServerOpcodes.PageText) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GameObjectGUID);
	}
}