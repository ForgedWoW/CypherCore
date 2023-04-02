// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Character;

public class GenerateRandomCharacterName : ClientPacket
{
    public byte Race;
    public byte Sex;
    public GenerateRandomCharacterName(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Race = _worldPacket.ReadUInt8();
        Sex = _worldPacket.ReadUInt8();
    }
}