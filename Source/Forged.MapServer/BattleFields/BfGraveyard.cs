// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.BattleFields;

public class BfGraveyard
{
    protected BattleField Bf;
    private readonly ObjectAccessor _objectAccessor;
    private readonly List<ObjectGuid> _resurrectQueue = new();
    private readonly ObjectGuid[] _spiritGuide = new ObjectGuid[SharedConst.PvpTeamsCount];

    public BfGraveyard(BattleField battlefield, ObjectAccessor objectAccessor)
    {
        Bf = battlefield;
        _objectAccessor = objectAccessor;
        GraveyardId = 0;
        ControlTeamId = TeamIds.Neutral;
        _spiritGuide[0] = ObjectGuid.Empty;
        _spiritGuide[1] = ObjectGuid.Empty;
    }

    public uint ControlTeamId { get; private set; }

    public uint GraveyardId { get; private set; }

    public void AddPlayer(ObjectGuid playerGuid)
    {
        if (_resurrectQueue.Contains(playerGuid))
            return;

        _resurrectQueue.Add(playerGuid);
        var player = _objectAccessor.FindPlayer(playerGuid);

        if (player)
            player.SpellFactory.CastSpell(player, BattlegroundConst.SpellWaitingForResurrect, true);
    }

    public float GetDistance(Player player)
    {
        var safeLoc = player.ObjectManager.GetWorldSafeLoc(GraveyardId);

        return player.Location.GetDistance2d(safeLoc.Loc.X, safeLoc.Loc.Y);
    }

    // Get the graveyard's ID.
    // For changing graveyard control
    public void GiveControlTo(uint team)
    {
        // Guide switching
        // Note: Visiblity changes are made by phasing
        /*if (_spiritGuide[1 - team])
            _spiritGuide[1 - team].SetVisible(false);
        if (_spiritGuide[team])
            _spiritGuide[team].SetVisible(true);*/

        ControlTeamId = team;
        // Teleport to other graveyard, player witch were on this graveyard
        RelocateDeadPlayers();
    }

    public bool HasNpc(ObjectGuid guid)
    {
        if (_spiritGuide[TeamIds.Alliance].IsEmpty || _spiritGuide[TeamIds.Horde].IsEmpty)
            return false;

        if (!Bf.GetCreature(_spiritGuide[TeamIds.Alliance]) ||
            !Bf.GetCreature(_spiritGuide[TeamIds.Horde]))
            return false;

        return (_spiritGuide[TeamIds.Alliance] == guid || _spiritGuide[TeamIds.Horde] == guid);
    }

    // Check if a player is in this graveyard's ressurect queue
    public bool HasPlayer(ObjectGuid guid)
    {
        return _resurrectQueue.Contains(guid);
    }

    public void Initialize(uint startControl, uint graveyardId)
    {
        ControlTeamId = startControl;
        GraveyardId = graveyardId;
    }

    public void RemovePlayer(ObjectGuid playerGuid)
    {
        _resurrectQueue.Remove(playerGuid);

        var player = _objectAccessor.FindPlayer(playerGuid);

        if (player)
            player.RemoveAura(BattlegroundConst.SpellWaitingForResurrect);
    }

    public void Resurrect()
    {
        if (_resurrectQueue.Empty())
            return;

        foreach (var guid in _resurrectQueue)
        {
            // Get player object from his guid
            var player = _objectAccessor.FindPlayer(guid);

            if (!player)
                continue;

            // Check  if the player is in world and on the good graveyard
            if (player.Location.IsInWorld)
            {
                var spirit = Bf.GetCreature(_spiritGuide[ControlTeamId]);

                if (spirit)
                    spirit.SpellFactory.CastSpell(spirit, BattlegroundConst.SpellSpiritHeal, true);
            }

            // Resurect player
            player.SpellFactory.CastSpell(player, BattlegroundConst.SpellResurrectionVisual, true);
            player.ResurrectPlayer(1.0f);
            player.SpellFactory.CastSpell(player, 6962, true);
            player.SpellFactory.CastSpell(player, BattlegroundConst.SpellSpiritHealMana, true);

            player.SpawnCorpseBones(false);
        }

        _resurrectQueue.Clear();
    }

    public void SetSpirit(Creature spirit, int teamIndex)
    {
        if (!spirit)
        {
            Log.Logger.Error("BfGraveyard:SetSpirit: Invalid Spirit.");

            return;
        }

        _spiritGuide[teamIndex] = spirit.GUID;
        spirit.ReactState = ReactStates.Passive;
    }

    private void RelocateDeadPlayers()
    {
        WorldSafeLocsEntry closestGrave = null;

        foreach (var guid in _resurrectQueue)
        {
            var player = _objectAccessor.FindPlayer(guid);

            if (!player)
                continue;

            if (closestGrave != null)
            {
                player.TeleportTo(closestGrave.Loc);
            }
            else
            {
                closestGrave = Bf.GetClosestGraveYard(player);

                if (closestGrave != null)
                    player.TeleportTo(closestGrave.Loc);
            }
        }
    }
}