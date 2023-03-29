// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class AccountHeirloomUpdate : ServerPacket
{
    public bool IsFullUpdate;
    public Dictionary<uint, HeirloomData> Heirlooms = new();
    public int Unk;
    public AccountHeirloomUpdate() : base(ServerOpcodes.AccountHeirloomUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteBit(IsFullUpdate);
        _worldPacket.FlushBits();

        _worldPacket.WriteInt32(Unk);

        // both lists have to have the same size
        _worldPacket.WriteInt32(Heirlooms.Count);
        _worldPacket.WriteInt32(Heirlooms.Count);

        foreach (var item in Heirlooms)
            _worldPacket.WriteUInt32(item.Key);

        foreach (var flags in Heirlooms)
            _worldPacket.WriteUInt32((uint)flags.Value.Flags);
    }
}