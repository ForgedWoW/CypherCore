// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Forged.MapServer.BattleGrounds;

/// <summary>
///     This class is used to remove player from BG queue after 1 minute 20 seconds from first invitation
///     We must store removeInvite time in case player left queue and joined and is invited again
///     We must store bgQueueTypeId, because Battleground can be deleted already, when player entered it
/// </summary>
internal class BGQueueRemoveEvent : BasicEvent
{
    private readonly uint m_BgInstanceGUID;
    private readonly BattlegroundQueueTypeId m_BgQueueTypeId;
    private readonly ObjectGuid m_PlayerGuid;
    private readonly uint m_RemoveTime;

    public BGQueueRemoveEvent(ObjectGuid plGuid, uint bgInstanceGUID, BattlegroundQueueTypeId bgQueueTypeId, uint removeTime)
    {
        m_PlayerGuid = plGuid;
        m_BgInstanceGUID = bgInstanceGUID;
        m_RemoveTime = removeTime;
        m_BgQueueTypeId = bgQueueTypeId;
    }

    public override void Abort(ulong e_time) { }

    public override bool Execute(ulong etime, uint pTime)
    {
        var player = Global.ObjAccessor.FindPlayer(m_PlayerGuid);

        if (!player)
            // player logged off (we should do nothing, he is correctly removed from queue in another procedure)
            return true;

        var bg = Global.BattlegroundMgr.GetBattleground(m_BgInstanceGUID, (BattlegroundTypeId)m_BgQueueTypeId.BattlemasterListId);
        //Battleground can be deleted already when we are removing queue info
        //bg pointer can be NULL! so use it carefully!

        var queueSlot = player.GetBattlegroundQueueIndex(m_BgQueueTypeId);

        if (queueSlot < SharedConst.PvpTeamsCount) // player is in queue, or in Battleground
        {
            // check if player is in queue for this BG and if we are removing his invite event
            var bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(m_BgQueueTypeId);

            if (bgQueue.IsPlayerInvited(m_PlayerGuid, m_BgInstanceGUID, m_RemoveTime))
            {
                Log.Logger.Debug("Battleground: removing player {0} from bg queue for instance {1} because of not pressing enter battle in time.", player.GUID.ToString(), m_BgInstanceGUID);

                player.RemoveBattlegroundQueueId(m_BgQueueTypeId);
                bgQueue.RemovePlayer(m_PlayerGuid, true);

                //update queues if Battleground isn't ended
                if (bg && bg.IsBattleground() && bg.GetStatus() != BattlegroundStatus.WaitLeave)
                    Global.BattlegroundMgr.ScheduleQueueUpdate(0, m_BgQueueTypeId, bg.GetBracketId());

                Global.BattlegroundMgr.BuildBattlegroundStatusNone(out var battlefieldStatus, player, queueSlot, player.GetBattlegroundQueueJoinTime(m_BgQueueTypeId));
                player.SendPacket(battlefieldStatus);
            }
        }

        //event will be deleted
        return true;
    }
}