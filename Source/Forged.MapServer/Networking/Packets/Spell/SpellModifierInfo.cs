// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellModifierInfo
{
    public byte ModIndex;
    public List<SpellModifierData> ModifierData = new();

    public void Write(WorldPacket data)
    {
        data.WriteUInt8(ModIndex);
        data.WriteInt32(ModifierData.Count);

        foreach (var modData in ModifierData)
            modData.Write(data);
    }
}