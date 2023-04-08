// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.ClientReader;

public class LocalizedString
{
    private readonly StringArray _stringStorage = new((int)Locale.Total);

    public string this[Locale locale]
    {
        get => _stringStorage[(int)locale] ?? "";
        set => _stringStorage[(int)locale] = value;
    }

    public bool HasString(Locale locale = SharedConst.DefaultLocale)
    {
        return !string.IsNullOrEmpty(_stringStorage[(int)locale]);
    }
}