// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellLinkedType
{
	Cast = 0, // +: cast; -: remove
	Hit = 1,
	Aura = 2, // +: aura; -: immune
	Remove = 3
}