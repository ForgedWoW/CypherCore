// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Dynamic;

public class TaskScheduler
{
    // Predicate type
    public delegate bool predicate_t();

    // Success handle type
    public delegate void success_t();

    // Contains all asynchronous tasks which will be invoked at
    // the next update tick.
    private readonly List<Action> _asyncHolder;

    // The Task Queue which contains all task objects.
    private readonly TaskQueue _task_holder;

    // The current time point (now)
    private DateTime _now;

    private predicate_t _predicate;

    public TaskScheduler()
    {
        _now = DateTime.Now;
        _task_holder = new TaskQueue();
        _asyncHolder = new List<Action>();
        _predicate = EmptyValidator;
    }

    public TaskScheduler(predicate_t predicate)
    {
        _now = DateTime.Now;
        _task_holder = new TaskQueue();
        _asyncHolder = new List<Action>();
        _predicate = predicate;
    }

    public TaskScheduler Async(Action callable)
    {
        _asyncHolder.Add(callable);

        return this;
    }

    public TaskScheduler CancelAll()
    {
        // Clear the task holder
        _task_holder.Clear();
        _asyncHolder.Clear();

        return this;
    }

    public TaskScheduler CancelGroup(uint group)
    {
        _task_holder.RemoveIf(task => task.IsInGroup(@group));

        return this;
    }

    public TaskScheduler CancelGroupsOf(List<uint> groups)
    {
        groups.ForEach(group => CancelGroup(group));

        return this;
    }

    /// <summary>
    ///     Delays all tasks with the given duration.
    /// </summary>
    /// <param name="duration"> </param>
    /// <returns> </returns>
    public TaskScheduler DelayAll(TimeSpan duration)
    {
        _task_holder.ModifyIf(task =>
        {
            task._end += duration;

            return true;
        });

        return this;
    }

    /// <summary>
    ///     Delays all tasks with a random duration between min and max.
    /// </summary>
    /// <param name="min"> </param>
    /// <param name="max"> </param>
    /// <returns> </returns>
    public TaskScheduler DelayAll(TimeSpan min, TimeSpan max)
    {
        return DelayAll(RandomHelper.RandTime(min, max));
    }

    /// <summary>
    ///     Delays all tasks of a group with the given duration.
    /// </summary>
    /// <param name="group"> </param>
    /// <param name="duration"> </param>
    /// <returns> </returns>
    public TaskScheduler DelayGroup(uint group, TimeSpan duration)
    {
        _task_holder.ModifyIf(task =>
        {
            if (task.IsInGroup(group))
            {
                task._end += duration;

                return true;
            }
            else
            {
                return false;
            }
        });

        return this;
    }

    /// <summary>
    ///     Delays all tasks of a group with a random duration between min and max.
    /// </summary>
    /// <param name="group"> </param>
    /// <param name="min"> </param>
    /// <param name="max"> </param>
    /// <returns> </returns>
    public TaskScheduler DelayGroup(uint group, TimeSpan min, TimeSpan max)
    {
        return DelayGroup(group, RandomHelper.RandTime(min, max));
    }

    /// <summary>
    ///     Reschedule all tasks with a given duration.
    /// </summary>
    /// <param name="duration"> </param>
    /// <returns> </returns>
    public TaskScheduler RescheduleAll(TimeSpan duration)
    {
        var end = _now + duration;

        _task_holder.ModifyIf(task =>
        {
            task._end = end;

            return true;
        });

        return this;
    }

    /// <summary>
    ///     Reschedule all tasks with a random duration between min and max.
    /// </summary>
    /// <param name="min"> </param>
    /// <param name="max"> </param>
    /// <returns> </returns>
    public TaskScheduler RescheduleAll(TimeSpan min, TimeSpan max)
    {
        return RescheduleAll(RandomHelper.RandTime(min, max));
    }

    /// <summary>
    ///     Reschedule all tasks of a group with the given duration.
    /// </summary>
    /// <param name="group"> </param>
    /// <param name="duration"> </param>
    /// <returns> </returns>
    public TaskScheduler RescheduleGroup(uint group, TimeSpan duration)
    {
        var end = _now + duration;

        _task_holder.ModifyIf(task =>
        {
            if (task.IsInGroup(group))
            {
                task._end = end;

                return true;
            }
            else
            {
                return false;
            }
        });

        return this;
    }

    /// <summary>
    ///     Reschedule all tasks of a group with a random duration between min and max.
    /// </summary>
    /// <param name="group"> </param>
    /// <param name="min"> </param>
    /// <param name="max"> </param>
    /// <returns> </returns>
    public TaskScheduler RescheduleGroup(uint group, TimeSpan min, TimeSpan max)
    {
        return RescheduleGroup(group, RandomHelper.RandTime(min, max));
    }

    /// <summary>
    ///     Schedule an event with a fixed rate.
    ///     Never call this from within a task context! Use TaskContext.Schedule instead!
    /// </summary>
    /// <param name="time"> </param>
    /// <param name="task"> </param>
    /// <returns> </returns>
    public TaskScheduler Schedule(TimeSpan time, Action<TaskContext> task)
    {
        return ScheduleAt(_now, time, task);
    }

