// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(51624)]
public class npc_vanessa_anchor_bunny : ScriptedAI
{
    private uint _achievementTimer;
    private bool _startTimerAchievement;
    private bool _getAchievementPlayers;

    public npc_vanessa_anchor_bunny(Creature creature) : base(creature) { }

    public override void Reset()
    {
        _startTimerAchievement = false;
        _getAchievementPlayers = true;
        _achievementTimer = 300000;
    }

    public override void SetData(uint uiI, uint uiValue)
    {
        if (uiValue == boss_vanessa_vancleef.eAchievementMisc.START_TIMER_ACHIEVEMENT && _startTimerAchievement == false)
            _startTimerAchievement = true;

        if (uiValue == boss_vanessa_vancleef.eAchievementMisc.ACHIEVEMENT_READY_GET && _getAchievementPlayers == true && _startTimerAchievement == true)
        {
            var map = Me.Map;
            var vigorous_vancleef_vindicator = Global.AchievementMgr.GetAchievementByReferencedId(boss_vanessa_vancleef.eAchievementMisc.ACHIEVEMENT_VIGOROUS_VANCLEEF_VINDICATOR).FirstOrDefault();

            if (map != null && map.IsDungeon && map.DifficultyID == Difficulty.Heroic)
            {
                var players = map.Players;

                if (!players.Empty())
                    foreach (var player in map.Players)
                        if (player != null)
                            if (player.GetDistance(Me) < 200.0f)
                                player.CompletedAchievement(vigorous_vancleef_vindicator);
            }
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (_startTimerAchievement == true && _getAchievementPlayers == true)
        {
            if (_achievementTimer <= diff)
                _getAchievementPlayers = false;
            else
                _achievementTimer -= diff;
        }
    }
}