using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Enums;
using UnityEngine;
using System.Linq;

namespace Gazeus.DesafioMatch3.Core
{
    public class GameService
    {
        private List<List<Tile>> _boardTiles;
        private List<int> _tilesTypes;
        private int _tileCount;

        public bool IsValidMovement(int fromX, int fromY, int toX, int toY)
        {
            List<List<Tile>> newBoard = CopyBoard(_boardTiles);

            (newBoard[toY][toX], newBoard[fromY][fromX]) = (newBoard[fromY][fromX], newBoard[toY][toX]);

            for (int y = 0; y < newBoard.Count; y++)
            {
                for (int x = 0; x < newBoard[y].Count; x++)
                {
                    if (x > 1 &&
                        newBoard[y][x].Type == newBoard[y][x - 1].Type &&
                        newBoard[y][x - 1].Type == newBoard[y][x - 2].Type)
                    {
                        return true;
                    }

                    if (y > 1 &&
                        newBoard[y][x].Type == newBoard[y - 1][x].Type &&
                        newBoard[y - 1][x].Type == newBoard[y - 2][x].Type)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<List<Tile>> StartGame(int boardWidth, int boardHeight)
        {
            _tilesTypes = new List<int> { 0, 1, 2, 3 };
            _boardTiles = CreateBoard(boardWidth, boardHeight, _tilesTypes);

            return _boardTiles;
        }

        public List<BoardSequence> SwapTile(int fromX, int fromY, int toX, int toY)
        {
            List<List<Tile>> newBoard = CopyBoard(_boardTiles);
            (newBoard[toY][toX], newBoard[fromY][fromX]) = (newBoard[fromY][fromX], newBoard[toY][toX]);

            List<BoardSequence> boardSequences = new();
            List<MatchInfos> allMatches = FindCompleteMatches(newBoard);

            while ( allMatches.Count > 0 )
            {

                SpecialTileType specialTypeToCreate = SpecialTileType.None;
                Vector2Int specialTileCreationPos = new Vector2Int(-1, -1);

                // Create the power tile;
                foreach ( var match in allMatches )
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

        private List<MatchInfos> FindCompleteMatches(List<List<Tile>> board)
        {
            List<MatchInfos> allMatches = new List<MatchInfos>();

            // Horizontal verification
            for (int y = 0; y < board.Count; y++ )
            {
                // Loop stops at 'Count - 2' as it's the last safe position
                // to start checking for a 3-tile match (at x, x+1, and x+2)
                for ( int x = 0; x < board[y].Count -2; )
                {
                    Tile currentTile = board [y] [x];

                    // Guard Clause
                    if ( currentTile.Type == -1 )
                    {
                        x++;
                        continue;
                    }

                    List<Vector2Int> currentMatch = new List<Vector2Int>();
                    currentMatch.Add(new Vector2Int(x, y));

                    for ( int i = x + 1; i < board [y].Count; i++ )
                    {
                        if ( board[y][i].Type == currentTile.Type )
                        {
                            currentMatch.Add(new Vector2Int(i, y));
                        } 
                        else
                        {
                            break;
                        }
                    }

                    if(currentMatch.Count >= 3 )
                    {
                        allMatches.Add(new MatchInfos(currentMatch, MatchDirection.Horizontal));
                    }

                    // Advance the cursor past the checked tiles to avoid redundant verification
                    x += currentMatch.Count;
                }
            }

            // Vertical verification
            // The information below is identical to the code above, but applied to the vertical axis
            for ( int x = 0; x < board[0].Count; x++ )
            {
                for (int y = 0; y < board.Count -2; )
                {
                    Tile currentTile = board [y] [x];

                    if(currentTile.Type == -1 )
                    {
                        y++;
                        continue;
                    }

                    List<Vector2Int> currentMatch = new List<Vector2Int>();
                    currentMatch.Add(new Vector2Int(x, y));

                    for ( int i = y + 1; i < board.Count; i++ )
                    {
                        if ( board [i] [x].Type == currentTile.Type )
                        {
                            currentMatch.Add(new Vector2Int(x, i));
                        } else
                        {
                            break;
                        }
                    }

                    if ( currentMatch.Count >= 3 )
                    {

                        allMatches.Add(new MatchInfos(currentMatch, MatchDirection.Vertical));
                    }

                    y += currentMatch.Count;
                }
            }

            return allMatches;
        }

        private static List<List<Tile>> CopyBoard(List<List<Tile>> boardToCopy)
        {
            List<List<Tile>> newBoard = new(boardToCopy.Count);
            for (int y = 0; y < boardToCopy.Count; y++)
            {
                newBoard.Add(new List<Tile>(boardToCopy[y].Count));
                for (int x = 0; x < boardToCopy[y].Count; x++)
                {
                    Tile tile = boardToCopy[y][x];

                    newBoard [y].Add(new Tile { Id = tile.Id, Type = tile.Type, Special = tile.Special });
                }
            }

            return newBoard;
        }

        private List<List<Tile>> CreateBoard(int width, int height, List<int> tileTypes)
        {
            List<List<Tile>> board = new(height);
            _tileCount = 0;
            for (int y = 0; y < height; y++)
            {
                board.Add(new List<Tile>(width));
                for (int x = 0; x < width; x++)
                {
                    board[y].Add(new Tile { Id = -1, Type = -1 });
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    List<int> noMatchTypes = new(tileTypes.Count);
                    for (int i = 0; i < tileTypes.Count; i++)
                    {
                        noMatchTypes.Add(_tilesTypes[i]);
                    }

                    if (x > 1 &&
                        board[y][x - 1].Type == board[y][x - 2].Type)
                    {
                        noMatchTypes.Remove(board[y][x - 1].Type);
                    }

                    if (y > 1 &&
                        board[y - 1][x].Type == board[y - 2][x].Type)
                    {
                        noMatchTypes.Remove(board[y - 1][x].Type);
                    }

                    board[y][x].Id = _tileCount++;
                    board[y][x].Type = noMatchTypes[Random.Range(0, noMatchTypes.Count)];
                }
            }

            return board;
        }
    }
}
