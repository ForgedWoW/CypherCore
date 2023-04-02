// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.DataStorage;

public class ConversationTemplate
{
    public List<ConversationActorTemplate> Actors = new();
    public uint FirstLineId;
    public uint Id;
    public List<ConversationLineTemplate> Lines = new();

    public uint ScriptId;

    // Link to ConversationLine.db2
    public uint TextureKitId; // Background texture
}