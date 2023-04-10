// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.DataStorage;

public class CharacterTemplate
{
    public List<CharacterTemplateClass> Classes { get; set; }
    public string Description { get; set; }
    public byte Level { get; set; }
    public string Name { get; set; }
    public uint TemplateSetId { get; set; }
}