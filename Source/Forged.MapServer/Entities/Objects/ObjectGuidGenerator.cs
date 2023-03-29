// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities.Objects;

public class ObjectGuidGenerator
{
    private readonly HighGuid _highGuid;
    private ulong _nextGuid;

    public ObjectGuidGenerator(HighGuid highGuid, ulong start = 1)
    {
        _highGuid = highGuid;
        _nextGuid = start;
    }

    public void Set(ulong val)
    {
        _nextGuid = val;
    }

    public ulong Generate()
    {
        if (_nextGuid >= ObjectGuid.GetMaxCounter(_highGuid) - 1)
            HandleCounterOverflow();

        if (_highGuid == HighGuid.Creature || _highGuid == HighGuid.Vehicle || _highGuid == HighGuid.GameObject || _highGuid == HighGuid.Transport)
            CheckGuidTrigger(_nextGuid);

        return _nextGuid++;
    }

    public ulong GetNextAfterMaxUsed()
    {
        return _nextGuid;
    }

    private void HandleCounterOverflow()
    {
        Log.Logger.Fatal("{0} guid overflow!! Can't continue, shutting down server. ", _highGuid);
        Global.WorldMgr.StopNow();
    }

    private void CheckGuidTrigger(ulong guidlow)
    {
        if (!Global.WorldMgr.IsGuidAlert && guidlow > GetDefaultValue("Respawn.GuidAlertLevel", 16000000))
            Global.WorldMgr.TriggerGuidAlert();
        else if (!Global.WorldMgr.IsGuidWarning && guidlow > GetDefaultValue("Respawn.GuidWarnLevel", 12000000))
            Global.WorldMgr.TriggerGuidWarning();
    }
}