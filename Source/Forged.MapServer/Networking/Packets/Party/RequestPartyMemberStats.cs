// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class RequestPartyMemberStats : ClientPacket
{
    public byte PartyIndex;
    public ObjectGuid TargetGUID;
    public RequestPartyMemberStats(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = _worldPacket.ReadUInt8();
        TargetGUID = _worldPacket.ReadPackedGuid();
    }
}