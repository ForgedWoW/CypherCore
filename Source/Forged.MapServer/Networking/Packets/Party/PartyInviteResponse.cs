// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

internal class PartyInviteResponse : ClientPacket
{
    public bool Accept;
    public byte PartyIndex;
    public uint? RolesDesired;
    public PartyInviteResponse(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = WorldPacket.ReadUInt8();

        Accept = WorldPacket.HasBit();

        var hasRolesDesired = WorldPacket.HasBit();

        if (hasRolesDesired)
            RolesDesired = WorldPacket.ReadUInt32();
    }
}