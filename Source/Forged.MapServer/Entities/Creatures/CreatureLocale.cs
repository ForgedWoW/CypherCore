// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureLocale
{
	public StringArray Name = new((int)Locale.Total);
	public StringArray NameAlt = new((int)Locale.Total);
	public StringArray Title = new((int)Locale.Total);
	public StringArray TitleAlt = new((int)Locale.Total);
}