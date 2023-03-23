// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.DataStorage.ClientReader;
using Game.Common.DataStorage.Structs.E;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Chat;
using Game.Common.Scripting.Interfaces.IPlayer;
using Game.Common.Server;

namespace Game.Common.Handlers;

public class ChatHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly DB6Storage<EmotesTextRecord> _emotes;

    public ChatHandler(WorldSession session, DB6Storage<EmotesTextRecord> emotes)
    {
        _session = session;
        _emotes = emotes;
    }

	[WorldPacketHandler(ClientOpcodes.SendTextEmote, Processing = PacketProcessing.Inplace)]
	void HandleTextEmote(CTextEmote packet)
	{
		if (!_session.Player.IsAlive)
			return;

		if (!_session.CanSpeak)
		{
			var timeStr = Time.secsToTimeString((ulong)(_session.MuteTime - GameTime.GetGameTime()));
            _session.SendNotification(CypherStrings.WaitBeforeSpeaking, timeStr);

			return;
		}

		Global.ScriptMgr.ForEach<IPlayerOnTextEmote>(p => p.OnTextEmote(_session.Player, (uint)packet.SoundIndex, (uint)packet.EmoteID, packet.Target));
		var em = _emotes.LookupByKey((uint)packet.EmoteID);

		if (em == null)
			return;

		var emote = (Emote)em.EmoteId;

		switch (emote)
		{
			case Emote.StateSleep:
			case Emote.StateSit:
			case Emote.StateKneel:
			case Emote.OneshotNone:
				break;
			case Emote.StateDance:
			case Emote.StateRead:
				_session.Player.EmoteState = emote;

				break;
			default:
				// Only allow text-emotes for "dead" entities (feign death included)
				if (_session.Player.HasUnitState(UnitState.Died))
					break;

				_session.Player.HandleEmoteCommand(emote, null, packet.SpellVisualKitIDs, packet.SequenceVariation);

				break;
		}

		STextEmote textEmote = new();
		textEmote.SourceGUID = _session.Player.GUID;
		textEmote.SourceAccountGUID = _session.AccountGUID;
		textEmote.TargetGUID = packet.Target;
		textEmote.EmoteID = packet.EmoteID;
		textEmote.SoundIndex = packet.SoundIndex;
		_session.Player.SendMessageToSetInRange(textEmote, WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote), true);

		var unit = Global.ObjAccessor.GetUnit(_session.Player, packet.Target);

		_session.Player.UpdateCriteria(CriteriaType.DoEmote, (uint)packet.EmoteID, 0, 0, unit);

		// Send scripted event call
		if (unit)
		{
			var creature = unit.AsCreature;

			if (creature)
				creature.AI.ReceiveEmote(_session.Player, (TextEmotes)packet.EmoteID);
		}

		if (emote != Emote.OneshotNone)
			_session.Player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Anim);
	}
}
