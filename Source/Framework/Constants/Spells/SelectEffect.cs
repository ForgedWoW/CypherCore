// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SelectEffect
{
	DontCare = 0, //All spell effects allowed
	Damage,       //Spell does damage
	Healing,      //Spell does healing
	Aura          //Spell applies an aura
}