// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Cryptography;
using Framework.Cryptography.Ed25519;

namespace Game.Networking.Packets;

class EnterEncryptedMode : ServerPacket
{
	static readonly byte[] expandedPrivateKey;

	static readonly byte[] EnableEncryptionSeed =
	{
		0x90, 0x9C, 0xD0, 0x50, 0x5A, 0x2C, 0x14, 0xDD, 0x5C, 0x2C, 0xC0, 0x64, 0x14, 0xF3, 0xFE, 0xC9
	};

	static readonly byte[] EnableEncryptionContext =
	{
		0xA7, 0x1F, 0xB6, 0x9B, 0xC9, 0x7C, 0xDD, 0x96, 0xE9, 0xBB, 0xB8, 0x21, 0x39, 0x8D, 0x5A, 0xD4
	};

	static readonly byte[] EnterEncryptedModePrivateKey =
	{
		0x08, 0xBD, 0xC7, 0xA3, 0xCC, 0xC3, 0x4F, 0x3F, 0x6A, 0x0B, 0xFF, 0xCF, 0x31, 0xC1, 0xB6, 0x97, 0x69, 0x1E, 0x72, 0x9A, 0x0A, 0xAB, 0x2C, 0x77, 0xC3, 0x6F, 0x8A, 0xE7, 0x5A, 0x9A, 0xA7, 0xC9
	};

	readonly byte[] EncryptionKey;
	readonly bool Enabled;

	static EnterEncryptedMode()
	{
		expandedPrivateKey = Ed25519.ExpandedPrivateKeyFromSeed(EnterEncryptedModePrivateKey);
	}

	public EnterEncryptedMode(byte[] encryptionKey, bool enabled) : base(ServerOpcodes.EnterEncryptedMode)
	{
		EncryptionKey = encryptionKey;
		Enabled = enabled;
	}

	public override void Write()
	{
		HmacSha256 toSign = new(EncryptionKey);
		toSign.Process(BitConverter.GetBytes(Enabled), 1);
		toSign.Finish(EnableEncryptionSeed, 16);

		_worldPacket.WriteBytes(Ed25519.Sign(toSign.Digest, expandedPrivateKey, 0, EnableEncryptionContext));
		_worldPacket.WriteBit(Enabled);
		_worldPacket.FlushBits();
	}
}