// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Serilog;

namespace Framework.Threading;

public class LimitedThreadTaskManager
{
    private readonly AutoResetEvent _mapUpdateComplete = new(false);
    private ActionBlock<Action> _actionBlock;
    private readonly ExecutionDataflowBlockOptions _blockOptions;
    private readonly List<Action> _staged = new List<Action>();
    private CancellationTokenSource _cancellationToken;

	public LimitedThreadTaskManager(int maxDegreeOfParallelism) : this(new ExecutionDataflowBlockOptions()
	{
		MaxDegreeOfParallelism = maxDegreeOfParallelism
	}) { }

	public LimitedThreadTaskManager(ExecutionDataflowBlockOptions blockOptions = null)
	{
		if (blockOptions == null)
			blockOptions = new ExecutionDataflowBlockOptions()
			{
				MaxDegreeOfParallelism = 1,
				EnsureOrdered = true
			};

		_blockOptions = blockOptions;
        _cancellationToken = new CancellationTokenSource();
		_blockOptions.CancellationToken = _cancellationToken.Token;
        _actionBlock = new ActionBlock<Action>(ProcessTask, _blockOptions);
	}

	public void Deactivate()
	{
		_actionBlock.Complete();
	}

	/// <summary>
	///		Blocks thread and waits for all scheduled tasks to complete
	/// </summary>
	public void Wait()
    {
		try
		{
			ExecuteStaged();

			_actionBlock.Complete();
			int i = 0;

			while (!_actionBlock.Completion.IsCompleted && _actionBlock.InputCount != 0 & i != 3) // after 3 its too long we have to tick
			{
				_actionBlock.Completion.Wait(1000);
				i++;
			}

			if (i == 3 && !_actionBlock.Completion.IsCompleted)
			{
				_cancellationToken.Cancel(); // abort the task if we hit 3
				Log.Logger.Fatal("_actionBlock.Completion.Wait over 3 seconds." + Environment.NewLine + Environment.StackTrace);
			}
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "");
		}
		finally
		{
			_cancellationToken = new CancellationTokenSource();
			_blockOptions.CancellationToken = _cancellationToken.Token;
			_actionBlock = new ActionBlock<Action>(ProcessTask, _blockOptions);
		}
    }

	/// <summary>
	///		Queues an action to be worked on. Use <see cref="Wait"/> to wait for all actions to complete.
	///		Scheduled actions start as soon as there is a thread available.
	/// </summary>
	/// <param name="a"></param>
    public void Schedule(Action a)
	{
		_actionBlock.Post(a);
	}


	/// <summary>
	///		Staged actions will not execute until <see cref="Wait"/> or <see cref="ExecuteStaged"/> is called.
	/// </summary>
	/// <param name="a"></param>
	public void Stage(Action a)
	{
		lock (_staged)
			_staged.Add(a);
	}

	/// <summary>
	///		Schedules all work queued 
	/// </summary>
    public void ExecuteStaged()
    {
        lock (_staged)
        {
            foreach (var a in _staged)
                Schedule(a);

            _staged.Clear();
        }
    }

    private void ProcessTask(Action a)
	{
		try
		{
			a();
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "");
		}
	}
}