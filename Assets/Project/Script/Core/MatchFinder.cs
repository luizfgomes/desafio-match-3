using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Enums;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Core
{
    public class MatchFinder
    {
        private enum ScanDirection
        {
            Horizontal,
            Vertical
        }

        public List<MatchInfos> FindCompleteMatches ( Board board )
        {
            var allMatches = new List<MatchInfos>();

            allMatches.AddRange(FindMatchesForAxis(board, ScanDirection.Horizontal));
            allMatches.AddRange(FindMatchesForAxis(board, ScanDirection.Vertical));

            return allMatches;
        }

        private IEnumerable<MatchInfos> FindMatchesForAxis ( Board board, ScanDirection direction )
        {
            bool isHorizontal = direction == ScanDirection.Horizontal;

            int outerLimit = isHorizontal ? board.Height : board.Width;
            int innerLimit = isHorizontal ? board.Width : board.Height;

            for ( int outer = 0; outer < outerLimit; outer++ )
            {
                for ( int inner = 0; inner < innerLimit - 2; )
                {
                    Tile currentTile = isHorizontal ? board.Tiles [outer] [inner] : board.Tiles [inner] [outer];

                    if ( currentTile.Type == TileType.Empty )
                    {
                        inner++;
                        continue;
                    }

                    var currentMatch = new List<Vector2Int>();

                    for ( int i = inner; i < innerLimit; i++ )
                    {
                        Tile nextTile = isHorizontal ? board.Tiles [outer] [i] : board.Tiles [i] [outer];
                        if ( nextTile.Type == currentTile.Type )
                        {
                            Vector2Int position = isHorizontal ? new Vector2Int(i, outer) : new Vector2Int(outer, i);
                            currentMatch.Add(position);
                        } else
                        {
                            break;
                        }
                    }

                    if ( currentMatch.Count >= 3 )
                    {
                        MatchDirection matchDirection = isHorizontal ? MatchDirection.Horizontal : MatchDirection.Vertical;
                        yield return new MatchInfos(currentMatch, matchDirection);
                    }

                    inner += currentMatch.Count > 0 ? currentMatch.Count : 1;
                }
            }
        }
    }
}
