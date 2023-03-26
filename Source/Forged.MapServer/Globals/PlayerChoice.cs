// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class PlayerChoice
{
    public int ChoiceId;
    public int UiTextureKitId;
    public uint SoundKitId;
    public uint CloseSoundKitId;
    public long Duration;
    public string Question;
    public string PendingChoiceText;
    public List<PlayerChoiceResponse> Responses = new();
    public bool HideWarboardHeader;
    public bool KeepOpenAfterChoice;

    public PlayerChoiceResponse GetResponse(int responseId)
    {
        return Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseId == responseId);
    }

    public PlayerChoiceResponse GetResponseByIdentifier(int responseIdentifier)
    {
        return Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseIdentifier == responseIdentifier);
    }
}