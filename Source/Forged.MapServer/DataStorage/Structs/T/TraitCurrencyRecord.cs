// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TraitCurrencyRecord
{
    public uint Id;
    public int Type;
    public int CurrencyTypesID;
    public int Flags;
    public int Icon;

    public TraitCurrencyType GetCurrencyType()
    {
        return (TraitCurrencyType)Type;
    }
}