// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Database;
using Framework.GameMath;
using Game.Common.DataStorage.Structs.A;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Artifact;
using Game.Common.Networking.Packets.BattleGround;
using Game.Common.Networking.Packets.GameObject;
using Game.Common.Networking.Packets.Misc;
using Game.Common.Server;

namespace Game.Common.Entities.GameObjects
{
    public class GameObject : WorldObject
    {
        protected GameObjectTemplate GoInfoProtected;
        protected GameObjectTemplateAddon GoTemplateAddonProtected;
        private readonly List<ObjectGuid> _uniqueUsers = new();
        private readonly Dictionary<uint, ObjectGuid> _chairListSlots = new();
        private readonly List<ObjectGuid> _skillupList = new();
        private GameObjectTypeBase _goTypeImpl;
        private uint _spellId;
        private uint _despawnDelay;
        private TimeSpan _despawnRespawnTime; // override respawn time after delayed despawn
        private ObjectGuid _lootStateUnitGuid; // GUID of the unit passed with SetLootState(LootState, Unit*)
        private long _restockTime;
        private long _cooldownTime; // used as internal reaction delay time store (not state change reaction).
                                    // For traps this: spell casting cooldown, for doors/buttons: reset time.


        private Player _ritualOwner; // used for GAMEOBJECT_TYPE_SUMMONING_RITUAL where GO is not summoned (no owner)
        private Quaternion _localRotation;
        private ushort _animKitId;
        private uint? _gossipMenuId;
        private Dictionary<ObjectGuid, PerPlayerState> _perPlayerState;
        private GameObjectState _prevGoState; // What state to set whenever resetting

        private ObjectGuid _linkedTrap;


        public GameObjectFieldData GameObjectFieldData { get; set; }
        public Position StationaryPosition { get; set; }

        public override ushort AIAnimKitId => _animKitId;

        public override ObjectGuid OwnerGUID => GameObjectFieldData.CreatedBy;

        public override uint Faction
        {
            get => GameObjectFieldData.FactionTemplate;
            set => SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.FactionTemplate), value);
        }

        public override float StationaryX => StationaryPosition.X;

        public override float StationaryY => StationaryPosition.Y;

        public override float StationaryZ => StationaryPosition.Z;

        public override float StationaryO => StationaryPosition.Orientation;

        public string AiName
        {
            get
            {
                var got = Global.ObjectMgr.GetGameObjectTemplate(Entry);

                if (got != null)
                    return got.AIName;

                return "";
            }
        }

        public GameObjectOverride GameObjectOverride
        {
            get
            {
                if (SpawnId != 0)
                {
                    var goOverride = Global.ObjectMgr.GetGameObjectOverride(SpawnId);

                    if (goOverride != null)
                        return goOverride;
                }

                return GoTemplateAddonProtected;
            }
        }

        public bool IsTransport
        {
            get
            {
                // If something is marked as a transport, don't transmit an out of range packet for it.
                var gInfo = Template;

                if (gInfo == null)
                    return false;

                return gInfo.type == GameObjectTypes.Transport || gInfo.type == GameObjectTypes.MapObjTransport;
            }
        }

        // is Dynamic transport = non-stop Transport
        public bool IsDynTransport
        {
            get
            {
                // If something is marked as a transport, don't transmit an out of range packet for it.
                var gInfo = Template;

                if (gInfo == null)
                    return false;

                return gInfo.type == GameObjectTypes.MapObjTransport || gInfo.type == GameObjectTypes.Transport;
            }
        }

        public bool IsDestructibleBuilding
        {
            get
            {
                var gInfo = Template;

                if (gInfo == null)
                    return false;

                return gInfo.type == GameObjectTypes.DestructibleBuilding;
            }
        }

        public GameObject LinkedTrap
        {
            get => ObjectAccessor.GetGameObject(this, _linkedTrap);
            set => _linkedTrap = value.GUID;
        }

        public uint WorldEffectID { get; set; }

        public uint GossipMenuId
        {
            get
            {
                if (_gossipMenuId.HasValue)
                    return _gossipMenuId.Value;

                return Template.GetGossipMenuId();
            }
            set { _gossipMenuId = value; }
        }

        public GameObjectTemplate Template => GoInfoProtected;

        public GameObjectTemplateAddon TemplateAddon => GoTemplateAddonProtected;

        public ulong SpawnId { get; private set; }

        public Quaternion LocalRotation => _localRotation;

        public long PackedLocalRotation { get; private set; }

        public uint SpellId
        {
            get => _spellId;
            set
            {
                IsSpawnedByDefault = false; // all summoned object is despawned after delay
                _spellId = value;
            }
        }

        public long RespawnTime { get; private set; }

        public long RespawnTimeEx
        {
            get
            {
                var now = GameTime.GetGameTime();

                if (RespawnTime > now)
                    return RespawnTime;
                else
                    return now;
            }
        }

        public bool IsSpawned => RespawnDelay == 0 ||
                                (RespawnTime > 0 && !IsSpawnedByDefault) ||
                                (RespawnTime == 0 && IsSpawnedByDefault);

        public bool IsSpawnedByDefault { get; private set; }

        public uint RespawnDelay { get; private set; }

        public GameObjectTypes GoType
        {
            get => (GameObjectTypes)(sbyte)GameObjectFieldData.TypeID;
            set => SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.TypeID), (sbyte)value);
        }

        public GameObject() : base(false)
        {
            ObjectTypeMask |= TypeMask.GameObject;
            ObjectTypeId = TypeId.GameObject;

            _updateFlag.Stationary = true;
            _updateFlag.Rotation = true;

            RespawnDelay = 300;
            _despawnDelay = 0;
            IsSpawnedByDefault = true;

            ResetLootMode(); // restore default loot mode
            StationaryPosition = new Position();

            GameObjectFieldData = new GameObjectFieldData();
        }

        public override void Dispose()
        {
            _ai = null;
            Model = null;

            base.Dispose();
        }
    }
}
