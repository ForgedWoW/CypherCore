// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Maps.Interfaces;

public interface IGridNotifierPlayer : IGridNotifier
{
    void Visit(IList<Player> objs);
}