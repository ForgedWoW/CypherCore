// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;

namespace Game.Common.Networking.Packets.Character;

public class CharCustomizeInfo
{
	public ObjectGuid CharGUID;
	public Gender SexID = Gender.None;
	public string CharName;
	public Array<ChrCustomizationChoice> Customizations = new(72);
}
