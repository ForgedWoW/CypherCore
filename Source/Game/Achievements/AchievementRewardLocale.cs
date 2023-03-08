using Framework.Collections;
using Framework.Constants;

namespace Game.Achievements;

public class AchievementRewardLocale
{
	public StringArray Subject = new((int)Locale.Total);
	public StringArray Body = new((int)Locale.Total);
}