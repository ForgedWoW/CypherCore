// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Quest;

public class QuestTemplateLocale
{
    public StringArray AreaDescription { get; set; } = new((int)Locale.Total);
    public StringArray LogDescription { get; set; } = new((int)Locale.Total);
    public StringArray LogTitle { get; set; } = new((int)Locale.Total);
    public StringArray PortraitGiverName { get; set; } = new((int)Locale.Total);
    public StringArray PortraitGiverText { get; set; } = new((int)Locale.Total);
    public StringArray PortraitTurnInName { get; set; } = new((int)Locale.Total);
    public StringArray PortraitTurnInText { get; set; } = new((int)Locale.Total);
    public StringArray QuestCompletionLog { get; set; } = new((int)Locale.Total);
    public StringArray QuestDescription { get; set; } = new((int)Locale.Total);
}