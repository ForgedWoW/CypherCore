// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.T;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Algorithms;
using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Entities.Taxis;

public class TaxiPathGraph
{
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    private readonly ConditionManager _conditionManager;
    private readonly GameObjectManager _objectManager;
    private readonly List<TaxiNodesRecord> _nodesByVertex = new();
    private readonly Dictionary<uint, uint> _verticesByNode = new();
    private EdgeWeightedDigraph _graph;

    public TaxiPathGraph(CliDB cliDB, DB2Manager db2Manager, ConditionManager conditionManager, GameObjectManager objectManager)
    {
        _cliDB = cliDB;
        _db2Manager = db2Manager;
        _conditionManager = conditionManager;
        _objectManager = objectManager;
    }

    public void Initialize()
    {
        if (_graph != null)
            return;

        List<Tuple<Tuple<uint, uint>, uint>> edges = new();

        // Initialize here
        foreach (var path in _cliDB.TaxiPathStorage.Values)
        {
            var from = _cliDB.TaxiNodesStorage.LookupByKey(path.FromTaxiNode);
            var to = _cliDB.TaxiNodesStorage.LookupByKey(path.ToTaxiNode);

            if (from != null && to != null && from.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde) && to.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde))
                AddVerticeAndEdgeFromNodeInfo(from, to, path.Id, edges);
        }

        // create graph
        _graph = new EdgeWeightedDigraph(_nodesByVertex.Count);

        for (var j = 0; j < edges.Count; ++j)
            _graph.AddEdge(new DirectedEdge(edges[j].Item1.Item1, edges[j].Item1.Item2, edges[j].Item2));
    }

    public int GetCompleteNodeRoute(TaxiNodesRecord from, TaxiNodesRecord to, Player player, List<uint> shortestPath)
    {
        /*
            Information about node algorithm from client
            Since client does not give information about *ALL* nodes you have to pass by when going from sourceNodeID to destinationNodeID, we need to use Dijkstra algorithm.
            Examining several paths I discovered the following algorithm:
            * If destinationNodeID has is the next destination, connected directly to sourceNodeID, then, client just pick up this route regardless of distance
            * else we use dijkstra to find the shortest path.
            * When early landing is requested, according to behavior on retail, you can never end in a node you did not discovered before
        */

        // Find if we have a direct path
        _objectManager.GetTaxiPath(from.Id, to.Id, out var pathId, out _);

        if (pathId != 0)
        {
            shortestPath.Add(from.Id);
            shortestPath.Add(to.Id);
        }
        else
        {
            shortestPath.Clear();
            // We want to use Dijkstra on this graph
            DijkstraShortestPath g = new(_graph, (int)GetVertexIDFromNodeID(from));
            var path = g.PathTo((int)GetVertexIDFromNodeID(to));
            // found a path to the goal
            shortestPath.Add(from.Id);

            foreach (var edge in path)
            {
                //todo  test me No clue about this....
                var dest = _nodesByVertex[(int)edge.To];
                var requireFlag = (player.Team == TeamFaction.Alliance) ? TaxiNodeFlags.Alliance : TaxiNodeFlags.Horde;

                if (!dest.Flags.HasAnyFlag(requireFlag))
                    continue;

                var condition = _cliDB.PlayerConditionStorage.LookupByKey(dest.ConditionID);

                if (condition != null)
                    if (!_conditionManager.IsPlayerMeetingCondition(player, condition))
                        continue;

                shortestPath.Add(GetNodeIDFromVertexID(edge.To));
            }
        }

        return shortestPath.Count;
    }

    //todo test me
    public void GetReachableNodesMask(TaxiNodesRecord from, byte[] mask)
    {
        _ = new DepthFirstSearch(_graph,
                                 GetVertexIDFromNodeID(from),
                                 vertex =>
                                 {
                                     var taxiNode = _cliDB.TaxiNodesStorage.LookupByKey(GetNodeIDFromVertexID(vertex));

                                     if (taxiNode != null)
                                         mask[(taxiNode.Id - 1) / 8] |= (byte)(1 << (int)((taxiNode.Id - 1) % 8));
                                 });
    }

    private void GetTaxiMapPosition(Vector3 position, int mapId, out Vector2 uiMapPosition, out int uiMapId)
    {
        if (!_db2Manager.GetUiMapPosition(position.X, position.Y, position.Z, mapId, 0, 0, 0, UiMapSystem.Adventure, false, out uiMapId, out uiMapPosition))
            _db2Manager.GetUiMapPosition(position.X, position.Y, position.Z, mapId, 0, 0, 0, UiMapSystem.Taxi, false, out uiMapId, out uiMapPosition);
    }

    private uint CreateVertexFromFromNodeInfoIfNeeded(TaxiNodesRecord node)
    {
        if (!_verticesByNode.ContainsKey(node.Id))
        {
            _verticesByNode.Add(node.Id, (uint)_nodesByVertex.Count);
            _nodesByVertex.Add(node);
        }

        return _verticesByNode[node.Id];
    }

    private void AddVerticeAndEdgeFromNodeInfo(TaxiNodesRecord from, TaxiNodesRecord to, uint pathId, List<Tuple<Tuple<uint, uint>, uint>> edges)
    {
        if (from.Id != to.Id)
        {
            var fromVertexId = CreateVertexFromFromNodeInfoIfNeeded(from);
            var toVertexId = CreateVertexFromFromNodeInfoIfNeeded(to);

            var totalDist = 0.0f;
            var nodes = _cliDB.TaxiPathNodesByPath[pathId];

            if (nodes.Length < 2)
            {
                edges.Add(Tuple.Create(Tuple.Create(fromVertexId, toVertexId), 0xFFFFu));

                return;
            }

            var last = nodes.Length;
            var first = 0;

            if (nodes.Length > 2)
            {
                --last;
                ++first;
            }

            for (var i = first + 1; i < last; ++i)
            {
                if (nodes[i - 1].Flags.HasAnyFlag(TaxiPathNodeFlags.Teleport))
                    continue;


                GetTaxiMapPosition(nodes[i - 1].Loc, nodes[i - 1].ContinentID, out var pos1, out var uiMap1);
                GetTaxiMapPosition(nodes[i].Loc, nodes[i].ContinentID, out var pos2, out var uiMap2);

                if (uiMap1 != uiMap2)
                    continue;

                totalDist += (float)Math.Sqrt((float)Math.Pow(pos2.X - pos1.X, 2) + (float)Math.Pow(pos2.Y - pos1.Y, 2));
            }

            var dist = (uint)(totalDist * 32767.0f);

            if (dist > 0xFFFF)
                dist = 0xFFFF;

            edges.Add(Tuple.Create(Tuple.Create(fromVertexId, toVertexId), dist));
        }
    }

    private uint GetVertexIDFromNodeID(TaxiNodesRecord node)
    {
        return _verticesByNode.ContainsKey(node.Id) ? _verticesByNode[node.Id] : uint.MaxValue;
    }

    private uint GetNodeIDFromVertexID(uint vertexID)
    {
        if (vertexID < _nodesByVertex.Count)
            return _nodesByVertex[(int)vertexID].Id;

        return uint.MaxValue;
    }
}