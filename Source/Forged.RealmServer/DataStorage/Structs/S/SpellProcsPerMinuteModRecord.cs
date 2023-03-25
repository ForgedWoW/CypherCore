// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class SpellProcsPerMinuteModRecord
{
	public uint Id;
	public SpellProcsPerMinuteModType Type;
	public uint Param;
	public float Coeff;
	public uint SpellProcsPerMinuteID;
}