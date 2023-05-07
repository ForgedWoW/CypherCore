// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TaxiPathRecord
{
    public uint Cost;
    public ushort FromTaxiNode;
    public uint Id;
    public ushort ToTaxiNode;
}