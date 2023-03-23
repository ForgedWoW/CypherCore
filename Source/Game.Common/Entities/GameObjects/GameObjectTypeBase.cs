// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.GameObjects;
using Game.Entities;

namespace Game.Common.Entities.GameObjects;

class GameObjectTypeBase
{
	protected readonly GameObject Owner;

	public GameObjectTypeBase(GameObject owner)
	{
		Owner = owner;
	}

	public virtual void Update(uint diff) { }
	public virtual void OnStateChanged(GameObjectState oldState, GameObjectState newState) { }
	public virtual void OnRelocated() { }

	public class CustomCommand
	{
		public virtual void Execute(GameObjectTypeBase type) { }
	}
}
