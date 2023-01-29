﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.GameMath;

namespace Game.Collision
{
    public class WModelRayCallBack : WorkerCallback
    {
        public bool Hit { get; set; }
        private readonly List<GroupModel> _models;

        public WModelRayCallBack(List<GroupModel> mod)
        {
            _models = mod;
            Hit = false;
        }

        public override bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit)
        {
            bool result = _models[(int)entry].IntersectRay(ray, ref distance, pStopAtFirstHit);
            if (result) Hit = true;

            return Hit;
        }
    }
}