// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class AccountHeirloomUpdate : ServerPacket
{
    public Dictionary<uint, HeirloomData> Heirlooms = new();
    public bool IsFullUpdate;
    public int Unk;
    public AccountHeirloomUpdate() : base(ServerOpcodes.AccountHeirloomUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteBit(IsFullUpdate);
        WorldPacket.FlushBits();

        WorldPacket.WriteInt32(Unk);

        // both lists have to have the same size
        WorldPacket.WriteInt32(Heirlooms.Count);
        WorldPacket.WriteInt32(Heirlooms.Count);

        foreach (var item in Heirlooms)
            WorldPacket.WriteUInt32(item.Key);

        foreach (var flags in Heirlooms)
            WorldPacket.WriteUInt32((uint)flags.Value.Flags);
    }
}