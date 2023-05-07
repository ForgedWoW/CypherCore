// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellReagentsCurrencyRecord
{
    public ushort CurrencyCount;
    public ushort CurrencyTypesID;
    public uint Id;
    public int SpellID;
}