// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Trait;

internal class ClassTalentsRenameConfig : ClientPacket
{
    public int ConfigID;
    public string Name;

    public ClassTalentsRenameConfig(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ConfigID = _worldPacket.ReadInt32();
        var nameLength = _worldPacket.ReadBits<uint>(9);
        Name = _worldPacket.ReadString(nameLength);
    }
}