// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.GameMath;

namespace Game.Collision;

public class BIHWrap<T> where T : IModel
{
	readonly BIH _tree = new();
	readonly List<T> _objects = new();
	readonly Dictionary<T, int> _obj2Idx = new();
	readonly HashSet<T> _objectsToPush = new();
	int _unbalancedTimes;

	public void Insert(T obj)
	{
		++_unbalancedTimes;

		lock (_objects)
		{
			_objectsToPush.Add(obj);
		}
	}

	public void Remove(T obj)
	{
		++_unbalancedTimes;

		lock (_objects)
		{
			if (_obj2Idx.TryGetValue(obj, out var Idx))
				_objects[Idx] = null;
			else
				_objectsToPush.Remove(obj);
		}
	}

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

	public void IntersectRay(Ray ray, WorkerCallback intersectCallback, ref float maxDist)
	{
		lock (_objects)
		{
			Balance();
			MDLCallback temp_cb = new(intersectCallback, _objects.ToArray(), _objects.Count);
			_tree.IntersectRay(ray, temp_cb, ref maxDist, true);
		}
	}

	public void IntersectPoint(Vector3 point, WorkerCallback intersectCallback)
	{
		lock (_objects)
		{
			Balance();
			MDLCallback callback = new(intersectCallback, _objects.ToArray(), _objects.Count);
			_tree.IntersectPoint(point, callback);
		}
	}

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

			var obj = objects[idx];

			if (obj != null)
				return _callback.Invoke(ray, obj, ref maxDist);

			return false;
		}

		/// Intersect point
		public override void Invoke(Vector3 p, int idx)
		{
			if (idx >= objects_size)
				return;

			var obj = objects[idx];

			if (obj != null)
				_callback.Invoke(p, obj as GameObjectModel);
		}
	}
}