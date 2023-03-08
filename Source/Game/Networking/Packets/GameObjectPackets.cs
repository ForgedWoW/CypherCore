// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class GameObjUse : ClientPacket
{
	public ObjectGuid Guid;
	public bool IsSoftInteract;
	public GameObjUse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		IsSoftInteract = _worldPacket.HasBit();
	}
}

public class GameObjReportUse : ClientPacket
{
	public ObjectGuid Guid;
	public bool IsSoftInteract;
	public GameObjReportUse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		IsSoftInteract = _worldPacket.HasBit();
	}
}

class GameObjectDespawn : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public GameObjectDespawn() : base(ServerOpcodes.GameObjectDespawn) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
	}
}

class PageTextPkt : ServerPacket
{
	public ObjectGuid GameObjectGUID;
	public PageTextPkt() : base(ServerOpcodes.PageText) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GameObjectGUID);
	}
}

class GameObjectActivateAnimKit : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public int AnimKitID;
	public bool Maintain;
	public GameObjectActivateAnimKit() : base(ServerOpcodes.GameObjectActivateAnimKit, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
		_worldPacket.WriteInt32(AnimKitID);
		_worldPacket.WriteBit(Maintain);
		_worldPacket.FlushBits();
	}
}

class DestructibleBuildingDamage : ServerPacket
{
	public ObjectGuid Target;
	public ObjectGuid Caster;
	public ObjectGuid Owner;
	public int Damage;
	public uint SpellID;
	public DestructibleBuildingDamage() : base(ServerOpcodes.DestructibleBuildingDamage, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(Owner);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteInt32(Damage);
		_worldPacket.WriteUInt32(SpellID);
	}
}

class FishNotHooked : ServerPacket
{
	public FishNotHooked() : base(ServerOpcodes.FishNotHooked) { }

	public override void Write() { }
}

class FishEscaped : ServerPacket
{
	public FishEscaped() : base(ServerOpcodes.FishEscaped) { }

	public override void Write() { }
}

class GameObjectCustomAnim : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public uint CustomAnim;
	public bool PlayAsDespawn;
	public GameObjectCustomAnim() : base(ServerOpcodes.GameObjectCustomAnim, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
		_worldPacket.WriteUInt32(CustomAnim);
		_worldPacket.WriteBit(PlayAsDespawn);
		_worldPacket.FlushBits();
	}
}

class GameObjectPlaySpellVisual : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public ObjectGuid ActivatorGUID;
	public uint SpellVisualID;
	public GameObjectPlaySpellVisual() : base(ServerOpcodes.GameObjectPlaySpellVisual) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
		_worldPacket.WritePackedGuid(ActivatorGUID);
		_worldPacket.WriteUInt32(SpellVisualID);
	}
}

class GameObjectSetStateLocal : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public byte State;
	public GameObjectSetStateLocal() : base(ServerOpcodes.GameObjectSetStateLocal, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
		_worldPacket.WriteUInt8(State);
	}
}

class GameObjectInteraction : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public PlayerInteractionType InteractionType;

	public GameObjectInteraction() : base(ServerOpcodes.GameObjectInteraction) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
		_worldPacket.WriteInt32((int)InteractionType);
	}
}

class GameObjectCloseInteraction : ServerPacket
{
	public PlayerInteractionType InteractionType;

	public GameObjectCloseInteraction() : base(ServerOpcodes.GameObjectCloseInteraction) { }

	public override void Write()
	{
		_worldPacket.WriteInt32((int)InteractionType);
	}
}