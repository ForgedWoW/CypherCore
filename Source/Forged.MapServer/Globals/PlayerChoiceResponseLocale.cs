using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseLocale
{
    public StringArray Answer = new((int)Locale.Total);
    public StringArray Header = new((int)Locale.Total);
    public StringArray SubHeader = new((int)Locale.Total);
    public StringArray ButtonTooltip = new((int)Locale.Total);
    public StringArray Description = new((int)Locale.Total);
    public StringArray Confirmation = new((int)Locale.Total);
}