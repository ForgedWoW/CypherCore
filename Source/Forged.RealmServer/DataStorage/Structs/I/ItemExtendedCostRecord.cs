// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class ItemExtendedCostRecord
{
	public uint Id;
	public ushort RequiredArenaRating;
	public byte ArenaBracket; // arena slot restrictions (min slot value)
	public byte Flags;
	public byte MinFactionID;
	public int MinReputation;
	public byte RequiredAchievement;                                             // required personal arena rating
	public uint[] ItemID = new uint[ItemConst.MaxItemExtCostItems];              // required item id
	public ushort[] ItemCount = new ushort[ItemConst.MaxItemExtCostItems];       // required count of 1st item
	public ushort[] CurrencyID = new ushort[ItemConst.MaxItemExtCostCurrencies]; // required curency id
	public uint[] CurrencyCount = new uint[ItemConst.MaxItemExtCostCurrencies];  // required curency count
}