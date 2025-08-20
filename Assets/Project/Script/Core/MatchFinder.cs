using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Enums;
using UnityEngine;
using System.Linq;

namespace Gazeus.DesafioMatch3.Core
{
    public class MatchFinder
    {
        /// <summary>
        /// Escaneia o tabuleiro em uma única direção (horizontal ou vertical) para encontrar sequências de 3 ou mais peças iguais.
        /// </summary>
        private List<MatchInfos> FindMatchesForAxis ( Board board, bool isHorizontal )
        {
            var foundMatches = new List<MatchInfos>();
            var rows = board.Height;
            var cols = board.Width;

            int primaryAxisLimit = isHorizontal ? rows : cols;
            int secondaryAxisLimit = isHorizontal ? cols : rows;

            for ( int i = 0; i < primaryAxisLimit; i++ )
            {
                for ( int j = 0; j < secondaryAxisLimit - 2; )
                {
                    Tile currentTile = isHorizontal ? board.Tiles [i] [j] : board.Tiles [j] [i];
                    if ( currentTile.Type == TileType.Empty )
                    {
                        j++;
                        continue;
                    }

                    var currentMatchSequence = new List<Vector2Int>();
                    for ( int k = j; k < secondaryAxisLimit; k++ )
                    {
                        Tile nextTile = isHorizontal ? board.Tiles [i] [k] : board.Tiles [k] [i];
                        if ( nextTile.Type == currentTile.Type )
                        {
                            var pos = isHorizontal ? new Vector2Int(k, i) : new Vector2Int(i, k);
                            currentMatchSequence.Add(pos);
                        } else
                        {
                            break;
                        }
                    }

                    if ( currentMatchSequence.Count >= 3 )
                    {
                        var direction = isHorizontal ? MatchDirection.Horizontal : MatchDirection.Vertical;
                        foundMatches.Add(new MatchInfos(currentMatchSequence, direction));
                    }

                    j += Mathf.Max(1, currentMatchSequence.Count);
                }
            }
            return foundMatches;
        }

        /// <summary>
        /// Analisa listas de matches para encontrar e agrupar todas os cruzamentos
        /// em matches complexos (L, T, +), garantindo que nenhuma peça seja contada duas vezes.
        /// </summary>
        private List<MatchInfos> ProcessComplexMatches ( List<MatchInfos> horizontal, List<MatchInfos> vertical )
        {
            var finalMatches = new List<MatchInfos>();
            var allSimpleMatches = horizontal.Concat(vertical).ToList();
            var consumedMatches = new HashSet<MatchInfos>();

            for ( int i = 0; i < allSimpleMatches.Count; i++ )
            {
                var currentMatch = allSimpleMatches [i];
                if ( consumedMatches.Contains(currentMatch) )
                {
                    continue;
                }

                var complexMatchTiles = new HashSet<Vector2Int>(currentMatch.MatchedTiles);
                var matchesInThisCombo = new List<MatchInfos> { currentMatch };

                bool newConnectionFound;
                do
                {
                    newConnectionFound = false;
                    for ( int j = 0; j < allSimpleMatches.Count; j++ )
                    {
                        var otherMatch = allSimpleMatches [j];

                        if ( matchesInThisCombo.Contains(otherMatch) || consumedMatches.Contains(otherMatch) )
                        {
                            continue;
                        }

                        if ( otherMatch.MatchedTiles.Any(tile => complexMatchTiles.Contains(tile)) )
                        {
                            complexMatchTiles.UnionWith(otherMatch.MatchedTiles);
                            matchesInThisCombo.Add(otherMatch);
                            newConnectionFound = true;
                        }
                    }
                } while ( newConnectionFound );


                if ( matchesInThisCombo.Count > 1 )
                {

                    finalMatches.Add(new MatchInfos(complexMatchTiles.ToList(), MatchDirection.Complex));

                    foreach ( var match in matchesInThisCombo )
                    {
                        consumedMatches.Add(match);
                    }
                }
            }

            finalMatches.AddRange(allSimpleMatches.Where(m => !consumedMatches.Contains(m)));

            return finalMatches;
        }

        public List<MatchInfos> FindCompleteMatches ( Board board )
        {
            var horizontalMatches = FindMatchesForAxis(board, isHorizontal: true);
            var verticalMatches = FindMatchesForAxis(board, isHorizontal: false);
            var processedMatches = ProcessComplexMatches(horizontalMatches, verticalMatches);
            return processedMatches;
        }
    }
}