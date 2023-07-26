// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Maps;
using Forged.MapServer.Scripting;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals;

public sealed class InstanceTemplateManager
{
    private readonly WorldDatabase _worldDatabase;
    private readonly MapManager _mapManager;
    private readonly ScriptManager _scriptManager;
    public Dictionary<uint, InstanceTemplate> InstanceTemplates { get; } = new();

    public InstanceTemplateManager(WorldDatabase worldDatabase, MapManager mapManager, ScriptManager scriptManager)
    {
        _worldDatabase = worldDatabase;
        _mapManager = mapManager;
        _scriptManager = scriptManager;
    }

    public InstanceTemplate GetInstanceTemplate(uint mapID)
    {
        return InstanceTemplates.LookupByKey(mapID);
    }

    public void LoadInstanceTemplate()
    {
        var time = Time.MSTime;

        //                                          0     1       2
        var result = _worldDatabase.Query("SELECT map, parent, script FROM instance_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 instance templates. DB table `instance_template` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var mapID = result.Read<uint>(0);

            if (!_mapManager.IsValidMap(mapID))
            {
                Log.Logger.Error("ObjectMgr.LoadInstanceTemplate: bad mapid {0} for template!", mapID);

                continue;
            }

            var instanceTemplate = new InstanceTemplate
            {
                Parent = result.Read<uint>(1),
                ScriptId = _scriptManager.GetScriptId(result.Read<string>(2))
            };

            InstanceTemplates.Add(mapID, instanceTemplate);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} instance templates in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }
}