// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Framework.Threading
{
    public class LimitedThreadTaskManager
    {
        readonly AutoResetEvent _mapUpdateComplete = new AutoResetEvent(false);
        Exception _exc = null;
        ActionBlock<Action> _actionBlock;
        ExecutionDataflowBlockOptions _blockOptions;

        public LimitedThreadTaskManager(int maxDegreeOfParallelism) : this(new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism })
        {

        }

        public LimitedThreadTaskManager(ExecutionDataflowBlockOptions blockOptions = null)
        {
            if (blockOptions == null)
                blockOptions = new ExecutionDataflowBlockOptions()
                    {
                        MaxDegreeOfParallelism = 1
                    };

            _blockOptions = blockOptions;
            _actionBlock = new ActionBlock<Action>(ProcessTask, _blockOptions);
        }

        public void Deactivate()
        {
            _actionBlock.Complete();
            _actionBlock.Completion.Wait();
        }

        public void Wait()
        {
            _actionBlock.Complete();
            _actionBlock.Completion.Wait();
            CheckForExcpetion();
            _actionBlock = new ActionBlock<Action>(ProcessTask, _blockOptions);
        }

        private void CheckForExcpetion()
        {
            if (_exc != null)
                throw new Exception("Error while processing task!", _exc);
        }

        public void Schedule(Action a)
        {
            CheckForExcpetion();
            _actionBlock.Post(a);
        }


        public void ProcessTask(Action a)
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                Log.outException(ex);
                _exc = ex;
            }
        }

        public void Complete(bool success)
        {
            _mapUpdateComplete.Set();
        }
    }
}