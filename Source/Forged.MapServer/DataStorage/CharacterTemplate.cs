// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.DataStorage;

public class CharacterTemplate
{
    public List<CharacterTemplateClass> Classes;
    public string Description;
    public byte Level;
    public string Name;
    public uint TemplateSetId;
}