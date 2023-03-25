// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class PlayerCurrency
{
	public PlayerCurrencyState State { get; set; }
	public uint Quantity { get; set; }
	public uint WeeklyQuantity { get; set; }
	public uint TrackedQuantity { get; set; }
	public uint IncreasedCapQuantity { get; set; }
	public uint EarnedQuantity { get; set; }
	public CurrencyDbFlags Flags { get; set; }
}