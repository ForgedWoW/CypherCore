// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Entities.Objects;
using Game.Common.Globals;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Scene;

namespace Game.Common.Entities.Players;

public class SceneMgr
{
	readonly Player _player;
	readonly Dictionary<uint, SceneTemplate> _scenesByInstance = new();
	readonly List<ServerPacket> _delayedScenes = new();
	uint _standaloneSceneInstanceId;
	bool _isDebuggingScenes;

	Player Player => _player;

	public SceneMgr(Player player)
	{
		_player = player;
		_standaloneSceneInstanceId = 0;
		_isDebuggingScenes = false;
	}

	public uint PlayScene(uint sceneId, Position position = null)
	{
		var sceneTemplate = Global.ObjectMgr.GetSceneTemplate(sceneId);

		return PlaySceneByTemplate(sceneTemplate, position);
	}

	public uint PlaySceneByTemplate(SceneTemplate sceneTemplate, Position position = null)
	{
		if (sceneTemplate == null)
			return 0;

		var entry = CliDB.SceneScriptPackageStorage.LookupByKey(sceneTemplate.ScenePackageId);

		if (entry == null)
			return 0;

		// By default, take player position
		if (position == null)
			position = Player.Location;

		var sceneInstanceId = GetNewStandaloneSceneInstanceId();

		if (_isDebuggingScenes)
			Player.SendSysMessage(CypherStrings.CommandSceneDebugPlay, sceneInstanceId, sceneTemplate.ScenePackageId, sceneTemplate.PlaybackFlags);

		PlayScene playScene = new();
		playScene.SceneID = sceneTemplate.SceneId;
		playScene.PlaybackFlags = (uint)sceneTemplate.PlaybackFlags;
		playScene.SceneInstanceID = sceneInstanceId;
		playScene.SceneScriptPackageID = sceneTemplate.ScenePackageId;
		playScene.Location = position;
		playScene.TransportGUID = Player.GetTransGUID();
		playScene.Encrypted = sceneTemplate.Encrypted;
		playScene.Write();

		if (Player.IsInWorld)
			Player.SendPacket(playScene);
		else
			_delayedScenes.Add(playScene);

		AddInstanceIdToSceneMap(sceneInstanceId, sceneTemplate);

		Global.ScriptMgr.RunScript<ISceneOnSceneStart>(script => script.OnSceneStart(Player, sceneInstanceId, sceneTemplate), sceneTemplate.ScriptId);

		return sceneInstanceId;
	}

	public uint PlaySceneByPackageId(uint sceneScriptPackageId, SceneFlags playbackflags, Position position = null)
	{
		SceneTemplate sceneTemplate = new();
		sceneTemplate.SceneId = 0;
		sceneTemplate.ScenePackageId = sceneScriptPackageId;
		sceneTemplate.PlaybackFlags = playbackflags;
		sceneTemplate.Encrypted = false;
		sceneTemplate.ScriptId = 0;

		return PlaySceneByTemplate(sceneTemplate, position);
	}

	public void OnSceneTrigger(uint sceneInstanceId, string triggerName)
	{
		if (!HasScene(sceneInstanceId))
			return;

		if (_isDebuggingScenes)
			Player.SendSysMessage(CypherStrings.CommandSceneDebugTrigger, sceneInstanceId, triggerName);

		var sceneTemplate = GetSceneTemplateFromInstanceId(sceneInstanceId);
		Global.ScriptMgr.RunScript<ISceneOnSceneTrigger>(script => script.OnSceneTriggerEvent(Player, sceneInstanceId, sceneTemplate, triggerName), sceneTemplate.ScriptId);
	}

	public void OnSceneCancel(uint sceneInstanceId)
	{
		if (!HasScene(sceneInstanceId))
			return;

		if (_isDebuggingScenes)
			Player.SendSysMessage(CypherStrings.CommandSceneDebugCancel, sceneInstanceId);

		var sceneTemplate = GetSceneTemplateFromInstanceId(sceneInstanceId);

		if (sceneTemplate.PlaybackFlags.HasFlag(SceneFlags.NotCancelable))
			return;

		// Must be done before removing aura
		RemoveSceneInstanceId(sceneInstanceId);

		if (sceneTemplate.SceneId != 0)
			RemoveAurasDueToSceneId(sceneTemplate.SceneId);

		Global.ScriptMgr.RunScript<ISceneOnSceneChancel>(script => script.OnSceneCancel(Player, sceneInstanceId, sceneTemplate), sceneTemplate.ScriptId);

		if (sceneTemplate.PlaybackFlags.HasFlag(SceneFlags.FadeToBlackscreenOnCancel))
			CancelScene(sceneInstanceId, false);
	}

