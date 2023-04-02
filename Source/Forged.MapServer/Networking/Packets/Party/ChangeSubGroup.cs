// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class ChangeSubGroup : ClientPacket
{
    public byte NewSubGroup;
    public sbyte PartyIndex;
    public ObjectGuid TargetGUID;
    public ChangeSubGroup(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        TargetGUID = WorldPacket.ReadPackedGuid();
        PartyIndex = WorldPacket.ReadInt8();
        NewSubGroup = WorldPacket.ReadUInt8();
    }
}