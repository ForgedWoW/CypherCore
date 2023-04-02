// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Authentication;

internal class AuthContinuedSession : ClientPacket
{
    public byte[] Digest = new byte[24];
    public ulong DosResponse;
    public ulong Key;
    public byte[] LocalChallenge = new byte[16];
    public AuthContinuedSession(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        DosResponse = WorldPacket.ReadUInt64();
        Key = WorldPacket.ReadUInt64();
        LocalChallenge = WorldPacket.ReadBytes(16);
        Digest = WorldPacket.ReadBytes(24);
    }
}