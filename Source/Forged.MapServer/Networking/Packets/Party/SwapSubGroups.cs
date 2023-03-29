// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class SwapSubGroups : ClientPacket
{
    public ObjectGuid FirstTarget;
    public ObjectGuid SecondTarget;
    public sbyte PartyIndex;
    public SwapSubGroups(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = _worldPacket.ReadInt8();
        FirstTarget = _worldPacket.ReadPackedGuid();
        SecondTarget = _worldPacket.ReadPackedGuid();
    }
}