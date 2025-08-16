using System.Collections.Generic;
using Gazeus.DesafioMatch3.Enums;
using UnityEngine;

// Holds all data from a match (size, direction, tiles), so the game logic can check if it needs to create a special tile.
namespace Gazeus.DesafioMatch3.Models
{
    public class MatchInfos
    {
        public List<Vector2Int> MatchedTiles { get;  }
        public MatchDirection Direction { get; }
        public int Count => MatchedTiles.Count;

        public MatchInfos(List<Vector2Int> matchedTiles, MatchDirection direction )
        {
            MatchedTiles = matchedTiles;
            Direction = direction;
        }
    }
}
