// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
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
    private readonly BattlegroundManager _battlegroundManager;
    private readonly uint _bgInstanceGUID;
    private readonly BattlegroundQueueTypeId _bgQueueTypeId;
    private readonly ObjectAccessor _objectAccessor;
    private readonly ObjectGuid _playerGuid;
    private readonly uint _removeTime;

    public BGQueueRemoveEvent(ObjectGuid plGuid, uint bgInstanceGUID, BattlegroundQueueTypeId bgQueueTypeId, uint removeTime, BattlegroundManager battlegroundManager, ObjectAccessor objectAccessor)
    {
        _playerGuid = plGuid;
        _bgInstanceGUID = bgInstanceGUID;
        _removeTime = removeTime;
        _battlegroundManager = battlegroundManager;
        _objectAccessor = objectAccessor;
        _bgQueueTypeId = bgQueueTypeId;
    }

    public override void Abort(ulong eTime)
    { }

    public override bool Execute(ulong etime, uint pTime)
    {
        var player = _objectAccessor.FindPlayer(_playerGuid);

        if (player == null)
            // player logged off (we should do nothing, he is correctly removed from queue in another procedure)
            return true;

        var bg = _battlegroundManager.GetBattleground(_bgInstanceGUID, (BattlegroundTypeId)_bgQueueTypeId.BattlemasterListId);
        //Battleground can be deleted already when we are removing queue info
        //bg pointer can be NULL! so use it carefully!

        var queueSlot = player.GetBattlegroundQueueIndex(_bgQueueTypeId);

        if (queueSlot >= SharedConst.PvpTeamsCount) // player is in queue, or in Battleground
            return true;

        // check if player is in queue for this BG and if we are removing his invite event
        var bgQueue = _battlegroundManager.GetBattlegroundQueue(_bgQueueTypeId);

        if (!bgQueue.IsPlayerInvited(_playerGuid, _bgInstanceGUID, _removeTime))
            return true;

        Log.Logger.Debug("Battleground: removing player {0} from bg queue for instance {1} because of not pressing enter battle in time.", player.GUID.ToString(), _bgInstanceGUID);

        player.RemoveBattlegroundQueueId(_bgQueueTypeId);
        bgQueue.RemovePlayer(_playerGuid, true);

        //update queues if Battleground isn't ended
        if (bg is { IsBattleground: true } && bg.Status != BattlegroundStatus.WaitLeave)
            _battlegroundManager.ScheduleQueueUpdate(0, _bgQueueTypeId, bg.BracketId);

        _battlegroundManager.BuildBattlegroundStatusNone(out var battlefieldStatus, player, queueSlot, player.GetBattlegroundQueueJoinTime(_bgQueueTypeId));
        player.SendPacket(battlefieldStatus);

        //event will be deleted
        return true;
    }
}