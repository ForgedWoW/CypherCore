// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class BgData
{
    public BgData()
    {
        BgTypeId = BattlegroundTypeId.None;
        ClearTaxiPath();
        JoinPos = new WorldLocation();
    }

    public byte BgAfkReportedCount { get; set; }
    public long BgAfkReportedTimer { get; set; }
    public List<ObjectGuid> BgAfkReporter { get; set; } = new();
    public uint BgInstanceId { get; set; } //< This variable is set to bg.m_InstanceID,
    public uint BgTeam { get; set; }

    //  when player is teleported to BG - (it is Battleground's GUID)
    public BattlegroundTypeId BgTypeId { get; set; }
    public bool HasTaxiPath => TaxiPath[0] != 0 && TaxiPath[1] != 0;
    public WorldLocation JoinPos { get; set; }
    public uint MountSpell { get; set; }
    public uint[] TaxiPath { get; set; } = new uint[2];

    //< From where player entered BG
    public void ClearTaxiPath()
    {
        TaxiPath[0] = TaxiPath[1] = 0;
    }
}