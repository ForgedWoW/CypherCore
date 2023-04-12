// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class GossipMenuItemsLocale
{
    public StringArray BoxText { get; set; } = new((int)Locale.Total);
    public StringArray OptionText { get; set; } = new((int)Locale.Total);
}