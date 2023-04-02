﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Collision.Models;
using Framework.Constants;
using Framework.GameMath;

namespace Forged.MapServer.Collision;

public class MapRayCallback : WorkerCallback
{
    private readonly ModelIgnoreFlags _flags;
    private readonly ModelInstance[] _prims;
    private bool _hit;

    public MapRayCallback(ModelInstance[] val, ModelIgnoreFlags ignoreFlags)
    {
        _prims = val;
        _hit = false;
        _flags = ignoreFlags;
    }

    public bool DidHit()
    {
        return _hit;
    }

    public override bool Invoke(Ray ray, int entry, ref float distance, bool pStopAtFirstHit = true)
    {
        if (_prims[entry] == null)
            return false;

        var result = _prims[entry].IntersectRay(ray, ref distance, pStopAtFirstHit, _flags);

        if (result)
            _hit = true;

        return result;
    }
}