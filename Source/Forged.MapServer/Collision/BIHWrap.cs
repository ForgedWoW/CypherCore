// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Collision.Models;
using Framework.GameMath;

namespace Forged.MapServer.Collision;

public class BIHWrap<T> where T : Model
{
    private readonly Dictionary<T, int> _obj2Idx = new();
    private readonly List<T> _objects = new();
    private readonly HashSet<T> _objectsToPush = new();
    private readonly BIH _tree = new();
    private int _unbalancedTimes;

    public void Balance()
    {
        if (_unbalancedTimes == 0)
            return;

        lock (_objects)
        {
            _unbalancedTimes = 0;
            _objects.Clear();
            _objects.AddRange(_obj2Idx.Keys);
            _objects.AddRange(_objectsToPush);

            _tree.Build(_objects);
        }
    }

    public void Insert(T obj)
    {
        ++_unbalancedTimes;

        lock (_objects)
        {
            _objectsToPush.Add(obj);
        }
    }

    public void IntersectPoint(Vector3 point, WorkerCallback intersectCallback)
    {
        lock (_objects)
        {
            Balance();
            MdlCallback callback = new(intersectCallback, _objects.ToArray(), _objects.Count);
            _tree.IntersectPoint(point, callback);
        }
    }

    public void IntersectRay(Ray ray, WorkerCallback intersectCallback, ref float maxDist)
    {
        lock (_objects)
        {
            Balance();
            MdlCallback tempCb = new(intersectCallback, _objects.ToArray(), _objects.Count);
            _tree.IntersectRay(ray, tempCb, ref maxDist, true);
        }
    }

    public void Remove(T obj)
    {
        ++_unbalancedTimes;

        lock (_objects)
        {
            if (_obj2Idx.TryGetValue(obj, out var idx))
                _objects[idx] = null;
            else
                _objectsToPush.Remove(obj);
        }
    }

    public class MdlCallback : WorkerCallback
    {
        private readonly WorkerCallback _callback;
        private readonly T[] _objects;
        private readonly int _objectsSize;

        public MdlCallback(WorkerCallback callback, T[] objectsArray, int size)
        {
            _objects = objectsArray;
            _callback = callback;
            _objectsSize = size;
        }

        /// Intersect ray
        public override bool Invoke(Ray ray, int idx, ref float maxDist, bool stopAtFirst)
        {
            if (idx >= _objectsSize)
                return false;

            var obj = _objects[idx];

            return obj != null && _callback.Invoke(ray, obj, ref maxDist);
        }

        /// Intersect point
        public override void Invoke(Vector3 p, int idx)
        {
            if (idx >= _objectsSize)
                return;

            var obj = _objects[idx];

            if (obj != null)
                _callback.Invoke(p, obj as GameObjectModel);
        }
    }
}