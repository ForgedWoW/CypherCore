// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Collision.Models;

namespace Forged.MapServer.Collision.Management;

public class ManagedModel
{
    private int _count;
    private WorldModel _model;

    public ManagedModel()
    {
        _model = new WorldModel();
        _count = 0;
    }

    public int DecRefCount()
    {
        return --_count;
    }

    public WorldModel GetModel()
    {
        return _model;
    }

    public void IncRefCount()
    {
        ++_count;
    }

    public void SetModel(WorldModel model)
    {
        _model = model;
    }
}