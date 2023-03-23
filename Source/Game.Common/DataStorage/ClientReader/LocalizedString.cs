// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;
using Game.DataStorage;

namespace Game.Common.DataStorage.ClientReader;

public class LocalizedString
{
	readonly StringArray stringStorage = new((int)Locale.Total);

	public string this[Locale locale]
	{
		get { return stringStorage[(int)locale] ?? ""; }
		set { stringStorage[(int)locale] = value; }
	}

	public bool HasString(Locale locale = SharedConst.DefaultLocale)
	{
		return !string.IsNullOrEmpty(stringStorage[(int)locale]);
	}
}
