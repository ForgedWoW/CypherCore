// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Web.API;

public class ApiRequest<T>
{
    public Func<T, bool> SearchFunc { get; set; }
    public uint? SearchId { get; set; }
}