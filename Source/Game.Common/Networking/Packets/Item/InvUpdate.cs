// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.Item;

public struct InvUpdate
{
	public InvUpdate(WorldPacket data)
	{
		Items = new List<InvItem>();
		var size = data.ReadBits<int>(2);
		data.ResetBitPos();

		for (var i = 0; i < size; ++i)
		{
			var item = new InvItem
			{
				ContainerSlot = data.ReadUInt8(),
				Slot = data.ReadUInt8()
			};

			Items.Add(item);
		}
	}

	public List<InvItem> Items;

	public struct InvItem
	{
		public byte ContainerSlot;
		public byte Slot;
	}
}
