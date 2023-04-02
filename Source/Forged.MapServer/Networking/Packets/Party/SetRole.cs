// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class SetRole : ClientPacket
{
    public sbyte PartyIndex;
    public int Role;
    public ObjectGuid TargetGUID;
    public SetRole(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = WorldPacket.ReadInt8();
        TargetGUID = WorldPacket.ReadPackedGuid();
        Role = WorldPacket.ReadInt32();
    }
}