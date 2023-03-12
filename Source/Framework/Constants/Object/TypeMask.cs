// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum TypeMask
{
	Object = 0x01,
	Item = 0x02,
	Container = 0x04,
	AzeriteEmpoweredItem = 0x08,
	AzeriteItem = 0x10,
	Unit = 0x20,
	Player = 0x40,
	ActivePlayer = 0x80,
	GameObject = 0x100,
	DynamicObject = 0x200,
	Corpse = 0x400,
	AreaTrigger = 0x800,
	SceneObject = 0x1000,
	Conversation = 0x2000,
	Seer = Player | Unit | DynamicObject
}