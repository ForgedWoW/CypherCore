// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.DataStorage.Structs.S;
using Game.Common.Entities.Units;

namespace Game.Common.Entities;

public class Puppet : Minion
{
	public Puppet(SummonPropertiesRecord propertiesRecord, Unit owner) : base(propertiesRecord, owner, false)
	{
		UnitTypeMask |= UnitTypeMask.Puppet;
	}

	public override void InitStats(uint duration)
	{
		base.InitStats(duration);

		SetLevel(OwnerUnit.Level);
		ReactState = ReactStates.Passive;
	}

	public override void Update(uint diff)
	{
		base.Update(diff);

		//check if caster is channelling?
		if (IsInWorld)
			if (!IsAlive)
				UnSummon();
		// @todo why long distance .die does not remove it
	}
}
