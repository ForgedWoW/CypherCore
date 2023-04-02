// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

internal class VoidStorageTransferChanges : ServerPacket
{
    public List<VoidItem> AddedItems = new();
    public List<ObjectGuid> RemovedItems = new();
    public VoidStorageTransferChanges() : base(ServerOpcodes.VoidStorageTransferChanges, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteBits(AddedItems.Count, 4);
        WorldPacket.WriteBits(RemovedItems.Count, 4);
        WorldPacket.FlushBits();

        foreach (var addedItem in AddedItems)
            addedItem.Write(WorldPacket);

        foreach (var removedItem in RemovedItems)
            WorldPacket.WritePackedGuid(removedItem);
    }
}