// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;
//To all Immune system, if target has immunes,
//some spell that related to ImmuneToDispel or ImmuneToSchool or ImmuneToDamage type can't cast to it,
//some spell_effects that related to ImmuneToEffect<effect>(only this effect in the spell) can't cast to it,
//some aura(related to Mechanics or ImmuneToState<aura>) can't apply to it.

[Flags]
public enum UnitDynFlags
{
    None = 0x00,
    HideModel = 0x02, // Object model is not shown with this flag
    Lootable = 0x04,
    TrackUnit = 0x08,
    Tapped = 0x10, // Lua_UnitIsTapped
    SpecialInfo = 0x20,
    CanSkin = 0x40, // previously UNIT_DYNFLAG_DEAD
    ReferAFriend = 0x80
}