// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Units;

public class UnitActionBarEntry
{
    public uint PackedData;

    public UnitActionBarEntry()
    {
        PackedData = (uint)ActiveStates.Disabled << 24;
    }

    public static uint MAKE_UNIT_ACTION_BUTTON(uint action, uint type)
    {
        return action | (type << 24);
    }

    public static uint UNIT_ACTION_BUTTON_ACTION(uint packedData)
    {
        return packedData & 0x00FFFFFF;
    }

    public static uint UNIT_ACTION_BUTTON_TYPE(uint packedData)
    {
        return (packedData & 0xFF000000) >> 24;
    }

    public uint GetAction()
    {
        return UNIT_ACTION_BUTTON_ACTION(PackedData);
    }

    public ActiveStates GetActiveState()
    {
        return (ActiveStates)UNIT_ACTION_BUTTON_TYPE(PackedData);
    }
    public bool IsActionBarForSpell()
    {
        var type = GetActiveState();

        return type is ActiveStates.Disabled or ActiveStates.Enabled or ActiveStates.Passive;
    }

    public void SetAction(uint action)
    {
        PackedData = (PackedData & 0xFF000000) | UNIT_ACTION_BUTTON_ACTION(action);
    }

    public void SetActionAndType(uint action, ActiveStates type)
    {
        PackedData = MAKE_UNIT_ACTION_BUTTON(action, (uint)type);
    }

    public void SetType(ActiveStates type)
    {
        PackedData = MAKE_UNIT_ACTION_BUTTON(UNIT_ACTION_BUTTON_ACTION(PackedData), (uint)type);
    }
}