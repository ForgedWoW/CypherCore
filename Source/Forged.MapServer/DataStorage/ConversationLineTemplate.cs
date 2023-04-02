﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage;

public class ConversationLineTemplate
{
    public byte ActorIdx;
    // Index from conversation_actors
    public byte Flags;

    public uint Id;         // Link to ConversationLine.db2
    public uint UiCameraID; // Link to UiCamera.db2
}