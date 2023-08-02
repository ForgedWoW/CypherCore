// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class SceneTemplateCache : IObjectCache
{
    private readonly Dictionary<uint, SceneTemplate> _sceneTemplateStorage = new();
    private readonly ScriptManager _scriptManager;
    private readonly WorldDatabase _worldDatabase;

    public SceneTemplateCache(WorldDatabase worldDatabase, ScriptManager scriptManager)
    {
        _worldDatabase = worldDatabase;
        _scriptManager = scriptManager;
    }

    public SceneTemplate GetSceneTemplate(uint sceneId)
    {
        return _sceneTemplateStorage.LookupByKey(sceneId);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;
        _sceneTemplateStorage.Clear();

        var result = _worldDatabase.Query("SELECT SceneId, Flags, ScriptPackageID, Encrypted, ScriptName FROM scene_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 scene templates. DB table `scene_template` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var sceneId = result.Read<uint>(0);

            SceneTemplate sceneTemplate = new()
            {
                SceneId = sceneId,
                PlaybackFlags = (SceneFlags)result.Read<uint>(1),
                ScenePackageId = result.Read<uint>(2),
                Encrypted = result.Read<byte>(3) != 0,
                ScriptId = _scriptManager.GetScriptId(result.Read<string>(4))
            };

            count++;
            _sceneTemplateStorage[sceneId] = sceneTemplate;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} scene templates in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}