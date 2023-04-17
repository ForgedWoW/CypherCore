// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage;

public class ConversationLineTemplate
{
    public byte ActorIdx { get; set; }

    // Index from conversation_actors
    public byte Flags { get; set; }

    public uint Id { get; set; }         // Link to ConversationLine.db2
    public uint UiCameraID { get; set; } // Link to UiCamera.db2
}