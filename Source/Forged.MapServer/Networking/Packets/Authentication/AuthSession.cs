// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Authentication;

internal class AuthSession : ClientPacket
{
    public uint BattlegroupID;
    public byte[] Digest = new byte[24];
    public ulong DosResponse;
    public Array<byte> LocalChallenge = new(16);
    public uint RealmID;
    public string RealmJoinTicket;
    public uint RegionID;
    public bool UseIPv6;
    public AuthSession(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        DosResponse = WorldPacket.ReadUInt64();
        RegionID = WorldPacket.ReadUInt32();
        BattlegroupID = WorldPacket.ReadUInt32();
        RealmID = WorldPacket.ReadUInt32();

        for (var i = 0; i < LocalChallenge.GetLimit(); ++i)
            LocalChallenge[i] = WorldPacket.ReadUInt8();

        Digest = WorldPacket.ReadBytes(24);

        UseIPv6 = WorldPacket.HasBit();
        var realmJoinTicketSize = WorldPacket.ReadUInt32();

        if (realmJoinTicketSize != 0)
            RealmJoinTicket = WorldPacket.ReadString(realmJoinTicketSize);
    }
}