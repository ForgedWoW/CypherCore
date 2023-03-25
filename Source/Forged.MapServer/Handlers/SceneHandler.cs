// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Handlers;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.SceneTriggerEvent)]
	void HandleSceneTriggerEvent(SceneTriggerEvent sceneTriggerEvent)
	{
		Log.Logger.Debug("HandleSceneTriggerEvent: SceneInstanceID: {0} Event: {1}", sceneTriggerEvent.SceneInstanceID, sceneTriggerEvent._Event);

		Player.SceneMgr.OnSceneTrigger(sceneTriggerEvent.SceneInstanceID, sceneTriggerEvent._Event);
	}

	[WorldPacketHandler(ClientOpcodes.ScenePlaybackComplete)]
	void HandleScenePlaybackComplete(ScenePlaybackComplete scenePlaybackComplete)
	{
		Log.Logger.Debug("HandleScenePlaybackComplete: SceneInstanceID: {0}", scenePlaybackComplete.SceneInstanceID);

		Player.SceneMgr.OnSceneComplete(scenePlaybackComplete.SceneInstanceID);
	}

	[WorldPacketHandler(ClientOpcodes.ScenePlaybackCanceled)]
	void HandleScenePlaybackCanceled(ScenePlaybackCanceled scenePlaybackCanceled)
	{
		Log.Logger.Debug("HandleScenePlaybackCanceled: SceneInstanceID: {0}", scenePlaybackCanceled.SceneInstanceID);

		Player.SceneMgr.OnSceneCancel(scenePlaybackCanceled.SceneInstanceID);
	}
}