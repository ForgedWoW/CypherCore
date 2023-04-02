// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Warden;

public class WardenCheck
{
    public WardenActions Action;
    public uint Address;
    public ushort CheckId;
    public string Comment;
    public byte[] Data;
    public char[] IdStr = new char[4];
    // PROC_CHECK, MEM_CHECK, PAGE_CHECK
    public byte Length;

    // PROC_CHECK, MEM_CHECK, PAGE_CHECK
    public string Str;

    public WardenCheckType Type;
    // LUA, MPQ, DRIVER
    // LUA
}