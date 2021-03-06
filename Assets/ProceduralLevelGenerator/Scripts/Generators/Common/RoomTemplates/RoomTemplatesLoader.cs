﻿using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ProceduralLevelGenerator.Scripts.Generators.Common.Utils;
using Assets.ProceduralLevelGenerator.Scripts.Utils;
using GeneralAlgorithms.Algorithms.Common;
using GeneralAlgorithms.DataStructures.Common;
using GeneralAlgorithms.DataStructures.Polygons;
using MapGeneration.Interfaces.Core.MapDescriptions;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.ProceduralLevelGenerator.Scripts.Generators.Common.RoomTemplates
{
    /// <summary>
    /// Class used to convert room templates to the representation used in the dungeon generator library.
    /// </summary>
    public static class RoomTemplatesLoader
    {
        /// <summary>
        /// Computes a polygon from its tiles.
        /// </summary>
        /// <param name="allPoints"></param>
        /// <returns></returns>
        public static GridPolygon GetPolygonFromTiles(HashSet<IntVector2> allPoints)
        {
            if (allPoints.Count == 0)
            {
                throw new ArgumentException("There must be at least one point");
            }

            var orderedDirections = new Dictionary<IntVector2, List<IntVector2>>
            {
                {IntVector2Helper.Top, new List<IntVector2> {IntVector2Helper.Left, IntVector2Helper.Top, IntVector2Helper.Right}},
                {IntVector2Helper.Right, new List<IntVector2> {IntVector2Helper.Top, IntVector2Helper.Right, IntVector2Helper.Bottom}},
                {IntVector2Helper.Bottom, new List<IntVector2> {IntVector2Helper.Right, IntVector2Helper.Bottom, IntVector2Helper.Left}},
                {IntVector2Helper.Left, new List<IntVector2> {IntVector2Helper.Bottom, IntVector2Helper.Left, IntVector2Helper.Top}}
            };

            var smallestX = allPoints.Min(x => x.X);
            var smallestXPoints = allPoints.Where(x => x.X == smallestX).ToList();
            var smallestXYPoint = smallestXPoints[smallestXPoints.MinBy(x => x.Y)];

            var startingPoint = smallestXYPoint;
            var startingDirection = IntVector2Helper.Top;

            var polygonPoints = new List<IntVector2>();
            var currentPoint = startingPoint + startingDirection;
            var firstPoint = currentPoint;
            var previousDirection = startingDirection;
            var first = true;

            if (!allPoints.Contains(currentPoint))
            {
                throw new ArgumentException("Invalid room shape.");
            }

            while (true)
            {
                var foundNeighbor = false;
                var currentDirection = new IntVector2();

                foreach (var directionVector in orderedDirections[previousDirection])
                {
                    var newPoint = currentPoint + directionVector;

                    if (allPoints.Contains(newPoint))
                    {
                        currentDirection = directionVector;
                        foundNeighbor = true;
                        break;
                    }
                }

                if (!foundNeighbor)
                    throw new ArgumentException("Invalid room shape.");

                if (currentDirection != previousDirection)
                {
                    polygonPoints.Add(currentPoint);
                }

                currentPoint += currentDirection;
                previousDirection = currentDirection;

                if (first)
                {
                    first = false;
                }
                else if (currentPoint == firstPoint)
                {
                    break;
                }
            }

            if (!IsClockwiseOriented(polygonPoints))
            {
                polygonPoints.Reverse();
            }

            return new GridPolygon(polygonPoints);
        }

        /// <summary>
        /// Computes a polygon from points on given tilemaps.
        /// </summary>
        /// <param name="tilemaps"></param>
        /// <returns></returns>
        public static GridPolygon GetPolygonFromTilemaps(ICollection<Tilemap> tilemaps)
        {
            var usedTiles = GetUsedTiles(RoomTemplateUtils.GetTilemapsForOutline(tilemaps));

            return GetPolygonFromTiles(usedTiles);
        }

        /// <summary>
        /// Computes a polygon from points on given tilemaps.
        /// </summary>
        /// <param name="roomTemplate"></param>
        /// <returns></returns>
        public static GridPolygon GetPolygonFromRoomTemplate(GameObject roomTemplate)
        {
            var tilemaps = RoomTemplateUtils.GetTilemaps(roomTemplate);
            var outline = RoomTemplateUtils.GetTilemapsForOutline(tilemaps);

            return GetPolygonFromTilemaps(outline);
        }

        /// <summary>
        /// Gets all tiles that are not null in given tilemaps.
        /// </summary>
        /// <param name="tilemaps"></param>
        /// <returns></returns>
        public static HashSet<IntVector2> GetUsedTiles(IEnumerable<Tilemap> tilemaps)
        {
            var usedTiles = new HashSet<IntVector2>();

            foreach (var tilemap in tilemaps)
            {
                foreach (var position in tilemap.cellBounds.allPositionsWithin)
                {
                    var tile = tilemap.GetTile(position);

                    if (tile == null)
                    {
                        continue;
                    }

                    usedTiles.Add(position.ToCustomIntVector2());
                }
            }

            return usedTiles;
        }

        /// <summary>
        ///     Computes a room room template from a given room template game object.
        /// </summary>
        /// <param name="roomTemplatePrefab"></param>
        /// <param name="allowedTransformations"></param>
        /// <returns></returns>
        public static IRoomTemplate GetRoomTemplate(GameObject roomTemplatePrefab, List<Transformation> allowedTransformations = null)
        {
            if (allowedTransformations == null)
            {
                allowedTransformations = new List<Transformation> {Transformation.Identity};
            }

            

            // TODO: weird to call PostProcessUtils
            var polygon = GetPolygonFromTilemaps(RoomTemplateUtils.GetTilemaps(roomTemplatePrefab));
            var doors = roomTemplatePrefab.GetComponent<Doors.Doors>();

            if (doors == null)
            {
                throw new GeneratorException($"Room template \"{roomTemplatePrefab.name}\" does not have any doors assigned.");
            }

            var roomTemplateComponent = roomTemplatePrefab.GetComponent<RoomTemplate>();
            var repeatMode = roomTemplateComponent?.RepeatMode ?? RepeatMode.AllowRepeat;
            var doorMode = doors.GetDoorMode();

            var roomDescription = new MapGeneration.Core.MapDescriptions.RoomTemplate(polygon, doorMode, allowedTransformations, repeatMode);

            return roomDescription;
        }

        private static bool IsClockwiseOriented(IList<IntVector2> points)
        {
            var previous = points[points.Count - 1];
            var sum = 0L;

            foreach (var point in points)
            {
                sum += (point.X - previous.X) * (long) (point.Y + previous.Y);
                previous = point;
            }

            return sum > 0;
        }
    }
}