    /// <summary>
    ///     Schedule an event with a fixed rate.
    ///     Never call this from within a task context! Use TaskContext.Schedule instead!
    /// </summary>
    /// <param name="time"> </param>
    /// <param name="group"> </param>
    /// <param name="task"> </param>
    /// <returns> </returns>
    public TaskScheduler Schedule(TimeSpan time, uint group, Action<TaskContext> task)
    {
        return ScheduleAt(_now, time, group, task);
    }

    /// <summary>
    ///     Schedule an event with a randomized rate between min and max rate.
    ///     Never call this from within a task context! Use TaskContext.Schedule instead!
    /// </summary>
    /// <param name="min"> </param>
    /// <param name="max"> </param>
    /// <param name="task"> </param>
    /// <returns> </returns>
    public TaskScheduler Schedule(TimeSpan min, TimeSpan max, Action<TaskContext> task)
    {
        return Schedule(RandomHelper.RandTime(min, max), task);
    }

    /// <summary>
    ///     Schedule an event with a fixed rate.
    ///     Never call this from within a task context! Use TaskContext.Schedule instead!
    /// </summary>
    /// <param name="min"> </param>
    /// <param name="max"> </param>
    /// <param name="group"> </param>
    /// <param name="task"> </param>
    /// <returns> </returns>
    public TaskScheduler Schedule(TimeSpan min, TimeSpan max, uint group, Action<TaskContext> task)
    {
        return Schedule(RandomHelper.RandTime(min, max), group, task);
    }

    /// <summary>
    ///     Sets a validator which is asked if tasks are allowed to be executed.
    /// </summary>
    /// <param name="predicate"> </param>
    /// <returns> </returns>
    public TaskScheduler SetValidator(predicate_t predicate)
    {
        _predicate = predicate;

        return this;
    }

    /// <summary>
    ///     Update the scheduler to the current time.
    ///     Calls the optional callback on successfully finish.
    /// </summary>
    /// <returns> </returns>
    public TaskScheduler Update(success_t callback = null)
    {
        _now = DateTime.Now;
        Dispatch(callback);

        return this;
    }

    /// <summary>
    ///     Update the scheduler with a difftime in ms.
    ///     Calls the optional callback on successfully finish.
    /// </summary>
    /// <param name="milliseconds"> </param>
    /// <param name="callback"> </param>
    /// <returns> </returns>
    public TaskScheduler Update(uint milliseconds, success_t callback = null)
    {
        return Update(TimeSpan.FromMilliseconds(milliseconds), callback);
    }

    internal TaskScheduler InsertTask(TaskSchedulerTask task)
    {
        _task_holder.Push(task);

        return this;
    }

    internal TaskScheduler ScheduleAt(DateTime end, TimeSpan time, Action<TaskContext> task)
    {
        return InsertTask(new TaskSchedulerTask(end + time, time, task));
    }

    /// <summary>
    ///     Schedule an event with a fixed rate.
    ///     Never call this from within a task context! Use TaskContext.schedule instead!
    /// </summary>
    /// <param name="end"> </param>
    /// <param name="time"> </param>
    /// <param name="group"> </param>
    /// <param name="task"> </param>
    /// <returns> </returns>
    internal TaskScheduler ScheduleAt(DateTime end, TimeSpan time, uint group, Action<TaskContext> task)
    {
        return InsertTask(new TaskSchedulerTask(end + time, time, group, 0, task));
    }

    private static bool EmptyValidator()
    {
        return true;
    }

    /// <summary>
    ///     Clears the validator which is asked if tasks are allowed to be executed.
    /// </summary>
    /// <returns> </returns>
    private TaskScheduler ClearValidator()
    {
        _predicate = EmptyValidator;

        return this;
    }

    private void Dispatch(success_t callback = null)
    {
        // If the validation failed abort the dispatching here.
        if (!_predicate())
            return;

        // Process all asyncs
        while (!_asyncHolder.Empty())
        {
            _asyncHolder.First().Invoke();
            _asyncHolder.RemoveAt(0);

            // If the validation failed abort the dispatching here.
            if (!_predicate())
                return;
        }

        while (!_task_holder.IsEmpty())
        {
            if (_task_holder.First()._end > _now)
                break;

            // Perfect forward the context to the handler
            // Use weak references to catch destruction before callbacks.
            TaskContext context = new(_task_holder.Pop(), this);

            // Invoke the context
            context.Invoke();

            // If the validation failed abort the dispatching here.
            if (!_predicate())
                return;
        }

        callback?.Invoke();
    }

    /// <summary>
    ///     Update the scheduler with a difftime.
    ///     Calls the optional callback on successfully finish.
    /// </summary>
    /// <param name="difftime"> </param>
    /// <param name="callback"> </param>
    /// <returns> </returns>
    private TaskScheduler Update(TimeSpan difftime, success_t callback = null)
    {
        _now += difftime;
        Dispatch(callback);

        return this;
    }
}