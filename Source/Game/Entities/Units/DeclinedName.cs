// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Game.Entities;

public class DeclinedName
{
	public StringArray Name = new(SharedConst.MaxDeclinedNameCases);
}