﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Scripting.Interfaces.IScene;

public interface ISceneOnSceneComplete : IScriptObject
{
	void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate);
}