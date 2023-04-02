// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

internal class GuildNameChanged : ServerPacket
{
    public ObjectGuid GuildGUID;
    public string GuildName;
    public GuildNameChanged() : base(ServerOpcodes.GuildNameChanged) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(GuildGUID);
        WorldPacket.WriteBits(GuildName.GetByteCount(), 7);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(GuildName);
    }
}