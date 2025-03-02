﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Dynamic;

class TaskQueue
{
	readonly SortedSet<TaskSchedulerTask> container = new();

	/// <summary>
	///  Pushes the task in the container
	/// </summary>
	/// <param name="task"> </param>
	public void Push(TaskSchedulerTask task)
	{
		if (!container.Add(task)) { }
	}

	/// <summary>
	///  Pops the task out of the container
	/// </summary>
	/// <returns> </returns>
	public TaskSchedulerTask Pop()
	{
		var result = container.First();
		container.Remove(result);

		return result;
	}

	public TaskSchedulerTask First()
	{
		return container.First();
	}

	public void Clear()
	{
		container.Clear();
	}

	public void RemoveIf(Predicate<TaskSchedulerTask> filter)
	{
		container.RemoveWhere(filter);
	}

	public void ModifyIf(Func<TaskSchedulerTask, bool> filter)
	{
		List<TaskSchedulerTask> cache = new();

		foreach (var task in container.Where(filter))
			if (filter(task))
			{
				cache.Add(task);
				container.Remove(task);
			}

		foreach (var task in cache)
			container.Add(task);
	}

	public bool IsEmpty()
	{
		return container.Empty();
	}
}