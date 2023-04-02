// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class UpdateRaidTarget : ClientPacket
{
    public sbyte PartyIndex;
    public sbyte Symbol;
    public ObjectGuid Target;
    public UpdateRaidTarget(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = WorldPacket.ReadInt8();
        Target = WorldPacket.ReadPackedGuid();
        Symbol = WorldPacket.ReadInt8();
    }
}