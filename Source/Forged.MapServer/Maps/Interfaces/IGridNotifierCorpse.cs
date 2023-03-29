﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;

namespace Forged.MapServer.Maps.Interfaces;

public interface IGridNotifierCorpse : IGridNotifier
{
    void Visit(IList<Corpse> objs);
}