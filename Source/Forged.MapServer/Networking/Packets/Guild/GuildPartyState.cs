// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildPartyState : ServerPacket
{
    public float GuildXPEarnedMult = 0.0f;
    public bool InGuildParty;
    public int NumMembers;
    public int NumRequired;
    public GuildPartyState() : base(ServerOpcodes.GuildPartyState, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteBit(InGuildParty);
        WorldPacket.FlushBits();

        WorldPacket.WriteInt32(NumMembers);
        WorldPacket.WriteInt32(NumRequired);
        WorldPacket.WriteFloat(GuildXPEarnedMult);
    }
}