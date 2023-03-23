using Game;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Warden;

public enum WardenCheckCategory
{
	Inject = 0, // checks that test whether the client's execution has been interfered with
	Lua,        // checks that test whether the lua sandbox has been modified
	Modded,     // checks that test whether the client has been modified

	Max // SKIP
}
