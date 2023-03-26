// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Combat;

public class PvPCombatReference : CombatReference
{
	public static uint PVP_COMBAT_TIMEOUT = 5 * Time.InMilliseconds;

    private uint _combatTimer = PVP_COMBAT_TIMEOUT;


	public PvPCombatReference(Unit first, Unit second) : base(first, second, true) { }

	public bool Update(uint tdiff)
	{
		if (_combatTimer <= tdiff)
			return false;

		_combatTimer -= tdiff;

		return true;
	}

	public void RefreshTimer()
	{
		_combatTimer = PVP_COMBAT_TIMEOUT;
	}
}