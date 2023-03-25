// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

class VoidStorageTransferChanges : ServerPacket
{
	public List<ObjectGuid> RemovedItems = new();
	public List<VoidItem> AddedItems = new();
	public VoidStorageTransferChanges() : base(ServerOpcodes.VoidStorageTransferChanges, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBits(AddedItems.Count, 4);
		_worldPacket.WriteBits(RemovedItems.Count, 4);
		_worldPacket.FlushBits();

		foreach (var addedItem in AddedItems)
			addedItem.Write(_worldPacket);

		foreach (var removedItem in RemovedItems)
			_worldPacket.WritePackedGuid(removedItem);
	}
}