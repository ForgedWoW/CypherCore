// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct ABNodeInfo
{
    public uint NodeId;

    public uint TextAllianceAssaulted;

    public uint TextAllianceClaims;

    public uint TextAllianceDefended;

    public uint TextAllianceTaken;

    public uint TextHordeAssaulted;

    public uint TextHordeClaims;

    public uint TextHordeDefended;

    public uint TextHordeTaken;

    public ABNodeInfo(uint nodeId, uint textAllianceAssaulted, uint textHordeAssaulted, uint textAllianceTaken, uint textHordeTaken, uint textAllianceDefended, uint textHordeDefended, uint textAllianceClaims, uint textHordeClaims)
    {
        NodeId = nodeId;
        TextAllianceAssaulted = textAllianceAssaulted;
        TextHordeAssaulted = textHordeAssaulted;
        TextAllianceTaken = textAllianceTaken;
        TextHordeTaken = textHordeTaken;
        TextAllianceDefended = textAllianceDefended;
        TextHordeDefended = textHordeDefended;
        TextAllianceClaims = textAllianceClaims;
        TextHordeClaims = textHordeClaims;
    }
}