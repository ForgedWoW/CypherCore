// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Warden;
using Game;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Warden;

public class WardenCheck
{
	public ushort CheckId;
	public WardenCheckType Type;
	public byte[] Data;
	public uint Address; // PROC_CHECK, MEM_CHECK, PAGE_CHECK
	public byte Length;  // PROC_CHECK, MEM_CHECK, PAGE_CHECK
	public string Str;   // LUA, MPQ, DRIVER
	public string Comment;
	public char[] IdStr = new char[4]; // LUA
	public WardenActions Action;
}
