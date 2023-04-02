// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LfgOfferContinue : ServerPacket
{
    public uint Slot;

    public LfgOfferContinue(uint slot) : base(ServerOpcodes.LfgOfferContinue, ConnectionType.Instance)
    {
        Slot = slot;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32(Slot);
    }
}