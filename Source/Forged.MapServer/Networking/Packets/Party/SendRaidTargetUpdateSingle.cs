// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class SendRaidTargetUpdateSingle : ServerPacket
{
    public ObjectGuid ChangedBy;
    public sbyte PartyIndex;
    public sbyte Symbol;
    public ObjectGuid Target;
    public SendRaidTargetUpdateSingle() : base(ServerOpcodes.SendRaidTargetUpdateSingle) { }

    public override void Write()
    {
        WorldPacket.WriteInt8(PartyIndex);
        WorldPacket.WriteInt8(Symbol);
        WorldPacket.WritePackedGuid(Target);
        WorldPacket.WritePackedGuid(ChangedBy);
    }
}