// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class Runes
{
    public List<byte> CooldownOrder { get; set; } = new();
    public uint[] Cooldown { get; set; } = new uint[PlayerConst.MaxRunes];
    public byte RuneState { get; set; } // mask of available runes

    public void SetRuneState(byte index, bool set = true)
    {
        var foundRune = CooldownOrder.Contains(index);

        if (set)
        {
            RuneState |= (byte)(1 << index); // usable

            if (foundRune)
                CooldownOrder.Remove(index);
        }
        else
        {
            RuneState &= (byte)~(1 << index); // on cooldown

            if (!foundRune)
                CooldownOrder.Add(index);
        }
    }
}