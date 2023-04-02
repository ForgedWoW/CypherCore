// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Duel;

public class DuelResponse : ClientPacket
{
    public bool Accepted;
    public ObjectGuid ArbiterGUID;
    public bool Forfeited;
    public DuelResponse(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ArbiterGUID = _worldPacket.ReadPackedGuid();
        Accepted = _worldPacket.HasBit();
        Forfeited = _worldPacket.HasBit();
    }
}