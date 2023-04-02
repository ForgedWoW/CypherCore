// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Social;

public class SetContactNotes : ClientPacket
{
    public string Notes;
    public QualifiedGUID Player;
    public SetContactNotes(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Player.Read(_worldPacket);
        Notes = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(10));
    }
}