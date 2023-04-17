// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IConversation;

namespace Scripts.World;

[Script]
internal class ConversationAlliedRaceDkDefenderOfAzeroth : ScriptObjectAutoAddDBBound, IConversationOnConversationCreate, IConversationOnConversationLineStarted
{
    private const uint NpcTalkToYourCommanderCredit = 161709;
    private const uint NpcListenToYourCommanderCredit = 163027;
    private const uint ConversationLinePlayer = 32926;

    public ConversationAlliedRaceDkDefenderOfAzeroth() : base("conversation_allied_race_dk_defender_of_azeroth") { }

    public void OnConversationCreate(Conversation conversation, Unit creator)
    {
        var player = creator.AsPlayer;

        player?.KilledMonsterCredit(NpcTalkToYourCommanderCredit);
    }

    public void OnConversationLineStarted(Conversation conversation, uint lineId, Player sender)
    {
        if (lineId != ConversationLinePlayer)
            return;

        sender.KilledMonsterCredit(NpcListenToYourCommanderCredit);
    }
}