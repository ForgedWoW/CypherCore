using Serilog;

namespace Forged.MapServer.Chrono;

public class WorldUpdateTime : UpdateTime
{
    private uint _lastRecordTime;
    private uint _recordUpdateTimeInverval;
    private uint _recordUpdateTimeMin;
    public void LoadFromConfig()
    {
        _recordUpdateTimeInverval = ConfigMgr.GetDefaultValue("RecordUpdateTimeDiffInterval", 60000u);
        _recordUpdateTimeMin = ConfigMgr.GetDefaultValue("MinRecordUpdateTimeDiff", 100u);
    }

    public void RecordUpdateTime(uint gameTimeMs, uint diff, uint sessionCount)
    {
        if (_recordUpdateTimeInverval > 0 && diff > _recordUpdateTimeMin)
            if (Time.GetMSTimeDiff(_lastRecordTime, gameTimeMs) > _recordUpdateTimeInverval)
            {
                Log.Logger.Debug($"Update time diff: {GetAverageUpdateTime()}. Players online: {sessionCount}.");
                _lastRecordTime = gameTimeMs;
            }
    }

    public void RecordUpdateTimeDuration(string text)
    {
        RecordUpdateTimeDuration(text, _recordUpdateTimeMin);
    }

    public void SetRecordUpdateTimeInterval(uint t)
    {
        _recordUpdateTimeInverval = t;
    }
}