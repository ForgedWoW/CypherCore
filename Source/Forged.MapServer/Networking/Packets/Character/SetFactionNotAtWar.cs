// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Character;

internal class SetFactionNotAtWar : ClientPacket
{
    public byte FactionIndex;
    public SetFactionNotAtWar(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        FactionIndex = _worldPacket.ReadUInt8();
    }
}