// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Framework.Dynamic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Framework.Threading
{
    public class LimitedThreadTaskManager
    {
        readonly AutoResetEvent _mapUpdateComplete = new AutoResetEvent(false);
        readonly ConcurrentQueue<Action> _queue = new();
        uint _workCount = 0;
        readonly int _numThreads;
        Exception _exc = null;

        public LimitedThreadTaskManager(int numThreads)
        {
            _numThreads = numThreads;
        }

        public void Deactivate()
        {
            _queue.Clear();

            Wait();

            // ensure we are all clear and tasks exit.
            _mapUpdateComplete.Set();
        }

        public void Wait()
        {
            while (_workCount > 0)
                _mapUpdateComplete.WaitOne();

            CheckForExcpetion();
        }

        private void CheckForExcpetion()
        {
            if (_exc != null)
                throw new Exception("Error while processing task!", _exc);
        }

        public void Schedule(Action a)
        {
            CheckForExcpetion();

            if (_workCount > _numThreads)
                _queue.Enqueue(a);
            else
                StartNewTask(a);
        }

        private void StartNewTask(Action task)
        {
            Interlocked.Increment(ref _workCount);
            Task.Run(() =>
            {
                try
                {
                    task();
                }
                catch (Exception ex)
                {
                    Log.outException(ex);
                    _exc = ex;
                }
            }).ContinueWith(OnTaskEnded);
        }

        private void OnTaskEnded(Task task)
        {
            while (!TryDequeue() && _queue.Count != 0)
                Thread.Sleep(10);

            Interlocked.Decrement(ref _workCount);
            
            if (_workCount == 0)
                _mapUpdateComplete.Set();
        }

        private bool TryDequeue()
        {
            if (_exc != null)
                _queue.Clear();

            else if (_queue.TryDequeue(out var result) && result != null)
            {
                StartNewTask(result);
                return true;
            }

            return false;
        }
    }
}