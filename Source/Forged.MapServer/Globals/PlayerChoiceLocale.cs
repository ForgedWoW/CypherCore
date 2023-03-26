using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class PlayerChoiceLocale
{
    public StringArray Question = new((int)Locale.Total);
    public Dictionary<int /*ResponseId*/, PlayerChoiceResponseLocale> Responses = new();
}