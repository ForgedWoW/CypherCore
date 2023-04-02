// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

public class RealmBuildInfo
{
    public uint BugfixVersion;
    public uint Build;
    public char[] HotfixVersion = new char[4];
    public byte[] Mac64AuthSeed = new byte[16];
    public uint MajorVersion;
    public uint MinorVersion;
    public byte[] Win64AuthSeed = new byte[16];
}