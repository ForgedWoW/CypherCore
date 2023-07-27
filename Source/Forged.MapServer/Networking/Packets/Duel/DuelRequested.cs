// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Duel;

public class DuelRequested : ServerPacket
{
    public ObjectGuid ArbiterGUID;
    public ObjectGuid RequestedByGUID;
    public ObjectGuid RequestedByWowAccount;
    public bool ToTheDeath = false;
    public DuelRequested() : base(ServerOpcodes.DuelRequested, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ArbiterGUID);
        WorldPacket.WritePackedGuid(RequestedByGUID);
        WorldPacket.WritePackedGuid(RequestedByWowAccount);
        WorldPacket.WriteBit(ToTheDeath);
        WorldPacket.FlushBits();
    }
}