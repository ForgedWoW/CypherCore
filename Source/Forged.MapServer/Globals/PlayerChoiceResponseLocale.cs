// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseLocale
{
    public StringArray Answer = new((int)Locale.Total);
    public StringArray ButtonTooltip = new((int)Locale.Total);
    public StringArray Confirmation = new((int)Locale.Total);
    public StringArray Description = new((int)Locale.Total);
    public StringArray Header = new((int)Locale.Total);
    public StringArray SubHeader = new((int)Locale.Total);
}