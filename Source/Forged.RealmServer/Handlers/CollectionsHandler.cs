// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.RealmServer;

public class CollectionsHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public CollectionsHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.CollectionItemSetFavorite)]
	void HandleCollectionItemSetFavorite(CollectionItemSetFavorite collectionItemSetFavorite)
	{
		switch (collectionItemSetFavorite.Type)
		{
			case CollectionType.Toybox:
                _session.CollectionMgr.ToySetFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);

				break;
			case CollectionType.Appearance:
			{
				var pair = _session.CollectionMgr.HasItemAppearance(collectionItemSetFavorite.Id);

				if (!pair.Item1 || pair.Item2)
					return;

                _session.CollectionMgr.SetAppearanceIsFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);

				break;
			}
			case CollectionType.TransmogSet:
				break;
			default:
				break;
		}
	}
}