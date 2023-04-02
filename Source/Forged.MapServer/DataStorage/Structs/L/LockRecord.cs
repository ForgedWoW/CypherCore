// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed class LockRecord
{
    public byte[] Action = new byte[SharedConst.MaxLockCase];
    public int Flags;
    public uint Id;
    public int[] Index = new int[SharedConst.MaxLockCase];
    public byte[] LockType = new byte[SharedConst.MaxLockCase];
    public ushort[] Skill = new ushort[SharedConst.MaxLockCase];
}