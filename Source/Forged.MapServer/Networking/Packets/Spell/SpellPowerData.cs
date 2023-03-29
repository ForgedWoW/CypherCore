// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public struct SpellPowerData
{
    public int Cost;
    public PowerType Type;

    public void Write(WorldPacket data)
    {
        data.WriteInt32(Cost);
        data.WriteInt8((sbyte)Type);
    }
}