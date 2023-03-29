// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.ClientConfig;

internal class SetAdvancedCombatLogging : ClientPacket
{
    public bool Enable;
    public SetAdvancedCombatLogging(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Enable = _worldPacket.HasBit();
    }
}