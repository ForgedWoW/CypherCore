// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.BattleGrounds;

/// <summary>
///     This class is used to invite player to BG again, when minute lasts from his first invitation
///     it is capable to solve all possibilities
/// </summary>
internal class BGQueueInviteEvent : BasicEvent
{
    private readonly ArenaTypes _arenaType;
    private readonly BattlegroundManager _battlegroundManager;
    private readonly uint _bgInstanceGUID;
    private readonly BattlegroundTypeId _bgTypeId;
    private readonly ObjectAccessor _objectAccessor;
    private readonly ObjectGuid _playerGuid;
    private readonly uint _removeTime;

    public BGQueueInviteEvent(ObjectGuid plGuid, uint bgInstanceGUID, BattlegroundTypeId bgTypeId, ArenaTypes arenaType, uint removeTime, BattlegroundManager battlegroundManager, ObjectAccessor objectAccessor)
    {
        _playerGuid = plGuid;
        _bgInstanceGUID = bgInstanceGUID;
        _bgTypeId = bgTypeId;
        _arenaType = arenaType;
        _removeTime = removeTime;
        _battlegroundManager = battlegroundManager;
        _objectAccessor = objectAccessor;
    }

    public override void Abort(ulong eTime) { }

    public override bool Execute(ulong etime, uint pTime)
    {
        var player = _objectAccessor.FindPlayer(_playerGuid);

        // player logged off (we should do nothing, he is correctly removed from queue in another procedure)
        if (!player)
            return true;

        var bg = _battlegroundManager.GetBattleground(_bgInstanceGUID, _bgTypeId);

        //if Battleground ended and its instance deleted - do nothing
        if (bg == null)
            return true;

        var bgQueueTypeId = bg.GetQueueId();
        var queueSlot = player.GetBattlegroundQueueIndex(bgQueueTypeId);

        if (queueSlot >= SharedConst.PvpTeamsCount) // player is in queue or in Battleground
            return true;                            //event will be deleted

        // check if player is invited to this bg
        var bgQueue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);

        if (!bgQueue.IsPlayerInvited(_playerGuid, _bgInstanceGUID, _removeTime))
            return true; //event will be deleted

        _battlegroundManager.BuildBattlegroundStatusNeedConfirmation(out var battlefieldStatus, bg, player, queueSlot, player.GetBattlegroundQueueJoinTime(bgQueueTypeId), BattlegroundConst.InviteAcceptWaitTime - BattlegroundConst.InvitationRemindTime, _arenaType);
        player.SendPacket(battlefieldStatus);

        return true; //event will be deleted
    }
}