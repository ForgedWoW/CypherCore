// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Chrono;

public class WorldUpdateTime : UpdateTime
{
    private readonly IConfiguration _configuration;
    private uint _lastRecordTime;
    private uint _recordUpdateTimeInverval;
    private uint _recordUpdateTimeMin;

    public WorldUpdateTime(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void LoadFromConfig()
    {
        _recordUpdateTimeInverval = _configuration.GetDefaultValue("RecordUpdateTimeDiffInterval", 60000u);
        _recordUpdateTimeMin = _configuration.GetDefaultValue("MinRecordUpdateTimeDiff", 100u);
    }

    public void RecordUpdateTime(uint gameTimeMs, uint diff, uint sessionCount)
    {
        if (_recordUpdateTimeInverval <= 0 || diff <= _recordUpdateTimeMin)
            return;

        if (Time.GetMSTimeDiff(_lastRecordTime, gameTimeMs) <= _recordUpdateTimeInverval)
            return;

        Log.Logger.Debug($"Update time diff: {GetAverageUpdateTime()}. Players online: {sessionCount}.");
        _lastRecordTime = gameTimeMs;
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