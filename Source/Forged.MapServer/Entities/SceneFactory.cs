// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Framework.Constants;
using Game.Common;

namespace Forged.MapServer.Entities;

public class SceneFactory
{
    private readonly GameObjectManager _gameObjectManager;
    private readonly ClassFactory _classFactory;

    public SceneFactory(GameObjectManager gameObjectManager, ClassFactory classFactory)
    {
        _gameObjectManager = gameObjectManager;
        _classFactory = classFactory;
    }

    public SceneObject CreateSceneObject(uint sceneId, Unit creator, Position pos, ObjectGuid privateObjectOwner)
    {
        var sceneTemplate = _gameObjectManager.SceneTemplateCache.GetSceneTemplate(sceneId);

        if (sceneTemplate == null)
            return null;

        var lowGuid = creator.Location.Map.GenerateLowGuid(HighGuid.SceneObject);

        var sceneObject = _classFactory.Resolve<SceneObject>();

        if (sceneObject.Create(lowGuid, SceneType.Normal, sceneId, sceneTemplate.ScenePackageId, creator.Location.Map, creator, pos, privateObjectOwner))
            return sceneObject;

        sceneObject.Dispose();

        return null;
    }
}