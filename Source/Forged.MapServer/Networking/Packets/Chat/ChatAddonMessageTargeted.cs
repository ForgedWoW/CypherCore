// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Chat;

internal class ChatAddonMessageTargeted : ClientPacket
{
    public ObjectGuid? ChannelGUID;
    public ChatAddonMessageParams Params = new();

    public string Target;

    // not optional in the packet. Optional for api reasons
    public ChatAddonMessageTargeted(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var targetLen = WorldPacket.ReadBits<uint>(9);
        Params.Read(WorldPacket);
        ChannelGUID = WorldPacket.ReadPackedGuid();
        Target = WorldPacket.ReadString(targetLen);
    }
}