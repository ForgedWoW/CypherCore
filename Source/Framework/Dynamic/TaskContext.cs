// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;

namespace Framework.Dynamic;

public class TaskContext
{
	// Associated task
	readonly TaskSchedulerTask _task;

	// Owner
	readonly TaskScheduler _owner;

	// Marks the task as consumed
	bool _consumed = true;

	public TaskContext(TaskSchedulerTask task, TaskScheduler owner)
	{
		_task = task;
		_owner = owner;
		_consumed = false;
	}

	/// <summary>
	///  Returns the repeat counter which increases every time the task is repeated.
	/// </summary>
	/// <returns> </returns>
	public uint GetRepeatCounter()
	{
		return _task._repeated;
	}

	/// <summary>
	///  Cancels all tasks from within the context.
	/// </summary>
	/// <returns> </returns>
	public TaskContext CancelAll()
	{
		return Dispatch(() => _owner.CancelAll());
	}

	/// <summary>
	///  Cancel all tasks of a single group from within the context.
	/// </summary>
	/// <param name="group"> </param>
	/// <returns> </returns>
	public TaskContext CancelGroup(uint group)
	{
		return Dispatch(() => _owner.CancelGroup(group));
	}

	/// <summary>
	///  Cancels all groups in the given std.vector from within the context.
	/// </summary>
	/// <param name="groups"> </param>
	/// <returns> </returns>
	public TaskContext CancelGroupsOf(List<uint> groups)
	{
		return Dispatch(() => _owner.CancelGroupsOf(groups));
	}

	/// <summary>
	///  Invokes the associated hook of the task.
	/// </summary>
	public void Invoke()
	{
		_task._task(this);
	}

	/// <summary>
	///  Repeats the event and sets a new duration.
	///  This will consume the task context, its not possible to repeat the task again
	///  from the same task context!
	/// </summary>
	/// <param name="duration"> </param>
	/// <returns> </returns>
	public TaskContext Repeat(TimeSpan duration)
	{
		AssertOnConsumed();

		// Set new duration, in-context timing and increment repeat counter
		_task._duration = duration;
		_task._end += duration;
		_task._repeated += 1;
		_consumed = true;

		return Dispatch(() => _owner.InsertTask(_task));
	}

	/// <summary>
	///  Repeats the event with the same duration.
	///  This will consume the task context, its not possible to repeat the task again
	///  from the same task context!
	/// </summary>
	/// <returns> </returns>
	public TaskContext Repeat()
	{
		return Repeat(_task._duration);
	}

	/// <summary>
	///  Repeats the event and set a new duration that is randomized between min and max.
	///  This will consume the task context, its not possible to repeat the task again
	///  from the same task context!
	/// </summary>
	/// <param name="min"> </param>
	/// <param name="max"> </param>
	/// <returns> </returns>
	public TaskContext Repeat(TimeSpan min, TimeSpan max)
	{
		return Repeat(RandomHelper.RandTime(min, max));
	}

	/// <summary>
	///  Schedule an event with a fixed rate from within the context.
	///  Its possible that the new event is executed immediately!
	///  Use TaskScheduler.Async to create a task
	///  which will be called at the next update tick.
	/// </summary>
	/// <param name="time"> </param>
	/// <param name="task"> </param>
	/// <returns> </returns>
	public TaskContext Schedule(TimeSpan time, Action<TaskContext> task)
	{
		var end = _task._end;

		return Dispatch(scheduler => scheduler.ScheduleAt(end, time, task));
	}

	public TaskContext Schedule(TimeSpan time, Action task)
	{
		return Schedule(time, delegate(TaskContext task1) { task(); });
	}

	/// <summary>
	///  Schedule an event with a fixed rate from within the context.
	///  Its possible that the new event is executed immediately!
	///  Use TaskScheduler.Async to create a task
	///  which will be called at the next update tick.
	/// </summary>
	/// <param name="time"> </param>
	/// <param name="group"> </param>
	/// <param name="task"> </param>
	/// <returns> </returns>
	public TaskContext Schedule(TimeSpan time, uint group, Action<TaskContext> task)
	{
		var end = _task._end;

		return Dispatch(scheduler => scheduler.ScheduleAt(end, time, @group, task));
	}

	public TaskContext Schedule(TimeSpan time, uint group, Action task)
	{
		return Schedule(time, group, delegate(TaskContext task1) { task(); });
	}

	/// <summary>
	///  Schedule an event with a randomized rate between min and max rate from within the context.
	///  Its possible that the new event is executed immediately!
	///  Use TaskScheduler.Async to create a task
	///  which will be called at the next update tick.
	/// </summary>
	/// <param name="min"> </param>
	/// <param name="max"> </param>
	/// <param name="task"> </param>
	/// <returns> </returns>
	public TaskContext Schedule(TimeSpan min, TimeSpan max, Action<TaskContext> task)
	{
		return Schedule(RandomHelper.RandTime(min, max), task);
	}

	public TaskContext Schedule(TimeSpan min, TimeSpan max, Action task)
	{
		return Schedule(min, max, delegate(TaskContext task1) { task(); });
	}

