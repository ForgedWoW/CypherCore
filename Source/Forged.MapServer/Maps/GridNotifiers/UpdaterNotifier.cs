// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Maps.GridNotifiers;

public class UpdaterNotifier : IGridNotifierWorldObject
{
    private readonly uint _timeDiff;
    private readonly ConcurrentBag<WorldObject> _worldObjects = new();

    public UpdaterNotifier(uint diff, GridType gridType)
    {
        _timeDiff = diff;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void ExecuteUpdate()
    {
        foreach (var obj in _worldObjects)
            try
            {
                obj.Update(_timeDiff);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void Visit(IList<WorldObject> objs)
    {
        foreach (var obj in objs)
        {
            if (obj == null || obj.IsTypeId(TypeId.Player) || obj.IsTypeId(TypeId.Corpse))
                continue;

            if (obj.Location.IsInWorld)
                _worldObjects.Add(obj);
        }
    }
}