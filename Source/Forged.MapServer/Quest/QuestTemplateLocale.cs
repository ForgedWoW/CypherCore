// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Quest;

public class QuestTemplateLocale
{
    public StringArray AreaDescription = new((int)Locale.Total);
    public StringArray LogDescription = new((int)Locale.Total);
    public StringArray LogTitle = new((int)Locale.Total);
    public StringArray PortraitGiverName = new((int)Locale.Total);
    public StringArray PortraitGiverText = new((int)Locale.Total);
    public StringArray PortraitTurnInName = new((int)Locale.Total);
    public StringArray PortraitTurnInText = new((int)Locale.Total);
    public StringArray QuestCompletionLog = new((int)Locale.Total);
    public StringArray QuestDescription = new((int)Locale.Total);
}