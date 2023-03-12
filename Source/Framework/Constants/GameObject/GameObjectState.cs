// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GameObjectState
{
	Active = 0,
	Ready = 1,
	Destroyed = 2,
	TransportActive = 24,
	TransportStopped = 25,
	Max = 3
}