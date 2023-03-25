// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

class VoidStorageContents : ServerPacket
{
	public List<VoidItem> Items = new();
	public VoidStorageContents() : base(ServerOpcodes.VoidStorageContents, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Items.Count, 8);
		_worldPacket.FlushBits();

		foreach (var voidItem in Items)
			voidItem.Write(_worldPacket);
	}
}