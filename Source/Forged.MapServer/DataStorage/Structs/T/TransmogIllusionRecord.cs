// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class TransmogIllusionRecord
{
	public uint Id;
	public int UnlockConditionID;
	public int TransmogCost;
	public int SpellItemEnchantmentID;
	public int Flags;

	public TransmogIllusionFlags GetFlags()
	{
		return (TransmogIllusionFlags)Flags;
	}
}