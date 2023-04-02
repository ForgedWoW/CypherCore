// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildEventPresenceChange : ServerPacket
{
    public ObjectGuid Guid;
    public bool LoggedOn;
    public bool Mobile;
    public string Name;
    public uint VirtualRealmAddress;
    public GuildEventPresenceChange() : base(ServerOpcodes.GuildEventPresenceChange) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Guid);
        _worldPacket.WriteUInt32(VirtualRealmAddress);

        _worldPacket.WriteBits(Name.GetByteCount(), 6);
        _worldPacket.WriteBit(LoggedOn);
        _worldPacket.WriteBit(Mobile);

        _worldPacket.WriteString(Name);
    }
}