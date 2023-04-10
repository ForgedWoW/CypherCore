// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.DataStorage;

public class ConversationTemplate
{
    public List<ConversationActorTemplate> Actors { get; set; } = new();
    public uint FirstLineId { get; set; }
    public uint Id { get; set; }
    public List<ConversationLineTemplate> Lines { get; set; } = new();

    public uint ScriptId { get; set; }

    // Link to ConversationLine.db2
    public uint TextureKitId { get; set; } // Background texture
}