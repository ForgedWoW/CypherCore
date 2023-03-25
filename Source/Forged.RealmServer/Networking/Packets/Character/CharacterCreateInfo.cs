// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class CharacterCreateInfo
{
	// User specified variables
	public Race RaceId = Race.None;
	public PlayerClass ClassId = PlayerClass.None;
	public Gender Sex = Gender.None;
	public Array<ChrCustomizationChoice> Customizations = new(72);
	public uint? TemplateSet;
	public bool IsTrialBoost;
	public bool UseNPE;
	public string Name;

	// Server side data
	public byte CharCount = 0;
}