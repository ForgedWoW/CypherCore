// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Common.Entities.GameObjects;

public class GameObjectAddon
{
	public Quaternion ParentRotation;
	public InvisibilityType invisibilityType;
	public uint invisibilityValue;
	public uint WorldEffectID;
	public uint AIAnimKitID;
}
