// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.DataStorage;

public class ConversationTemplate
{
	public uint Id;
	public uint FirstLineId;  // Link to ConversationLine.db2
	public uint TextureKitId; // Background texture
	public uint ScriptId;

	public List<ConversationActorTemplate> Actors = new();
	public List<ConversationLineTemplate> Lines = new();
}