// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Character;

internal class CheckCharacterNameAvailability : ClientPacket
{
    public uint SequenceIndex;
    public string Name;

    public CheckCharacterNameAvailability(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        SequenceIndex = _worldPacket.ReadUInt32();
        Name = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
    }
}