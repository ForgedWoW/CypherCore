// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Entities.Units;

public class DispelInfo
{
	readonly WorldObject _dispeller;
	readonly uint _dispellerSpell;
	byte _chargesRemoved;

	public DispelInfo(WorldObject dispeller, uint dispellerSpellId, byte chargesRemoved)
	{
		_dispeller = dispeller;
		_dispellerSpell = dispellerSpellId;
		_chargesRemoved = chargesRemoved;
	}

	public WorldObject GetDispeller()
	{
		return _dispeller;
	}

	public byte GetRemovedCharges()
	{
		return _chargesRemoved;
	}

	public void SetRemovedCharges(byte amount)
	{
		_chargesRemoved = amount;
	}

	uint GetDispellerSpellId()
	{
		return _dispellerSpell;
	}
}
