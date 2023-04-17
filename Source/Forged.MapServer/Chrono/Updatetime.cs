// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Serilog;

namespace Forged.MapServer.Chrono;

public class UpdateTime
{
    private readonly uint[] _updateTimeDataTable = new uint[500];
    private uint _averageUpdateTime;
    private uint _maxUpdateTime;
    private uint _maxUpdateTimeOfCurrentTable;
    private uint _maxUpdateTimeOfLastTable;
    private uint _recordedTime;
    private uint _totalUpdateTime;
    private uint _updateTimeTableIndex;

    public uint GetAverageUpdateTime()
    {
        return _averageUpdateTime;
    }

    public uint GetLastUpdateTime()
    {
        return _updateTimeDataTable[_updateTimeTableIndex != 0 ? _updateTimeTableIndex - 1 : _updateTimeDataTable.Length - 1u];
    }

    public uint GetMaxUpdateTime()
    {
        return _maxUpdateTime;
    }

    public uint GetMaxUpdateTimeOfCurrentTable()
    {
        return Math.Max(_maxUpdateTimeOfCurrentTable, _maxUpdateTimeOfLastTable);
    }

    public uint GetTimeWeightedAverageUpdateTime()
    {
        uint sum = 0, weightsum = 0;

        foreach (var diff in _updateTimeDataTable)
        {
            sum += diff * diff;
            weightsum += diff;
        }

        if (weightsum == 0)
            return 0;

        return sum / weightsum;
    }

    public void RecordUpdateTimeDuration(string text, uint minUpdateTime)
    {
        var thisTime = Time.MSTime;
        var diff = Time.GetMSTimeDiff(_recordedTime, thisTime);

        if (diff > minUpdateTime)
            Log.Logger.Information($"Recored Update Time of {text}: {diff}.");

        _recordedTime = thisTime;
    }

    public void RecordUpdateTimeReset()
    {
        _recordedTime = Time.MSTime;
    }

    public void UpdateWithDiff(uint diff)
    {
        _totalUpdateTime = _totalUpdateTime - _updateTimeDataTable[_updateTimeTableIndex] + diff;
        _updateTimeDataTable[_updateTimeTableIndex] = diff;

        if (diff > _maxUpdateTime)
            _maxUpdateTime = diff;

        if (diff > _maxUpdateTimeOfCurrentTable)
            _maxUpdateTimeOfCurrentTable = diff;

        if (++_updateTimeTableIndex >= _updateTimeDataTable.Length)
        {
            _updateTimeTableIndex = 0;
            _maxUpdateTimeOfLastTable = _maxUpdateTimeOfCurrentTable;
            _maxUpdateTimeOfCurrentTable = 0;
        }

        if (_updateTimeDataTable[^1] != 0)
            _averageUpdateTime = (uint)(_totalUpdateTime / _updateTimeDataTable.Length);
        else if (_updateTimeTableIndex != 0)
            _averageUpdateTime = _totalUpdateTime / _updateTimeTableIndex;
    }
}