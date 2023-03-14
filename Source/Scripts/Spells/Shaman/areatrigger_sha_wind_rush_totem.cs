// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Shaman;

[Script] //  12676 - AreaTriggerId
internal class areatrigger_sha_wind_rush_totem : AreaTriggerScript, IAreaTriggerOnUpdate, IAreaTriggerOnUnitEnter, IAreaTriggerOnCreate
{
	private static readonly int REFRESH_TIME = 4500;

	private int _refreshTimer;

	public void OnCreate()
	{
		_refreshTimer = REFRESH_TIME;
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
		{
			if (!caster.IsFriendlyTo(unit))
				return;

			caster.CastSpell(unit, ShamanSpells.WindRush, true);
		}
	}

	public void OnUpdate(uint diff)
	{
		_refreshTimer -= (int)diff;

		if (_refreshTimer <= 0)
		{
			var caster = At.GetCaster();

			if (caster != null)
				foreach (var guid in At.InsideUnits)
				{
					var unit = Global.ObjAccessor.GetUnit(caster, guid);

					if (unit != null)
					{
						if (!caster.IsFriendlyTo(unit))
							continue;

						caster.CastSpell(unit, ShamanSpells.WindRush, true);
					}
				}

			_refreshTimer += REFRESH_TIME;
		}
	}
}