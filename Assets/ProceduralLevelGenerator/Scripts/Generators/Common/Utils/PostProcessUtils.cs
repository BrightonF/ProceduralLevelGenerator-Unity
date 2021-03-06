﻿using System.Collections.Generic;
using System.Linq;
using Assets.ProceduralLevelGenerator.Scripts.Generators.Common.Rooms;
using Assets.ProceduralLevelGenerator.Scripts.Generators.Common.RoomTemplates;
using Assets.ProceduralLevelGenerator.Scripts.Generators.Common.RoomTemplates.TilemapLayers;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.ProceduralLevelGenerator.Scripts.Generators.Common.Utils
{
    public static class PostProcessUtils
    {
        public static void CenterGrid(GeneratedLevel level)
        {
            var tilemaps = level.GetSharedTilemaps();
            tilemaps[0].CompressBounds();

            var offset = tilemaps[0].cellBounds.center;

            foreach (Transform transform in level.RootGameObject.transform)
            {
                transform.position -= offset;
            }
        }

        public static void InitializeSharedTilemaps(GeneratedLevel level, ITilemapLayersHandler tilemapLayersHandler)
        {
            // Initialize GameObject that will hold tilemaps
            var tilemapsRoot = new GameObject(GeneratorConstants.TilemapsRootName);
            tilemapsRoot.transform.parent = level.RootGameObject.transform;

            // Create individual tilemaps
            tilemapLayersHandler.InitializeTilemaps(tilemapsRoot);
        }

        public static void CopyTilesToSharedTilemaps(GeneratedLevel level)
        {
            foreach (var roomInstance in level.GetRoomInstances().OrderBy(x => x.IsCorridor))
            {
                CopyTilesToSharedTilemaps(level, roomInstance);
            }
        }

        public static void CopyTilesToSharedTilemaps(GeneratedLevel level, RoomInstance roomInstance)
        {
            var destinationTilemaps = level.GetSharedTilemaps();
            var sourceTilemaps = RoomTemplateUtils.GetTilemaps(roomInstance.RoomTemplateInstance);

            CopyTiles(sourceTilemaps, destinationTilemaps, roomInstance.Position);
        }

        public static void CopyTiles(List<Tilemap> sourceTilemaps, List<Tilemap> destinationTilemaps, Vector3Int offset)
        {
            sourceTilemaps = RoomTemplateUtils.GetTilemapsForCopying(sourceTilemaps);

            DeleteNonNullTiles(sourceTilemaps, destinationTilemaps, offset);

            foreach (var sourceTilemap in sourceTilemaps)
            {
                var destinationTilemap = destinationTilemaps.FirstOrDefault(x => x.name == sourceTilemap.name);

                if (destinationTilemap == null)
                {
                    continue;
                }

                foreach (var tilemapPosition in sourceTilemap.cellBounds.allPositionsWithin)
                {
                    var tile = sourceTilemap.GetTile(tilemapPosition);

                    if (tile != null)
                    {
                        destinationTilemap.SetTile(tilemapPosition + offset, tile);
                    }
                }
            }
        }

        /// <summary>
        ///     Finds all non null tiles in a given room and then takes these positions and deletes
        ///     all such tiles on all tilemaps of the dungeon. The reason for this is that we want to
        ///     replace all existing tiles with new tiles from the room.
        /// </summary>
        /// <param name="sourceTilemaps"></param>
        /// <param name="offset"></param>
        /// <param name="destinationTilemaps"></param>
        private static void DeleteNonNullTiles(List<Tilemap> sourceTilemaps, List<Tilemap> destinationTilemaps, Vector3Int offset)
        {
            var tilesToRemove = new HashSet<Vector3Int>();

            // Find non-null tiles across all source tilemaps
            foreach (var sourceTilemap in sourceTilemaps)
            {
                foreach (var tilemapPosition in sourceTilemap.cellBounds.allPositionsWithin)
                {
                    var tile = sourceTilemap.GetTile(tilemapPosition);

                    if (tile != null)
                    {
                        tilesToRemove.Add(tilemapPosition);
                    }
                }
            }

            // Delete all found tiles across all destination tilemaps
            for (var i = 0; i < sourceTilemaps.Count; i++)
            {
                var destinationTilemap = destinationTilemaps[i];

                foreach (var tilemapPosition in tilesToRemove)
                {
                    destinationTilemap.SetTile(tilemapPosition + offset, null);
                }
            }
        }

        public static void DisableRoomTemplatesRenderers(GeneratedLevel level)
        {
            foreach (var roomInstance in level.GetRoomInstances())
            {
                var roomTemplateInstance = roomInstance.RoomTemplateInstance;
                var tilemaps = RoomTemplateUtils.GetTilemaps(roomTemplateInstance);

                foreach (var tilemap in tilemaps)
                {
                    var tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
                    Destroy(tilemapRenderer);
                }
            }
        }

        public static void DisableRoomTemplatesColliders(GeneratedLevel level)
        {
            foreach (var roomInstance in level.GetRoomInstances())
            {
                var roomTemplateInstance = roomInstance.RoomTemplateInstance;
                var tilemaps = RoomTemplateUtils.GetTilemaps(roomTemplateInstance);

                foreach (var tilemap in tilemaps)
                {
                    var compositeCollider = tilemap.GetComponent<CompositeCollider2D>();

                    if (compositeCollider != null && !compositeCollider.isTrigger)
                    {
                        compositeCollider.enabled = false;
                    }
                }
            }
        }

        // TODO: where to put this?
        public static void Destroy(Object gameObject)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(gameObject);
            }
            else
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}