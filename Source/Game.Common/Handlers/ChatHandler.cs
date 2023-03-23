// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Chat;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IPlayer;

namespace Game;

public partial class WorldSession
{

	[WorldPacketHandler(ClientOpcodes.SendTextEmote, Processing = PacketProcessing.Inplace)]
	void HandleTextEmote(CTextEmote packet)
	{
		if (!_player.IsAlive)
			return;

		if (!CanSpeak)
		{
			var timeStr = Time.secsToTimeString((ulong)(MuteTime - GameTime.GetGameTime()));
			SendNotification(CypherStrings.WaitBeforeSpeaking, timeStr);

			return;
		}

		Global.ScriptMgr.ForEach<IPlayerOnTextEmote>(p => p.OnTextEmote(_player, (uint)packet.SoundIndex, (uint)packet.EmoteID, packet.Target));
		var em = CliDB.EmotesTextStorage.LookupByKey(packet.EmoteID);

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
				_player.EmoteState = emote;

				break;
			default:
				// Only allow text-emotes for "dead" entities (feign death included)
				if (_player.HasUnitState(UnitState.Died))
					break;

				_player.HandleEmoteCommand(emote, null, packet.SpellVisualKitIDs, packet.SequenceVariation);

				break;
		}

		STextEmote textEmote = new();
		textEmote.SourceGUID = _player.GUID;
		textEmote.SourceAccountGUID = AccountGUID;
		textEmote.TargetGUID = packet.Target;
		textEmote.EmoteID = packet.EmoteID;
		textEmote.SoundIndex = packet.SoundIndex;
		_player.SendMessageToSetInRange(textEmote, WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote), true);

		var unit = Global.ObjAccessor.GetUnit(_player, packet.Target);

		_player.UpdateCriteria(CriteriaType.DoEmote, (uint)packet.EmoteID, 0, 0, unit);

		// Send scripted event call
		if (unit)
		{
			var creature = unit.AsCreature;

			if (creature)
				creature.AI.ReceiveEmote(_player, (TextEmotes)packet.EmoteID);
		}

		if (emote != Emote.OneshotNone)
			_player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Anim);
	}
}