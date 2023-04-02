// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.World;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Entities.Objects;

public class ObjectGuidGenerator
{
    private readonly IConfiguration _configuration;
    private readonly HighGuid _highGuid;
    private readonly WorldManager _worldManager;
    private ulong _nextGuid;

    public ObjectGuidGenerator(HighGuid highGuid, ulong start, WorldManager worldManager, IConfiguration configuration)
    {
        _highGuid = highGuid;
        _nextGuid = start;
        _worldManager = worldManager;
        _configuration = configuration;
    }

    public ulong Generate()
    {
        if (_highGuid is HighGuid.Creature or HighGuid.Vehicle or HighGuid.GameObject or HighGuid.Transport)
            CheckGuidTrigger(_nextGuid);

        return _nextGuid++;
    }

    public ulong GetNextAfterMaxUsed()
    {
        return _nextGuid;
    }

    public void Set(ulong val)
    {
        _nextGuid = val;
    }
    private void CheckGuidTrigger(ulong guidlow)
    {
        if (!_worldManager.IsGuidAlert && guidlow > _configuration.GetDefaultValue("Respawn.GuidAlertLevel", 16000000ul))
            _worldManager.TriggerGuidAlert();
        else if (!_worldManager.IsGuidWarning && guidlow > _configuration.GetDefaultValue("Respawn.GuidWarnLevel", 12000000ul))
            _worldManager.TriggerGuidWarning();
    }
}