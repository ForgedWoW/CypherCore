// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum GridMapTypeMask
{
	None = 0x00,
	Corpse = 0x01,
	Creature = 0x02,
	DynamicObject = 0x04,
	GameObject = 0x08,
	Player = 0x10,
	AreaTrigger = 0x20,
	SceneObject = 0x40,
	Conversation = 0x80,

	All = 0xFF,

	//GameObjects, Creatures(except pets), DynamicObject, Corpse(Bones), AreaTrigger, SceneObject
	AllGrid = GameObject | Creature | DynamicObject | Corpse | AreaTrigger | SceneObject | Conversation,

	//Player, Pets, Corpse(resurrectable), DynamicObject(farsight)
	AllWorld = Player | Creature | Corpse | DynamicObject
}