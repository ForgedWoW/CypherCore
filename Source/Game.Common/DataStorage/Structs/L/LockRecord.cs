// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Common.DataStorage.Structs.L;

public sealed class LockRecord
{
	public uint Id;
	public int Flags;
	public int[] Index = new int[SharedConst.MaxLockCase];
	public ushort[] Skill = new ushort[SharedConst.MaxLockCase];
	public byte[] LockType = new byte[SharedConst.MaxLockCase];
	public byte[] Action = new byte[SharedConst.MaxLockCase];
}
