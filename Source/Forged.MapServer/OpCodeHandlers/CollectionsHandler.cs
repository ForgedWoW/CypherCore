// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Collection;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.OpCodeHandlers;

public class CollectionsHandler : IWorldSessionHandler
{
    private readonly CollectionMgr _collectionMgr;

    public CollectionsHandler(CollectionMgr collectionMgr)
    {
		_collectionMgr = collectionMgr;
    }

	[WorldPacketHandler(ClientOpcodes.CollectionItemSetFavorite)]
	void HandleCollectionItemSetFavorite(CollectionItemSetFavorite collectionItemSetFavorite)
	{
		switch (collectionItemSetFavorite.Type)
		{
			case CollectionType.Toybox:
                _collectionMgr.ToySetFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);

				break;
			case CollectionType.Appearance:
			{
				var pair = _collectionMgr.HasItemAppearance(collectionItemSetFavorite.Id);

				if (!pair.Item1 || pair.Item2)
					return;

                _collectionMgr.SetAppearanceIsFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);

				break;
			}
			case CollectionType.TransmogSet:
				break;
			
		}
	}
}