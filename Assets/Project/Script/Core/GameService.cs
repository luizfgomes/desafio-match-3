using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Enums;
using UnityEngine;
using System.Linq;

namespace Gazeus.DesafioMatch3.Core
{
    public class GameService
    {
        private Board _currentBoard;
        private readonly MatchFinder _matchFinder = new MatchFinder();
        private List<int> _tilesTypes;
        private int _tileCount;

        public bool IsValidMovement ( int fromX, int fromY, int toX, int toY )
        {
            Board simulatedBoard = _currentBoard.Copy();

            (simulatedBoard.Tiles [toY] [toX], simulatedBoard.Tiles [fromY] [fromX]) =
                (simulatedBoard.Tiles [fromY] [fromX], simulatedBoard.Tiles [toY] [toX]);

            return _matchFinder.FindCompleteMatches(simulatedBoard).Count > 0;
        }

        public List<List<Tile>> StartGame ( int boardWidth, int boardHeight )
        {
            var tileTypes = new List<TileType> { TileType.Blue, TileType.Green, TileType.Red, TileType.Yellow };
            _currentBoard = CreateBoard(boardWidth, boardHeight, tileTypes);

            return _currentBoard.Tiles;
        }

        public List<BoardSequence> SwapTile ( int fromX, int fromY, int toX, int toY )
        {
            List<BoardSequence> boardSequences = new();
            (_currentBoard.Tiles [toY] [toX], _currentBoard.Tiles [fromY] [fromX]) =
                (_currentBoard.Tiles [fromY] [fromX], _currentBoard.Tiles [toY] [toX]);

            List<MatchInfos> allMatches = _matchFinder.FindCompleteMatches(_currentBoard);

            while ( allMatches.Count > 0 )
            {

                SpecialTileType specialTypeToCreate = SpecialTileType.None;
                Vector2Int specialTileCreationPos = new Vector2Int(-1, -1);

                while ( allMatches.Count > 0 )
                {
                    if ( match.Count >= 4 )
                    {
                        specialTypeToCreate = (match.Direction == MatchDirection.Horizontal) ? SpecialTileType.LineClearHorizontal : SpecialTileType.LineClearVertical;

                        Vector2Int movedPiecePos = new Vector2Int(toX, toY);
                        if ( match.MatchedTiles.Contains(movedPiecePos) )
                        {
                            specialTileCreationPos = movedPiecePos;
                        } else
                        {
                            specialTileCreationPos = match.MatchedTiles [0];
                        }
                        break;
                    }
                }

                //New cleaning the matched tiles
                HashSet<Vector2Int> positionsToProcess = new HashSet<Vector2Int>();
                foreach ( var match in allMatches )
                {
                    foreach ( var pos in match.MatchedTiles )
                    {
                        positionsToProcess.Add(pos);
                    }
                }

                List<Vector2Int> finalMatchedPositions = new List<Vector2Int>();
                List<TransformedTileInfo> transformedTiles = new List<TransformedTileInfo>();

                foreach ( Vector2Int pos in positionsToProcess )
                {
                    if ( pos == specialTileCreationPos )
                    {
                        newBoard [pos.y] [pos.x].Special = specialTypeToCreate;
                        transformedTiles.Add(new TransformedTileInfo
                        {
                            Position = pos,
                            NewSpecialType = specialTypeToCreate
                        });
                    } else
                    {
                        newBoard [pos.y] [pos.x].Type = -1;
                        newBoard [pos.y] [pos.x].Special = SpecialTileType.None;
                        finalMatchedPositions.Add(pos);
                    }
                }

                // New dropping the tiles
                List<MovedTileInfo> movedTilesList = new List<MovedTileInfo>();
                var affectedColumns = positionsToProcess.Select(p => p.x).Distinct();

                foreach ( int x in affectedColumns )
                {
                    int writeY = newBoard.Count - 1;
                    for ( int readY = newBoard.Count - 1; readY >= 0; readY-- )
                    {
                        Tile tile = newBoard [readY] [x];
                        if ( tile.Type != -1 )
                        {
                            if ( writeY != readY )
                            {
                                movedTilesList.Add(new MovedTileInfo
                                {
                                    From = new Vector2Int(x, readY),
                                    To = new Vector2Int(x, writeY)
                                });
                                newBoard [writeY] [x] = tile;
                                newBoard [readY] [x] = new Tile { Id = -1, Type = -1 };
                            }
                            writeY--;
                        }
                    }
                }

                List<AddedTileInfo> addedTiles = new();
                for ( int y = 0; y < newBoard.Count; y++ )
                {
                    for ( int x = 0; x < newBoard [y].Count; x++ )
                    {
                        if ( newBoard [y] [x].Type == -1 )
                        {
                            int tileType = Random.Range(0, _tilesTypes.Count);
                            Tile tile = new Tile
                            {
                                Id = _tileCount++,
                                Type = _tilesTypes [tileType],
                                Special = SpecialTileType.None
                            };
                            newBoard [y] [x] = tile;
                            addedTiles.Add(new AddedTileInfo { Position = new Vector2Int(x, y), Type = tile.Type });
                        }
                    }
                }

                BoardSequence sequence = new()
                {
                    MatchedPosition = finalMatchedPositions,
                    MovedTiles = movedTilesList,
                    AddedTiles = addedTiles,
                    TransformedTiles = transformedTiles
                };
                boardSequences.Add(sequence);
                allMatches = FindCompleteMatches(newBoard);
            }

            _boardTiles = newBoard;

            return boardSequences;
        }

        private Board CreateBoard ( int width, int height, List<TileType> tileTypes )
        {
            Board board = new Board(width, height);
            _tileCount = 0;

            for ( int y = 0; y < height; y++ )
            {
                for ( int x = 0; x < width; x++ )
                {
                    List<TileType> noMatchTypes = new List<TileType>(tileTypes);

                    if ( x > 1 &&
                        board.Tiles [y] [x - 1].Type == board.Tiles [y] [x - 2].Type )
                    {
                        noMatchTypes.Remove(board.Tiles [y] [x - 1].Type);
                    }

                    if ( y > 1 &&
                        board.Tiles [y - 1] [x].Type == board.Tiles [y - 2] [x].Type )
                    {
                        noMatchTypes.Remove(board.Tiles [y - 1] [x].Type);
                    }

                    board.Tiles [y] [x].Id = _tileCount++;
                    board.Tiles [y] [x].Type = noMatchTypes [Random.Range(0, noMatchTypes.Count)];
                }
            }

            return board;
        }
    }
}
