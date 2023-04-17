// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IScene;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.DragonIsles;

internal struct SpellIds
{
    // Spells
    public const uint DRACTHYR_LOGIN = 369728;       // teleports to random room, plays scene for the room, binds the home position
    public const uint STASIS1 = 369735;             // triggers 366620
    public const uint STASIS2 = 366620;             // triggers 366636
    public const uint STASIS3 = 366636;             // removes 365560, sends first quest (64864)
    public const uint STASIS4 = 365560;             // freeze the Target
    public const uint DRACTHYR_MOVIE_ROOM01 = 394245; // scene for room 1
    public const uint DRACTHYR_MOVIE_ROOM02 = 394279; // scene for room 2
    public const uint DRACTHYR_MOVIE_ROOM03 = 394281; // scene for room 3

    public const uint DRACTHYR_MOVIE_ROOM04 = 394282; // scene for room 4
    //public const uint DracthyrMovieRoom05    = 394283, // scene for room 5 (only plays sound, unused?)
}

internal struct MiscConst
{
    public static Tuple<uint, Position>[] LoginRoomData =
    {
        Tuple.Create(SpellIds.DRACTHYR_MOVIE_ROOM01, new Position(5725.32f, -3024.26f, 251.047f, 0.01745329238474369f)), Tuple.Create(SpellIds.DRACTHYR_MOVIE_ROOM02, new Position(5743.03f, -3067.28f, 251.047f, 0.798488140106201171f)), Tuple.Create(SpellIds.DRACTHYR_MOVIE_ROOM03, new Position(5787.1597f, -3083.3906f, 251.04698f, 1.570796370506286621f)), Tuple.Create(SpellIds.DRACTHYR_MOVIE_ROOM04, new Position(5829.32f, -3064.49f, 251.047f, 2.364955902099609375f))
    };
}

[Script] // 369728 - Dracthyr Login
internal class SpellDracthyrLogin : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleTeleport, 0, SpellEffectName.TeleportUnits, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleTeleport(int effIndex)
    {
        var room = MiscConst.LoginRoomData[RandomHelper.URand(0, 3)];

        var dest = HitUnit.Location;
        ExplTargetDest = dest;

        HitDest.Relocate(room.Item2);

        Caster.SpellFactory.CastSpell(HitUnit, room.Item1, true);
    }
}

[Script] // 3730 - Dracthyr Evoker Intro (Post Movie)
internal class SceneDracthyrEvokerIntro : ScriptObjectAutoAddDBBound, ISceneOnSceneChancel, ISceneOnSceneComplete
{
    public SceneDracthyrEvokerIntro() : base("scene_dracthyr_evoker_intro") { }

    public void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
    {
        player.SpellFactory.CastSpell(player, SpellIds.STASIS1, true);
    }

    public void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
    {
        player.SpellFactory.CastSpell(player, SpellIds.STASIS1, true);
    }
}