	public void OnSceneComplete(uint sceneInstanceId)
	{
		if (!HasScene(sceneInstanceId))
			return;

		if (_isDebuggingScenes)
			Player.SendSysMessage(CypherStrings.CommandSceneDebugComplete, sceneInstanceId);

		var sceneTemplate = GetSceneTemplateFromInstanceId(sceneInstanceId);

		// Must be done before removing aura
		RemoveSceneInstanceId(sceneInstanceId);

		if (sceneTemplate.SceneId != 0)
			RemoveAurasDueToSceneId(sceneTemplate.SceneId);

		Global.ScriptMgr.RunScript<ISceneOnSceneComplete>(script => script.OnSceneComplete(Player, sceneInstanceId, sceneTemplate), sceneTemplate.ScriptId);

		if (sceneTemplate.PlaybackFlags.HasFlag(SceneFlags.FadeToBlackscreenOnComplete))
			CancelScene(sceneInstanceId, false);
	}

	public void CancelSceneBySceneId(uint sceneId)
	{
		List<uint> instancesIds = new();

		foreach (var pair in _scenesByInstance)
			if (pair.Value.SceneId == sceneId)
				instancesIds.Add(pair.Key);

		foreach (var sceneInstanceId in instancesIds)
			CancelScene(sceneInstanceId);
	}

	public void CancelSceneByPackageId(uint sceneScriptPackageId)
	{
		List<uint> instancesIds = new();

		foreach (var sceneTemplate in _scenesByInstance)
			if (sceneTemplate.Value.ScenePackageId == sceneScriptPackageId)
				instancesIds.Add(sceneTemplate.Key);

		foreach (var sceneInstanceId in instancesIds)
			CancelScene(sceneInstanceId);
	}

	public uint GetActiveSceneCount(uint sceneScriptPackageId = 0)
	{
		uint activeSceneCount = 0;

		foreach (var sceneTemplate in _scenesByInstance.Values)
			if (sceneScriptPackageId == 0 || sceneTemplate.ScenePackageId == sceneScriptPackageId)
				++activeSceneCount;

		return activeSceneCount;
	}

	public void TriggerDelayedScenes()
	{
		foreach (var playScene in _delayedScenes)
			Player.SendPacket(playScene);

		_delayedScenes.Clear();
	}

	public Dictionary<uint, SceneTemplate> GetSceneTemplateByInstanceMap()
	{
		return _scenesByInstance;
	}

	public void ToggleDebugSceneMode()
	{
		_isDebuggingScenes = !_isDebuggingScenes;
	}

	public bool IsInDebugSceneMode()
	{
		return _isDebuggingScenes;
	}

	void CancelScene(uint sceneInstanceId, bool removeFromMap = true)
	{
		if (removeFromMap)
			RemoveSceneInstanceId(sceneInstanceId);

		CancelScene cancelScene = new();
		cancelScene.SceneInstanceID = sceneInstanceId;
		Player.SendPacket(cancelScene);
	}

	bool HasScene(uint sceneInstanceId, uint sceneScriptPackageId = 0)
	{
		var sceneTempalte = _scenesByInstance.LookupByKey(sceneInstanceId);

		if (sceneTempalte != null)
			return sceneScriptPackageId == 0 || sceneScriptPackageId == sceneTempalte.ScenePackageId;

		return false;
	}

	void AddInstanceIdToSceneMap(uint sceneInstanceId, SceneTemplate sceneTemplate)
	{
		_scenesByInstance[sceneInstanceId] = sceneTemplate;
	}

	void RemoveSceneInstanceId(uint sceneInstanceId)
	{
		_scenesByInstance.Remove(sceneInstanceId);
	}

	void RemoveAurasDueToSceneId(uint sceneId)
	{
		var scenePlayAuras = Player.GetAuraEffectsByType(AuraType.PlayScene);

		foreach (var scenePlayAura in scenePlayAuras)
			if (scenePlayAura.MiscValue == sceneId)
			{
				Player.RemoveAura(scenePlayAura.Base);

				break;
			}
	}

	SceneTemplate GetSceneTemplateFromInstanceId(uint sceneInstanceId)
	{
		return _scenesByInstance.LookupByKey(sceneInstanceId);
	}

	void RecreateScene(uint sceneScriptPackageId, SceneFlags playbackflags, Position position = null)
	{
		CancelSceneByPackageId(sceneScriptPackageId);
		PlaySceneByPackageId(sceneScriptPackageId, playbackflags, position);
	}

	uint GetNewStandaloneSceneInstanceId()
	{
		return ++_standaloneSceneInstanceId;
	}
}