	/// <summary>
	///  Schedule an event with a randomized rate between min and max rate from within the context.
	///  Its possible that the new event is executed immediately!
	///  Use TaskScheduler.Async to create a task
	///  which will be called at the next update tick.
	/// </summary>
	/// <param name="min"> </param>
	/// <param name="max"> </param>
	/// <param name="group"> </param>
	/// <param name="task"> </param>
	/// <returns> </returns>
	public TaskContext Schedule(TimeSpan min, TimeSpan max, uint group, Action<TaskContext> task)
	{
		return Schedule(RandomHelper.RandTime(min, max), group, task);
	}

	public TaskContext Schedule(TimeSpan min, TimeSpan max, uint group, Action task)
	{
		return Schedule(min, max, group, delegate(TaskContext task1) { task(); });
	}

	/// <summary>
	///  Delays all tasks with the given duration from within the context.
	/// </summary>
	/// <param name="duration"> </param>
	/// <returns> </returns>
	public TaskContext DelayAll(TimeSpan duration)
	{
		return Dispatch(() => _owner.DelayAll(duration));
	}

	/// <summary>
	///  Delays all tasks with a random duration between min and max from within the context.
	/// </summary>
	/// <param name="min"> </param>
	/// <param name="max"> </param>
	/// <returns> </returns>
	public TaskContext DelayAll(TimeSpan min, TimeSpan max)
	{
		return DelayAll(RandomHelper.RandTime(min, max));
	}

	/// <summary>
	///  Delays all tasks of a group with the given duration from within the context.
	/// </summary>
	/// <param name="group"> </param>
	/// <param name="duration"> </param>
	/// <returns> </returns>
	public TaskContext DelayGroup(uint group, TimeSpan duration)
	{
		return Dispatch(() => _owner.DelayGroup(group, duration));
	}

	/// <summary>
	///  Delays all tasks of a group with a random duration between min and max from within the context.
	/// </summary>
	/// <param name="group"> </param>
	/// <param name="min"> </param>
	/// <param name="max"> </param>
	/// <returns> </returns>
	public TaskContext DelayGroup(uint group, TimeSpan min, TimeSpan max)
	{
		return DelayGroup(group, RandomHelper.RandTime(min, max));
	}

	/// <summary>
	///  Reschedule all tasks with the given duration.
	/// </summary>
	/// <param name="duration"> </param>
	/// <returns> </returns>
	public TaskContext RescheduleAll(TimeSpan duration)
	{
		return Dispatch(() => _owner.RescheduleAll(duration));
	}

	/// <summary>
	///  Reschedule all tasks with a random duration between min and max.
	/// </summary>
	/// <param name="min"> </param>
	/// <param name="max"> </param>
	/// <returns> </returns>
	public TaskContext RescheduleAll(TimeSpan min, TimeSpan max)
	{
		return RescheduleAll(RandomHelper.RandTime(min, max));
	}

	/// <summary>
	///  Reschedule all tasks of a group with the given duration.
	/// </summary>
	/// <param name="group"> </param>
	/// <param name="duration"> </param>
	/// <returns> </returns>
	public TaskContext RescheduleGroup(uint group, TimeSpan duration)
	{
		return Dispatch(() => _owner.RescheduleGroup(group, duration));
	}

	/// <summary>
	///  Reschedule all tasks of a group with a random duration between min and max.
	/// </summary>
	/// <param name="group"> </param>
	/// <param name="min"> </param>
	/// <param name="max"> </param>
	/// <returns> </returns>
	public TaskContext RescheduleGroup(uint group, TimeSpan min, TimeSpan max)
	{
		return RescheduleGroup(group, RandomHelper.RandTime(min, max));
	}

	/// <summary>
	///  Dispatches an action safe on the TaskScheduler
	/// </summary>
	/// <param name="apply"> </param>
	/// <returns> </returns>
	TaskContext Dispatch(Action apply)
	{
		apply();

		return this;
	}

	TaskContext Dispatch(Func<TaskScheduler, TaskScheduler> apply)
	{
		apply(_owner);

		return this;
	}

	bool IsExpired()
	{
		return _owner == null;
	}

	/// <summary>
	///  Returns true if the event is in the given group
	/// </summary>
	/// <param name="group"> </param>
	/// <returns> </returns>
	bool IsInGroup(uint group)
	{
		return _task.IsInGroup(group);
	}

	/// <summary>
	///  Sets the event in the given group
	/// </summary>
	/// <param name="group"> </param>
	/// <returns> </returns>
	TaskContext SetGroup(uint group)
	{
		_task._group = group;

		return this;
	}

	/// <summary>
	///  Removes the group from the event
	/// </summary>
	/// <returns> </returns>
	TaskContext ClearGroup()
	{
		_task._group = null;

		return this;
	}

	/// <summary>
	///  Schedule a callable function that is executed at the next update tick from within the context.
	///  Its safe to modify the TaskScheduler from within the callable.
	/// </summary>
	/// <param name="callable"> </param>
	/// <returns> </returns>
	TaskContext Async(Action callable)
	{
		return Dispatch(() => _owner.Async(callable));
	}

	/// <summary>
	///  Asserts if the task was consumed already.
	/// </summary>
	void AssertOnConsumed()
	{
		// This was adapted to TC to prevent static analysis tools from complaining.
		// If you encounter this assertion check if you repeat a TaskContext more then 1 time!
		
	}
}