// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharRaceOrFactionChangeInfo
{
    public Array<ChrCustomizationChoice> Customizations = new(125);
    public bool FactionChange;
    public ObjectGuid Guid;
    public Race InitialRaceID = Race.None;
    public string Name;
    public Race RaceID = Race.None;
    public Gender SexID = Gender.None;
}