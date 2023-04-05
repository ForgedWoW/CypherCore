// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureLocale
{
    public StringArray Name { get; set; } = new((int)Locale.Total);
    public StringArray NameAlt { get; set; } = new((int)Locale.Total);
    public StringArray Title { get; set; } = new((int)Locale.Total);
    public StringArray TitleAlt { get; set; } = new((int)Locale.Total);
}