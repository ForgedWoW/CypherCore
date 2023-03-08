// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class UpdateTalentData : ServerPacket
{
	public TalentInfoUpdate Info = new();
	public UpdateTalentData() : base(ServerOpcodes.UpdateTalentData, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(Info.ActiveGroup);
		_worldPacket.WriteUInt32(Info.PrimarySpecialization);
		_worldPacket.WriteInt32(Info.TalentGroups.Count);

		foreach (var talentGroupInfo in Info.TalentGroups)
		{
			_worldPacket.WriteUInt32(talentGroupInfo.SpecID);
			_worldPacket.WriteInt32(talentGroupInfo.TalentIDs.Count);
			_worldPacket.WriteInt32(talentGroupInfo.PvPTalents.Count);

			foreach (var talentID in talentGroupInfo.TalentIDs)
				_worldPacket.WriteUInt16(talentID);

			foreach (var talent in talentGroupInfo.PvPTalents)
				talent.Write(_worldPacket);
		}
	}

	public class TalentGroupInfo
	{
		public uint SpecID;
		public List<ushort> TalentIDs = new();
		public List<PvPTalent> PvPTalents = new();
	}

	public class TalentInfoUpdate
	{
		public byte ActiveGroup;
		public uint PrimarySpecialization;
		public List<TalentGroupInfo> TalentGroups = new();
	}
}

class LearnTalents : ClientPacket
{
	public Array<ushort> Talents = new(PlayerConst.MaxTalentTiers);
	public LearnTalents(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadBits<uint>(6);

		for (var i = 0; i < count; ++i)
			Talents[i] = _worldPacket.ReadUInt16();
	}
}

class RespecWipeConfirm : ServerPacket
{
	public ObjectGuid RespecMaster;
	public uint Cost;
	public SpecResetType RespecType;
	public RespecWipeConfirm() : base(ServerOpcodes.RespecWipeConfirm) { }

	public override void Write()
	{
		_worldPacket.WriteInt8((sbyte)RespecType);
		_worldPacket.WriteUInt32(Cost);
		_worldPacket.WritePackedGuid(RespecMaster);
	}
}

class ConfirmRespecWipe : ClientPacket
{
	public ObjectGuid RespecMaster;
	public SpecResetType RespecType;
	public ConfirmRespecWipe(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RespecMaster = _worldPacket.ReadPackedGuid();
		RespecType = (SpecResetType)_worldPacket.ReadUInt8();
	}
}

class LearnTalentFailed : ServerPacket
{
	public uint Reason;
	public int SpellID;
	public List<ushort> Talents = new();
	public LearnTalentFailed() : base(ServerOpcodes.LearnTalentFailed) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Reason, 4);
		_worldPacket.WriteInt32(SpellID);
		_worldPacket.WriteInt32(Talents.Count);

		foreach (var talent in Talents)
			_worldPacket.WriteUInt16(talent);
	}
}

class ActiveGlyphs : ServerPacket
{
	public List<GlyphBinding> Glyphs = new();
	public bool IsFullUpdate;
	public ActiveGlyphs() : base(ServerOpcodes.ActiveGlyphs) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Glyphs.Count);

		foreach (var glyph in Glyphs)
			glyph.Write(_worldPacket);

		_worldPacket.WriteBit(IsFullUpdate);
		_worldPacket.FlushBits();
	}
}

class LearnPvpTalents : ClientPacket
{
	public Array<PvPTalent> Talents = new(4);
	public LearnPvpTalents(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var size = _worldPacket.ReadUInt32();

		for (var i = 0; i < size; ++i)
			Talents[i] = new PvPTalent(_worldPacket);
	}
}

class LearnPvpTalentFailed : ServerPacket
{
	public uint Reason;
	public uint SpellID;
	public List<PvPTalent> Talents = new();
	public LearnPvpTalentFailed() : base(ServerOpcodes.LearnPvpTalentFailed) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Reason, 4);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteInt32(Talents.Count);

		foreach (var pvpTalent in Talents)
			pvpTalent.Write(_worldPacket);
	}
}

//Structs
public struct PvPTalent
{
	public ushort PvPTalentID;
	public byte Slot;

	public PvPTalent(WorldPacket data)
	{
		PvPTalentID = data.ReadUInt16();
		Slot = data.ReadUInt8();
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt16(PvPTalentID);
		data.WriteUInt8(Slot);
	}
}

struct GlyphBinding
{
	public GlyphBinding(uint spellId, ushort glyphId)
	{
		SpellID = spellId;
		GlyphID = glyphId;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellID);
		data.WriteUInt16(GlyphID);
	}

	readonly uint SpellID;
	readonly ushort GlyphID;
}