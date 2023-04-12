// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class PlayerChoice
{
    public int ChoiceId { get; set; }
    public uint CloseSoundKitId { get; set; }
    public long Duration { get; set; }
    public bool HideWarboardHeader { get; set; }
    public bool KeepOpenAfterChoice { get; set; }
    public string PendingChoiceText { get; set; }
    public string Question { get; set; }
    public List<PlayerChoiceResponse> Responses { get; set; } = new();
    public uint SoundKitId { get; set; }
    public int UiTextureKitId { get; set; }

    public PlayerChoiceResponse GetResponse(int responseId)
    {
        return Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseId == responseId);
    }

    public PlayerChoiceResponse GetResponseByIdentifier(int responseIdentifier)
    {
        return Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseIdentifier == responseIdentifier);
    }
}