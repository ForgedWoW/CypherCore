﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO)
    internal class NearestGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
    {
        private readonly uint _entry;

        private readonly WorldObject _obj;
        private readonly bool _spawnedOnly;
        private float _range;

        public NearestGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range, bool spawnedOnly = true)
        {
            _obj = obj;
            _entry = entry;
            _range = range;
            _spawnedOnly = spawnedOnly;
        }

        public bool Invoke(GameObject go)
        {
            if ((!_spawnedOnly || go.IsSpawned()) &&
                go.GetEntry() == _entry &&
                go.GetGUID() != _obj.GetGUID() &&
                _obj.IsWithinDist(go, _range))
            {
                _range = _obj.GetDistance(go); // use found GO range as new range limit for next check

                return true;
            }

            return false;
        }
    }
}