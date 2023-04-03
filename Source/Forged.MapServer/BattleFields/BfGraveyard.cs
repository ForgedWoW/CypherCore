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
    protected BattleField m_Bf;
    private readonly List<ObjectGuid> m_ResurrectQueue = new();
    private readonly ObjectGuid[] m_SpiritGuide = new ObjectGuid[SharedConst.PvpTeamsCount];
    private uint m_ControlTeam;
    private uint m_GraveyardId;

    public BfGraveyard(BattleField battlefield)
    {
        m_Bf = battlefield;
        m_GraveyardId = 0;
        m_ControlTeam = TeamIds.Neutral;
        m_SpiritGuide[0] = ObjectGuid.Empty;
        m_SpiritGuide[1] = ObjectGuid.Empty;
    }

    public void AddPlayer(ObjectGuid playerGuid)
    {
        if (!m_ResurrectQueue.Contains(playerGuid))
        {
            m_ResurrectQueue.Add(playerGuid);
            var player = Global.ObjAccessor.FindPlayer(playerGuid);

            if (player)
                player.CastSpell(player, BattlegroundConst.SpellWaitingForResurrect, true);
        }
    }

    public uint GetControlTeamId()
    {
        return m_ControlTeam;
    }

    public float GetDistance(Player player)
    {
        var safeLoc = Global.ObjectMgr.GetWorldSafeLoc(m_GraveyardId);

        return player.Location.GetDistance2d(safeLoc.Loc.X, safeLoc.Loc.Y);
    }

    // Get the graveyard's ID.
    public uint GetGraveyardId()
    {
        return m_GraveyardId;
    }

    // For changing graveyard control
    public void GiveControlTo(uint team)
    {
        // Guide switching
        // Note: Visiblity changes are made by phasing
        /*if (m_SpiritGuide[1 - team])
            m_SpiritGuide[1 - team].SetVisible(false);
        if (m_SpiritGuide[team])
            m_SpiritGuide[team].SetVisible(true);*/

        m_ControlTeam = team;
        // Teleport to other graveyard, player witch were on this graveyard
        RelocateDeadPlayers();
    }

    public bool HasNpc(ObjectGuid guid)
    {
        if (m_SpiritGuide[TeamIds.Alliance].IsEmpty || m_SpiritGuide[TeamIds.Horde].IsEmpty)
            return false;

        if (!m_Bf.GetCreature(m_SpiritGuide[TeamIds.Alliance]) ||
            !m_Bf.GetCreature(m_SpiritGuide[TeamIds.Horde]))
            return false;

        return (m_SpiritGuide[TeamIds.Alliance] == guid || m_SpiritGuide[TeamIds.Horde] == guid);
    }

    // Check if a player is in this graveyard's ressurect queue
    public bool HasPlayer(ObjectGuid guid)
    {
        return m_ResurrectQueue.Contains(guid);
    }

    public void Initialize(uint startControl, uint graveyardId)
    {
        m_ControlTeam = startControl;
        m_GraveyardId = graveyardId;
    }

    public void RemovePlayer(ObjectGuid playerGuid)
    {
        m_ResurrectQueue.Remove(playerGuid);

        var player = Global.ObjAccessor.FindPlayer(playerGuid);

        if (player)
            player.RemoveAura(BattlegroundConst.SpellWaitingForResurrect);
    }

    public void Resurrect()
    {
        if (m_ResurrectQueue.Empty())
            return;

        foreach (var guid in m_ResurrectQueue)
        {
            // Get player object from his guid
            var player = Global.ObjAccessor.FindPlayer(guid);

            if (!player)
                continue;

            // Check  if the player is in world and on the good graveyard
            if (player.IsInWorld)
            {
                var spirit = m_Bf.GetCreature(m_SpiritGuide[m_ControlTeam]);

                if (spirit)
                    spirit.CastSpell(spirit, BattlegroundConst.SpellSpiritHeal, true);
            }

            // Resurect player
            player.CastSpell(player, BattlegroundConst.SpellResurrectionVisual, true);
            player.ResurrectPlayer(1.0f);
            player.CastSpell(player, 6962, true);
            player.CastSpell(player, BattlegroundConst.SpellSpiritHealMana, true);

            player.SpawnCorpseBones(false);
        }

        m_ResurrectQueue.Clear();
    }

    public void SetSpirit(Creature spirit, int teamIndex)
    {
        if (!spirit)
        {
            Log.Logger.Error("BfGraveyard:SetSpirit: Invalid Spirit.");

            return;
        }

        m_SpiritGuide[teamIndex] = spirit.GUID;
        spirit.ReactState = ReactStates.Passive;
    }

    private void RelocateDeadPlayers()
    {
        WorldSafeLocsEntry closestGrave = null;

        foreach (var guid in m_ResurrectQueue)
        {
            var player = Global.ObjAccessor.FindPlayer(guid);

            if (!player)
                continue;

            if (closestGrave != null)
            {
                player.TeleportTo(closestGrave.Loc);
            }
            else
            {
                closestGrave = m_Bf.GetClosestGraveYard(player);

                if (closestGrave != null)
                    player.TeleportTo(closestGrave.Loc);
            }
        }
    }
}