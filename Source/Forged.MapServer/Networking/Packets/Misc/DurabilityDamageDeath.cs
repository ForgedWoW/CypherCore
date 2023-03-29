// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class DurabilityDamageDeath : ServerPacket
{
    public uint Percent;
    public DurabilityDamageDeath() : base(ServerOpcodes.DurabilityDamageDeath) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(Percent);
    }
}