// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IScene;

namespace Scripts.World.SceneScripts;

internal struct SpellIds
{
    public const uint DEATHWING_SIMULATOR = 201184;
}

[Script]
internal class SceneDeathwingSimulator : ScriptObjectAutoAddDBBound, ISceneOnSceneTrigger
{
    public SceneDeathwingSimulator() : base("scene_deathwing_simulator") { }

    // Called when a player receive trigger from scene
    public void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
    {
        if (triggerName == "Burn Player")
            player.SpellFactory.CastSpell(player, SpellIds.DEATHWING_SIMULATOR, true); // Deathwing Simulator Burn player
    }
}