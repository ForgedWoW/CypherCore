// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Warden;

public enum WardenCheckType
{
	None = 0,
	Timing = 87,   // nyi
	Driver = 113,  // uint Seed + byte[20] SHA1 + byte driverNameIndex (check to ensure driver isn't loaded)
	Proc = 126,    // nyi
	LuaEval = 139, // evaluate arbitrary Lua check
	Mpq = 152,     // get hash of MPQ file (to check it is not modified)
	PageA = 178,   // scans all pages for specified SHA1 hash
	PageB = 191,   // scans only pages starts with MZ+PE headers for specified hash
	Module = 217,  // check to make sure module isn't injected
	Mem = 243,     // retrieve specific memory
}
