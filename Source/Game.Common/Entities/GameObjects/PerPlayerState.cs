// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Common.Entities.GameObjects;

public class PerPlayerState
{
	public GameObjectState? State;
	public DateTime ValidUntil { get; set; } = DateTime.MinValue;
	public bool Despawned { get; set; }
}
