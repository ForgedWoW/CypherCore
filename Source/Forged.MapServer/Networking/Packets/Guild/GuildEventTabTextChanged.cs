// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildEventTabTextChanged : ServerPacket
{
    public int Tab;
    public GuildEventTabTextChanged() : base(ServerOpcodes.GuildEventTabTextChanged) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Tab);
    }
}