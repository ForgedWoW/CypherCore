// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseLocale
{
    public StringArray Answer { get; set; } = new((int)Locale.Total);
    public StringArray ButtonTooltip { get; set; } = new((int)Locale.Total);
    public StringArray Confirmation { get; set; } = new((int)Locale.Total);
    public StringArray Description { get; set; } = new((int)Locale.Total);
    public StringArray Header { get; set; } = new((int)Locale.Total);
    public StringArray SubHeader { get; set; } = new((int)Locale.Total);
}