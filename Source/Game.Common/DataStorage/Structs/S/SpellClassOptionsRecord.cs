// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellClassOptionsRecord
{
	public uint Id;
	public uint SpellID;
	public uint ModalNextSpell;
	public byte SpellClassSet;
	public FlagArray128 SpellClassMask;
}
