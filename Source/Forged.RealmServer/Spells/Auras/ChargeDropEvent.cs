// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;

namespace Forged.RealmServer.Spells;

public class ChargeDropEvent : BasicEvent
{
	readonly Aura _base;
	readonly AuraRemoveMode _mode;

	public ChargeDropEvent(Aura aura, AuraRemoveMode mode)
	{
		_base = aura;
		_mode = mode;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		// _base is always valid (look in Aura._Remove())
		_base.ModChargesDelayed(-1, _mode);

		return true;
	}
}