// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildEventStatusChange : ServerPacket
{
    public bool AFK;
    public bool DND;
    public ObjectGuid Guid;
    public GuildEventStatusChange() : base(ServerOpcodes.GuildEventStatusChange) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Guid);
        _worldPacket.WriteBit(AFK);
        _worldPacket.WriteBit(DND);
        _worldPacket.FlushBits();
    }
}