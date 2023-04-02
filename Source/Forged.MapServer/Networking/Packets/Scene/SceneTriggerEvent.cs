// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Scene;

internal class SceneTriggerEvent : ClientPacket
{
    public string _Event;
    public uint SceneInstanceID;
    public SceneTriggerEvent(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var len = WorldPacket.ReadBits<uint>(6);
        SceneInstanceID = WorldPacket.ReadUInt32();
        _Event = WorldPacket.ReadString(len);
    }
}