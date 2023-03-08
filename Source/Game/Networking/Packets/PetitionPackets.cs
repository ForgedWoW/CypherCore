// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class QueryPetition : ClientPacket
{
	public ObjectGuid ItemGUID;
	public uint PetitionID;
	public QueryPetition(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetitionID = _worldPacket.ReadUInt32();
		ItemGUID = _worldPacket.ReadPackedGuid();
	}
}

public class QueryPetitionResponse : ServerPacket
{
	public uint PetitionID = 0;
	public bool Allow = false;
	public PetitionInfo Info;
	public QueryPetitionResponse() : base(ServerOpcodes.QueryPetitionResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(PetitionID);
		_worldPacket.WriteBit(Allow);
		_worldPacket.FlushBits();

		if (Allow)
			Info.Write(_worldPacket);
	}
}

public class PetitionShowList : ClientPacket
{
	public ObjectGuid PetitionUnit;
	public PetitionShowList(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetitionUnit = _worldPacket.ReadPackedGuid();
	}
}

public class ServerPetitionShowList : ServerPacket
{
	public ObjectGuid Unit;
	public uint Price = 0;
	public ServerPetitionShowList() : base(ServerOpcodes.PetitionShowList) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt32(Price);
	}
}

public class PetitionBuy : ClientPacket
{
	public ObjectGuid Unit;
	public string Title;
	public uint Unused910;
	public PetitionBuy(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var titleLen = _worldPacket.ReadBits<uint>(7);

		Unit = _worldPacket.ReadPackedGuid();
		Unused910 = _worldPacket.ReadUInt32();
		Title = _worldPacket.ReadString(titleLen);
	}
}

public class PetitionShowSignatures : ClientPacket
{
	public ObjectGuid Item;
	public PetitionShowSignatures(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Item = _worldPacket.ReadPackedGuid();
	}
}

public class ServerPetitionShowSignatures : ServerPacket
{
	public ObjectGuid Item;
	public ObjectGuid Owner;
	public ObjectGuid OwnerAccountID;
	public int PetitionID = 0;
	public List<PetitionSignature> Signatures;

	public ServerPetitionShowSignatures() : base(ServerOpcodes.PetitionShowSignatures)
	{
		Signatures = new List<PetitionSignature>();
	}

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Item);
		_worldPacket.WritePackedGuid(Owner);
		_worldPacket.WritePackedGuid(OwnerAccountID);
		_worldPacket.WriteInt32(PetitionID);

		_worldPacket.WriteInt32(Signatures.Count);

		foreach (var signature in Signatures)
		{
			_worldPacket.WritePackedGuid(signature.Signer);
			_worldPacket.WriteInt32(signature.Choice);
		}
	}

	public struct PetitionSignature
	{
		public ObjectGuid Signer;
		public int Choice;
	}
}

public class SignPetition : ClientPacket
{
	public ObjectGuid PetitionGUID;
	public byte Choice;
	public SignPetition(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetitionGUID = _worldPacket.ReadPackedGuid();
		Choice = _worldPacket.ReadUInt8();
	}
}

public class PetitionSignResults : ServerPacket
{
	public ObjectGuid Item;
	public ObjectGuid Player;
	public PetitionSigns Error = 0;
	public PetitionSignResults() : base(ServerOpcodes.PetitionSignResults) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Item);
		_worldPacket.WritePackedGuid(Player);

		_worldPacket.WriteBits(Error, 4);
		_worldPacket.FlushBits();
	}
}

public class PetitionAlreadySigned : ServerPacket
{
	public ObjectGuid SignerGUID;
	public PetitionAlreadySigned() : base(ServerOpcodes.PetitionAlreadySigned) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(SignerGUID);
	}
}

public class DeclinePetition : ClientPacket
{
	public ObjectGuid PetitionGUID;
	public DeclinePetition(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetitionGUID = _worldPacket.ReadPackedGuid();
	}
}

public class TurnInPetition : ClientPacket
{
	public ObjectGuid Item;
	public TurnInPetition(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Item = _worldPacket.ReadPackedGuid();
	}
}

public class TurnInPetitionResult : ServerPacket
{
	public PetitionTurns Result = 0; // PetitionError
	public TurnInPetitionResult() : base(ServerOpcodes.TurnInPetitionResult) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Result, 4);
		_worldPacket.FlushBits();
	}
}

public class OfferPetition : ClientPacket
{
	public ObjectGuid TargetPlayer;
	public ObjectGuid ItemGUID;
	public OfferPetition(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ItemGUID = _worldPacket.ReadPackedGuid();
		TargetPlayer = _worldPacket.ReadPackedGuid();
	}
}

public class OfferPetitionError : ServerPacket
{
	public ObjectGuid PlayerGUID;
	public OfferPetitionError() : base(ServerOpcodes.OfferPetitionError) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PlayerGUID);
	}
}

public class PetitionRenameGuild : ClientPacket
{
	public ObjectGuid PetitionGuid;
	public string NewGuildName;
	public PetitionRenameGuild(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetitionGuid = _worldPacket.ReadPackedGuid();

		_worldPacket.ResetBitPos();
		var nameLen = _worldPacket.ReadBits<uint>(7);

		NewGuildName = _worldPacket.ReadString(nameLen);
	}
}

public class PetitionRenameGuildResponse : ServerPacket
{
	public ObjectGuid PetitionGuid;
	public string NewGuildName;
	public PetitionRenameGuildResponse() : base(ServerOpcodes.PetitionRenameGuildResponse) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PetitionGuid);

		_worldPacket.WriteBits(NewGuildName.GetByteCount(), 7);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(NewGuildName);
	}
}

public class PetitionInfo
{
	public int PetitionID;
	public ObjectGuid Petitioner;
	public string Title;
	public string BodyText;
	public uint MinSignatures;
	public uint MaxSignatures;
	public int DeadLine;
	public int IssueDate;
	public int AllowedGuildID;
	public int AllowedClasses;
	public int AllowedRaces;
	public short AllowedGender;
	public int AllowedMinLevel;
	public int AllowedMaxLevel;
	public int NumChoices;
	public int StaticType;
	public uint Muid = 0;
	public StringArray Choicetext = new(10);

	public void Write(WorldPacket data)
	{
		data.WriteInt32(PetitionID);
		data.WritePackedGuid(Petitioner);

		data.WriteUInt32(MinSignatures);
		data.WriteUInt32(MaxSignatures);
		data.WriteInt32(DeadLine);
		data.WriteInt32(IssueDate);
		data.WriteInt32(AllowedGuildID);
		data.WriteInt32(AllowedClasses);
		data.WriteInt32(AllowedRaces);
		data.WriteInt16(AllowedGender);
		data.WriteInt32(AllowedMinLevel);
		data.WriteInt32(AllowedMaxLevel);
		data.WriteInt32(NumChoices);
		data.WriteInt32(StaticType);
		data.WriteUInt32(Muid);

		data.WriteBits(Title.GetByteCount(), 7);
		data.WriteBits(BodyText.GetByteCount(), 12);

		for (byte i = 0; i < Choicetext.Length; i++)
			data.WriteBits(Choicetext[i].GetByteCount(), 6);

		data.FlushBits();

		for (byte i = 0; i < Choicetext.Length; i++)
			data.WriteString(Choicetext[i]);

		data.WriteString(Title);
		data.WriteString(BodyText);
	}
}