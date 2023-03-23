// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.S;

public sealed class SummonPropertiesRecord
{
	public uint Id;
	public SummonCategory Control;
	public uint Faction;
	public SummonTitle Title;
	public int Slot;
	public uint[] Flags = new uint[2];

	public SummonPropertiesFlags GetFlags()
	{
		return (SummonPropertiesFlags)Flags[0];
	}
}
