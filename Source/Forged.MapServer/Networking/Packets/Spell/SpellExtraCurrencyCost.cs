// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Spell;

public struct SpellExtraCurrencyCost
{
    public int Count;
    public int CurrencyID;
    public void Read(WorldPacket data)
    {
        CurrencyID = data.ReadInt32();
        Count = data.ReadInt32();
    }
}