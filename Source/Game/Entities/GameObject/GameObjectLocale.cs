using Framework.Collections;
using Framework.Constants;

namespace Game.Entities;

public class GameObjectLocale
{
	public StringArray Name = new((int)Locale.Total);
	public StringArray CastBarCaption = new((int)Locale.Total);
	public StringArray Unk1 = new((int)Locale.Total);
}