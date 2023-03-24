// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;

namespace Game.Common.Networking.Packets.Character;

public class CharRaceOrFactionChangeInfo
{
	public Race RaceID = Race.None;
	public Race InitialRaceID = Race.None;
	public Gender SexID = Gender.None;
	public ObjectGuid Guid;
	public bool FactionChange;
	public string Name;
	public Array<ChrCustomizationChoice> Customizations = new(72);
}
