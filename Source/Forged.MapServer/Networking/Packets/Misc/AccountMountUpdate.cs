// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class AccountMountUpdate : ServerPacket
{
    public bool IsFullUpdate = false;
    public Dictionary<uint, MountStatusFlags> Mounts = new();
    public AccountMountUpdate() : base(ServerOpcodes.AccountMountUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteBit(IsFullUpdate);
        WorldPacket.WriteInt32(Mounts.Count);

        foreach (var (spellId, flags) in Mounts)
        {
            WorldPacket.WriteUInt32(spellId);
            WorldPacket.WriteBits(flags, 4);
        }

        WorldPacket.FlushBits();
    }
}