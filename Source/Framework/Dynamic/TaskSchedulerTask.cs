// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Dynamic;

public class TaskSchedulerTask : IComparable<TaskSchedulerTask>
{
    internal DateTime _end;
    internal TimeSpan _duration;
    internal uint? _group;
    internal uint _repeated;
    internal Action<TaskContext> _task;

    public TaskSchedulerTask(DateTime end, TimeSpan duration, uint group, uint repeated, Action<TaskContext> task)
    {
        _end = end;
        _duration = duration;
        _group = group;
        _repeated = repeated;
        _task = task;
    }

    public TaskSchedulerTask(DateTime end, TimeSpan duration, Action<TaskContext> task)
    {
        _end = end;
        _duration = duration;
        _task = task;
    }

    public int CompareTo(TaskSchedulerTask other)
    {
        return _end.CompareTo(other._end);
    }

	/// <summary>
	///     Returns true if the task is in the given group
	/// </summary>
	/// <param name="group"> </param>
	/// <returns> </returns>
	public bool IsInGroup(uint group)
    {
        return _group.HasValue && _group == group;
    }
}