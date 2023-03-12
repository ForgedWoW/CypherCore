// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LootState
{
	NotReady = 0,
	Ready, // can be ready but despawned, and then not possible activate until spawn
	Activated,
	JustDeactivated
}