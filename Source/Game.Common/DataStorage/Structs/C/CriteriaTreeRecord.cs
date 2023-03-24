// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Common.DataStorage.Structs.C;

public sealed class CriteriaTreeRecord
{
	public uint Id;
	public string Description;
	public uint Parent;
	public uint Amount;
	public sbyte Operator;
	public uint CriteriaID;
	public int OrderIndex;
	public CriteriaTreeFlags Flags;
}
