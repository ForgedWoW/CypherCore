// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class SetRole : ClientPacket
{
    public sbyte PartyIndex;
    public ObjectGuid TargetGUID;
    public int Role;
    public SetRole(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = _worldPacket.ReadInt8();
        TargetGUID = _worldPacket.ReadPackedGuid();
        Role = _worldPacket.ReadInt32();
    }
}