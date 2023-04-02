// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemExtendedCostRecord
{
    public byte ArenaBracket;
    public uint[] CurrencyCount = new uint[ItemConst.MaxItemExtCostCurrencies];
    public ushort[] CurrencyID = new ushort[ItemConst.MaxItemExtCostCurrencies];
    // arena slot restrictions (min slot value)
    public byte Flags;

    public uint Id;
    public ushort[] ItemCount = new ushort[ItemConst.MaxItemExtCostItems];
    public uint[] ItemID = new uint[ItemConst.MaxItemExtCostItems];
    public byte MinFactionID;
    public int MinReputation;
    public byte RequiredAchievement;
    public ushort RequiredArenaRating;
    // required personal arena rating
    // required item id
    // required count of 1st item
    // required curency id
    // required curency count
}