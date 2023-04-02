// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Dynamic;

public class BasicEvent
{
    public ulong m_addTime;   // time when the event was added to queue, filled by event handler
    public double m_execTime; // planned time of next execution, filled by event handler

    private AbortState m_abortState; // set by externals when the event is aborted, aborted events don't execute

    public bool IsAborted => m_abortState == AbortState.Aborted;
    public bool IsAbortScheduled => m_abortState == AbortState.Scheduled;
    public virtual bool IsDeletable => true;

    public bool IsRunning => m_abortState == AbortState.Running;

    public BasicEvent()
    {
        m_abortState = AbortState.Running;
    }

    public virtual void Abort(ulong e_time) { }

    // this method executes when the event is triggered
    // return false if event does not want to be deleted
    // e_time is execution time, p_time is update interval
    public virtual bool Execute(ulong etime, uint pTime)
    {
        return true;
    }

    public void ScheduleAbort()
    {
        m_abortState = AbortState.Scheduled;
    }

    public void SetAborted()
    {
        m_abortState = AbortState.Aborted;
    }

    // this method executes when the event is aborted
}