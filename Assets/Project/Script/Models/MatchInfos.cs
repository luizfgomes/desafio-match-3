using System.Collections.Generic;
using System.Linq;
using Gazeus.DesafioMatch3.Enums;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Models
{
    public class MatchInfos
    {
        public List<Vector2Int> MatchedTiles { get; }
        public MatchDirection Direction { get; }

        public MatchInfos ( List<Vector2Int> matchedTiles, MatchDirection direction )
        {
            MatchedTiles = matchedTiles;
            Direction = direction;
        }

        public override bool Equals ( object obj )
        {
            if ( obj is not MatchInfos other )
            {
                return false;
            }

            if ( MatchedTiles.Count != other.MatchedTiles.Count )
            {
                return false;
            }

            var myTiles = new HashSet<Vector2Int>(MatchedTiles);
            return myTiles.SetEquals(other.MatchedTiles);
        }

        public override int GetHashCode ()
        {
            int hash = 19;
            foreach ( var tile in MatchedTiles.OrderBy(t => t.x).ThenBy(t => t.y) )
            {
                hash = hash * 31 + tile.GetHashCode();
                Debug.Log("O hash é:" + hash);
            }
            return hash;
        }
    }
}