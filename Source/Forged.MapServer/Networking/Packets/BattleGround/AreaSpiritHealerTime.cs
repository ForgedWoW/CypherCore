// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class AreaSpiritHealerTime : ServerPacket
{
    public ObjectGuid HealerGuid;
    public uint TimeLeft;

    public AreaSpiritHealerTime() : base(ServerOpcodes.AreaSpiritHealerTime) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(HealerGuid);
        WorldPacket.WriteUInt32(TimeLeft);
    }
}