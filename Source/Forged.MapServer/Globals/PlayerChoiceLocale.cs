// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class PlayerChoiceLocale
{
    public StringArray Question { get; set; } = new((int)Locale.Total);
    public Dictionary<int /*ResponseId*/, PlayerChoiceResponseLocale> Responses { get; set; } = new();
}