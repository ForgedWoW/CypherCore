// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.Item;

public class AutoEquipItemSlot : ClientPacket
{
	public ObjectGuid Item;
	public byte ItemDstSlot;
	public InvUpdate Inv;
	public AutoEquipItemSlot(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		Item = _worldPacket.ReadPackedGuid();
		ItemDstSlot = _worldPacket.ReadUInt8();
	}
}
