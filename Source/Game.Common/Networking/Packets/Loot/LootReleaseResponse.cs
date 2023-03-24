// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Loot;

public class LootReleaseResponse : ServerPacket
{
	public ObjectGuid LootObj;
	public ObjectGuid Owner;
	public LootReleaseResponse() : base(ServerOpcodes.LootRelease) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WritePackedGuid(Owner);
	}
}
