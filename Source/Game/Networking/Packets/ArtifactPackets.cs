// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class ArtifactAddPower : ClientPacket
{
	public ObjectGuid ArtifactGUID;
	public ObjectGuid ForgeGUID;
	public Array<ArtifactPowerChoice> PowerChoices = new(1);
	public ArtifactAddPower(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ArtifactGUID = _worldPacket.ReadPackedGuid();
		ForgeGUID = _worldPacket.ReadPackedGuid();

		var powerCount = _worldPacket.ReadUInt32();

		for (var i = 0; i < powerCount; ++i)
		{
			ArtifactPowerChoice artifactPowerChoice;
			artifactPowerChoice.ArtifactPowerID = _worldPacket.ReadUInt32();
			artifactPowerChoice.Rank = _worldPacket.ReadUInt8();
			PowerChoices[i] = artifactPowerChoice;
		}
	}

	public struct ArtifactPowerChoice
	{
		public uint ArtifactPowerID;
		public byte Rank;
	}
}

class ArtifactSetAppearance : ClientPacket
{
	public ObjectGuid ArtifactGUID;
	public ObjectGuid ForgeGUID;
	public int ArtifactAppearanceID;
	public ArtifactSetAppearance(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ArtifactGUID = _worldPacket.ReadPackedGuid();
		ForgeGUID = _worldPacket.ReadPackedGuid();
		ArtifactAppearanceID = _worldPacket.ReadInt32();
	}
}

class ConfirmArtifactRespec : ClientPacket
{
	public ObjectGuid ArtifactGUID;
	public ObjectGuid NpcGUID;
	public ConfirmArtifactRespec(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ArtifactGUID = _worldPacket.ReadPackedGuid();
		NpcGUID = _worldPacket.ReadPackedGuid();
	}
}

class OpenArtifactForge : ServerPacket
{
	public ObjectGuid ArtifactGUID;
	public ObjectGuid ForgeGUID;
	public OpenArtifactForge() : base(ServerOpcodes.OpenArtifactForge) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ArtifactGUID);
		_worldPacket.WritePackedGuid(ForgeGUID);
	}
}

class ArtifactRespecPrompt : ServerPacket
{
	public ObjectGuid ArtifactGUID;
	public ObjectGuid NpcGUID;
	public ArtifactRespecPrompt() : base(ServerOpcodes.ArtifactRespecPrompt) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ArtifactGUID);
		_worldPacket.WritePackedGuid(NpcGUID);
	}
}

class ArtifactXpGain : ServerPacket
{
	public ObjectGuid ArtifactGUID;
	public ulong Amount;
	public ArtifactXpGain() : base(ServerOpcodes.ArtifactXpGain) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ArtifactGUID);
		_worldPacket.WriteUInt64(Amount);
	}
}