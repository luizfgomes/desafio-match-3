using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Enums;
using Gazeus.DesafioMatch3.Core.Abstractions;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Core
{
    public class GameService
    {
        private readonly MatchFinder _matchFinder = new MatchFinder();
        private readonly BoardFactory _boardFactory;
        private readonly IRandomizer _randomizer;

        private List<TileType> _tileTypesList;
        private Board _currentBoard;
        private int _tileCount = 0;

        public GameService (BoardFactory boardFactory, IRandomizer randomizer)
        {
            _boardFactory = boardFactory;
            _randomizer = randomizer;
        }

        public bool IsValidMovement ( int fromX, int fromY, int toX, int toY )
        {
            Board simulatedBoard = _currentBoard.Copy();

            (simulatedBoard.Tiles [toY] [toX], simulatedBoard.Tiles [fromY] [fromX]) =
                (simulatedBoard.Tiles [fromY] [fromX], simulatedBoard.Tiles [toY] [toX]);

            return _matchFinder.FindCompleteMatches(simulatedBoard).Count > 0;
        }

        public List<List<Tile>> StartGame ( int boardWidth, int boardHeight )
        {
            _tileTypesList = new List<TileType> { TileType.Blue, TileType.Green, TileType.Red, TileType.Yellow };
            _currentBoard = _boardFactory.CreateBoard(boardWidth, boardHeight, _tileTypesList);

            return _currentBoard.Tiles;
        }

        public List<BoardSequence> SwapTile ( int fromX, int fromY, int toX, int toY )
        {
            var boardSequences = new List<BoardSequence>();

            (_currentBoard.Tiles [toY] [toX], _currentBoard.Tiles [fromY] [fromX]) =
                (_currentBoard.Tiles [fromY] [fromX], _currentBoard.Tiles [toY] [toX]);

            var allMatches = _matchFinder.FindCompleteMatches(_currentBoard);

            while ( allMatches.Count > 0 )
            {
                var movePosition = new Vector2Int(toX, toY);
                MatchProcessingResult processingResult = ProcessMatches(_currentBoard, allMatches, movePosition);

                List<MovedTileInfo> movedTiles = ApplyGravity(_currentBoard);

                List<AddedTileInfo> addedTiles = RefillBoard(_currentBoard);

                boardSequences.Add(new BoardSequence
                {
                    MatchedPosition = processingResult.DestroyedPositions,
                    TransformedTiles = processingResult.TransformedTiles,
                    MovedTiles = movedTiles,
                    AddedTiles = addedTiles
                });

                allMatches = _matchFinder.FindCompleteMatches(_currentBoard);
            }

            return boardSequences;
        }

        private List<MovedTileInfo> ApplyGravity (Board board )
        {
            var movedTiles = new List<MovedTileInfo>();
            for ( int x = 0; x < board.Width; x++ )
            {
                int writeY = board.Height - 1;
                for ( int readY = board.Height - 1; readY >= 0; readY-- )
                {
                    Tile tile = board.Tiles [readY] [x];
                    if ( tile.Type != TileType.Empty )
                    {
                        if ( writeY != readY )
                        {
                            board.Tiles [writeY] [x] = tile;
                            board.Tiles [readY] [x] = new Tile { Id = null, Type = TileType.Empty };
                            movedTiles.Add(new MovedTileInfo
                            {
                                From = new Vector2Int(x, readY),
                                To = new Vector2Int(x, writeY)
                            });
                        }
                        writeY--;
                    }
                }
            }
            return movedTiles;
        }

        private List<AddedTileInfo> RefillBoard ( Board board )
        {
            var addedTiles = new List<AddedTileInfo>();
            for ( int y = 0; y < board.Height; y++ )
            {
                for ( int x = 0; x < board.Width; x++ )
                {
                    if ( board.Tiles [y] [x].Type == TileType.Empty )
                    {
                        int tileTypeIndex = _randomizer.Range(0, _tileTypesList.Count);
                        Tile newTile = new Tile
                        {
                            Id = _tileCount++,
                            Type = _tileTypesList [tileTypeIndex],
                            Special = SpecialTileType.None
                        };
                        board.Tiles [y] [x] = newTile;
                        addedTiles.Add(new AddedTileInfo { Position = new Vector2Int(x, y), Type = newTile.Type });
                    }
                }
            }
            return addedTiles;
        }

        private MatchProcessingResult ProcessMatches ( Board board, List<MatchInfos> allMatches, Vector2Int movePosition )
        {
            var destroyedPositions = new List<Vector2Int>();
            var transformedTiles = new List<TransformedTileInfo>();

            SpecialTileType specialTypeToCreate = SpecialTileType.None;
            Vector2Int specialTileCreationPos = new Vector2Int(-1, -1);

            foreach ( var match in allMatches )
            {
                if ( match.Count >= 4 )
                {
                    specialTypeToCreate = (match.Direction == MatchDirection.Horizontal) ? SpecialTileType.LineClearHorizontal : SpecialTileType.LineClearVertical;

                    if ( match.MatchedTiles.Contains(movePosition) )
                    {
                        specialTileCreationPos = movePosition;
                    } else 
                    {
                        specialTileCreationPos = match.MatchedTiles [0];
                    }
                    break;
                }
            }

            var positionsToClear = new HashSet<Vector2Int>();
            foreach ( var match in allMatches )
            {
                foreach ( var pos in match.MatchedTiles )
                {
                    positionsToClear.Add(pos);
                }
            }

            foreach ( var pos in positionsToClear )
            {
                if ( pos == specialTileCreationPos )
                {
                    board.Tiles [pos.y] [pos.x].Special = specialTypeToCreate;

                    transformedTiles.Add(new TransformedTileInfo
                    {
                        Position = pos,
                        NewSpecialType = specialTypeToCreate
                    });
                } else
                {
                    board.Tiles [pos.y] [pos.x].Type = TileType.Empty;
                    board.Tiles [pos.y] [pos.x].Id = null;
                    board.Tiles [pos.y] [pos.x].Special = SpecialTileType.None;

                    destroyedPositions.Add(pos);
                }
            }

            return new MatchProcessingResult(destroyedPositions, transformedTiles);
        }

        private class MatchProcessingResult
        {
            public readonly List<Vector2Int> DestroyedPositions;
            public readonly List<TransformedTileInfo> TransformedTiles;

            public MatchProcessingResult ( List<Vector2Int> destroyedPositions, List<TransformedTileInfo> transformedTiles )
            {
                DestroyedPositions = destroyedPositions;
                TransformedTiles = transformedTiles;
            }
        }
    }
}
