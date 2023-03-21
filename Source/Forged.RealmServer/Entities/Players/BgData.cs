// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Entities;

public class BgData
{
	public uint BgInstanceId { get; set; } //< This variable is set to bg.m_InstanceID,

	//  when player is teleported to BG - (it is Battleground's GUID)
	public BattlegroundTypeId BgTypeId { get; set; }

	public List<ObjectGuid> BgAfkReporter { get; set; } = new();
	public byte BgAfkReportedCount { get; set; }
	public long BgAfkReportedTimer { get; set; }

	public uint BgTeam { get; set; } //< What side the player will be added to

	public uint MountSpell { get; set; }
	public uint[] TaxiPath { get; set; } = new uint[2];

	public WorldLocation JoinPos { get; set; } //< From where player entered BG

	public BgData()
	{
		BgTypeId = BattlegroundTypeId.None;
		ClearTaxiPath();
		JoinPos = new WorldLocation();
	}

	public void ClearTaxiPath()
	{
		TaxiPath[0] = TaxiPath[1] = 0;
	}

	public bool HasTaxiPath()
	{
		return TaxiPath[0] != 0 && TaxiPath[1] != 0;
	}
}