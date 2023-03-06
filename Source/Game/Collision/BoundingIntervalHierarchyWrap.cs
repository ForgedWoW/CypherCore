﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.GameMath;

namespace Game.Collision
{
    public class BIHWrap<T> where T : IModel
    {
        public void Insert(T obj)
        {
            ++unbalanced_times;

            lock (m_objects)
                m_objects_to_push.Add(obj);
        }
        public void Remove(T obj)
        {
            ++unbalanced_times;
            lock (m_objects)
                if (m_obj2Idx.TryGetValue(obj, out int Idx))
                    m_objects[Idx] = null;
                else
                    m_objects_to_push.Remove(obj);
        }

        public void Balance()
        {
            if (unbalanced_times == 0)
                return;

            lock (m_objects)
            {
                unbalanced_times = 0;
                m_objects.Clear();
                m_objects.AddRange(m_obj2Idx.Keys);
                m_objects.AddRange(m_objects_to_push);

                m_tree.Build(m_objects);
            }
        }

        public void IntersectRay(Ray ray, WorkerCallback intersectCallback, ref float maxDist)
        {
            lock (m_objects)
            {
                Balance();
                MDLCallback temp_cb = new(intersectCallback, m_objects.ToArray(), m_objects.Count);
                m_tree.IntersectRay(ray, temp_cb, ref maxDist, true);
            }
        }

        public void IntersectPoint(Vector3 point, WorkerCallback intersectCallback)
        {
            lock (m_objects)
            {
                Balance();
                MDLCallback callback = new(intersectCallback, m_objects.ToArray(), m_objects.Count);
                m_tree.IntersectPoint(point, callback);
            }
        }

        readonly BIH m_tree = new();
        readonly List<T> m_objects = new();
        readonly Dictionary<T, int> m_obj2Idx = new();
        readonly HashSet<T> m_objects_to_push = new();
        int unbalanced_times;

        public class MDLCallback : WorkerCallback
        {
            readonly T[] objects;
            readonly WorkerCallback _callback;
            readonly int objects_size;

            public MDLCallback(WorkerCallback callback, T[] objects_array, int size)
            {
                objects = objects_array;
                _callback = callback;
                objects_size = size;
            }

            /// Intersect ray
            public override bool Invoke(Ray ray, int idx, ref float maxDist, bool stopAtFirst)
            {
                if (idx >= objects_size)
                    return false;

                T obj = objects[idx];
                if (obj != null)
                    return _callback.Invoke(ray, obj, ref maxDist);
                return false;
            }

            /// Intersect point
            public override void Invoke(Vector3 p, int idx)
            {
                if (idx >= objects_size)
                    return;

                T obj = objects[idx];
                if (obj != null)
                    _callback.Invoke(p, obj as GameObjectModel);
            }
        }
    }
}
