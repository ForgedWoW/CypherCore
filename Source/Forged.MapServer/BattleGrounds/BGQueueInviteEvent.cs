// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.BattleGrounds;

/// <summary>
///     This class is used to invite player to BG again, when minute lasts from his first invitation
///     it is capable to solve all possibilities
/// </summary>
internal class BGQueueInviteEvent : BasicEvent
{
    private readonly ArenaTypes m_ArenaType;
    private readonly uint m_BgInstanceGUID;
    private readonly BattlegroundTypeId m_BgTypeId;
    private readonly ObjectGuid m_PlayerGuid;
    private readonly uint m_RemoveTime;

    public BGQueueInviteEvent(ObjectGuid plGuid, uint bgInstanceGUID, BattlegroundTypeId bgTypeId, ArenaTypes arenaType, uint removeTime)
    {
        m_PlayerGuid = plGuid;
        m_BgInstanceGUID = bgInstanceGUID;
        m_BgTypeId = bgTypeId;
        m_ArenaType = arenaType;
        m_RemoveTime = removeTime;
    }

    public override void Abort(ulong e_time) { }

    public override bool Execute(ulong etime, uint pTime)
    {
        var player = Global.ObjAccessor.FindPlayer(m_PlayerGuid);

        // player logged off (we should do nothing, he is correctly removed from queue in another procedure)
        if (!player)
            return true;

        var bg = Global.BattlegroundMgr.GetBattleground(m_BgInstanceGUID, m_BgTypeId);

        //if Battleground ended and its instance deleted - do nothing
        if (bg == null)
            return true;

        var bgQueueTypeId = bg.GetQueueId();
        var queueSlot = player.GetBattlegroundQueueIndex(bgQueueTypeId);

        if (queueSlot < SharedConst.PvpTeamsCount) // player is in queue or in Battleground
        {
            // check if player is invited to this bg
            var bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);

            if (bgQueue.IsPlayerInvited(m_PlayerGuid, m_BgInstanceGUID, m_RemoveTime))
            {
                Global.BattlegroundMgr.BuildBattlegroundStatusNeedConfirmation(out var battlefieldStatus, bg, player, queueSlot, player.GetBattlegroundQueueJoinTime(bgQueueTypeId), BattlegroundConst.InviteAcceptWaitTime - BattlegroundConst.InvitationRemindTime, m_ArenaType);
                player.SendPacket(battlefieldStatus);
            }
        }

        return true; //event will be deleted
    }
}