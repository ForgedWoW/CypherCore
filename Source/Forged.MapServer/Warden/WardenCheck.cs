// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Warden;

public class WardenCheck
{
    public WardenActions Action { get; set; }
    public uint Address { get; set; }
    public ushort CheckId { get; set; }
    public string Comment { get; set; }
    public byte[] Data { get; set; }
    public char[] IdStr { get; set; } = new char[4];
    // PROC_CHECK, MEM_CHECK, PAGE_CHECK
    public byte Length { get; set; }

    // PROC_CHECK, MEM_CHECK, PAGE_CHECK
    public string Str { get; set; }

    public WardenCheckType Type { get; set; }
    // LUA, MPQ, DRIVER
    // LUA
}