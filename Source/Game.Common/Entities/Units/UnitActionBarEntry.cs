// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities;
using Game.Entities;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Entities.Units;

public class UnitActionBarEntry
{
	public uint packedData;

	public UnitActionBarEntry()
	{
		packedData = (uint)ActiveStates.Disabled << 24;
	}

	public ActiveStates GetActiveState()
	{
		return (ActiveStates)UNIT_ACTION_BUTTON_TYPE(packedData);
	}

	public uint GetAction()
	{
		return UNIT_ACTION_BUTTON_ACTION(packedData);
	}

	public bool IsActionBarForSpell()
	{
		var Type = GetActiveState();

		return Type == ActiveStates.Disabled || Type == ActiveStates.Enabled || Type == ActiveStates.Passive;
	}

	public void SetActionAndType(uint action, ActiveStates type)
	{
		packedData = MAKE_UNIT_ACTION_BUTTON(action, (uint)type);
	}

	public void SetType(ActiveStates type)
	{
		packedData = MAKE_UNIT_ACTION_BUTTON(UNIT_ACTION_BUTTON_ACTION(packedData), (uint)type);
	}

	public void SetAction(uint action)
	{
		packedData = (packedData & 0xFF000000) | UNIT_ACTION_BUTTON_ACTION(action);
	}

	public static uint MAKE_UNIT_ACTION_BUTTON(uint action, uint type)
	{
		return (action | (type << 24));
	}

	public static uint UNIT_ACTION_BUTTON_ACTION(uint packedData)
	{
		return (packedData & 0x00FFFFFF);
	}

	public static uint UNIT_ACTION_BUTTON_TYPE(uint packedData)
	{
		return ((packedData & 0xFF000000) >> 24);
	}
}
