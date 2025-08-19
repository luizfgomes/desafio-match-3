using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Enums;
using Gazeus.DesafioMatch3.Core.Abstractions;
using UnityEngine;
using System.Linq;

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

        public GameService ( BoardFactory boardFactory, IRandomizer randomizer )
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
            var fromPos = new Vector2Int(fromX, fromY);
            var toPos = new Vector2Int(toX, toY);

            (_currentBoard.Tiles [toY] [toX], _currentBoard.Tiles [fromY] [fromX]) =
                (_currentBoard.Tiles [fromY] [fromX], _currentBoard.Tiles [toY] [toX]);

            var allMatches = _matchFinder.FindCompleteMatches(_currentBoard);

            var powerupPos = new Vector2Int(-1, -1);

            if ( _currentBoard.Tiles [fromPos.y] [fromPos.x].Special != SpecialTileType.None )
            {
                if ( allMatches.Any(m => m.MatchedTiles.Contains(fromPos)) )
                {
                    powerupPos = fromPos;
                }
            }

            if ( _currentBoard.Tiles [toPos.y] [toPos.x].Special != SpecialTileType.None )
            {
                if ( allMatches.Any(m => m.MatchedTiles.Contains(toPos)) )
                {
                    powerupPos = toPos;
                }
            }

            if ( powerupPos != new Vector2Int(-1, -1) )
            {
                var matchedTiles = new List<Vector2Int>();
                var tile = _currentBoard.Tiles [powerupPos.y] [powerupPos.x];

                if ( tile.Special == SpecialTileType.LineClearHorizontal )
                {
                    for ( int x = 0; x < _currentBoard.Width; x++ )
                        matchedTiles.Add(new Vector2Int(x, powerupPos.y));
                } else if ( tile.Special == SpecialTileType.LineClearVertical )
                {
                    for ( int y = 0; y < _currentBoard.Height; y++ )
                        matchedTiles.Add(new Vector2Int(powerupPos.x, y));
                }

                tile.Special = SpecialTileType.None;

                allMatches.Add(new MatchInfos(matchedTiles, MatchDirection.None));
            }

            while ( allMatches.Count > 0 )
            {
                var specialTileCreationPosition = new Vector2Int(toX, toY);
                MatchProcessingResult processingResult = ProcessMatches(_currentBoard, allMatches, specialTileCreationPosition);

                List<MovedTileInfo> movedTiles = ApplyGravity(_currentBoard);

                foreach ( var transformedTile in processingResult.TransformedTiles )
                {
                    foreach ( var movedTile in movedTiles )
                    {
                        if ( movedTile.From == transformedTile.Position )
                        {
                            transformedTile.Position = movedTile.To;
                            break;
                        }
                    }
                }

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

        private List<MovedTileInfo> ApplyGravity ( Board board )
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

        private MatchProcessingResult ProcessMatches ( Board board, List<MatchInfos> allMatches, Vector2Int? specialTileCreationPosition )
        {
            var destroyedPositions = new HashSet<Vector2Int>();
            var transformedTiles = new List<TransformedTileInfo>();
            var powerupsToActivate = new Queue<Vector2Int>();

            foreach ( var match in allMatches )
            {
                var activatedPowerupPos = match.MatchedTiles.FirstOrDefault(pos => board.Tiles [pos.y] [pos.x].Special != SpecialTileType.None);

                if ( activatedPowerupPos != Vector2Int.zero )
                {
                    powerupsToActivate.Enqueue(activatedPowerupPos);
                }
            }

            while ( powerupsToActivate.Count > 0 )
            {
                var currentPowerupPos = powerupsToActivate.Dequeue();
                var tile = board.Tiles [currentPowerupPos.y] [currentPowerupPos.x];

                if ( tile.Special == SpecialTileType.None )
                {
                    continue;
                }

                if ( tile.Special == SpecialTileType.LineClearHorizontal )
                {
                    for ( int x = 0; x < board.Width; x++ )
                    {
                        var targetPos = new Vector2Int(x, currentPowerupPos.y);
                        var targetTile = board.Tiles [targetPos.y] [targetPos.x];

                        if ( targetTile.Special != SpecialTileType.None && !destroyedPositions.Contains(targetPos) )
                        {
                            powerupsToActivate.Enqueue(targetPos);
                        }
                        destroyedPositions.Add(targetPos);
                    }
                } else if ( tile.Special == SpecialTileType.LineClearVertical )
                {
                    for ( int y = 0; y < board.Height; y++ )
                    {
                        var targetPos = new Vector2Int(currentPowerupPos.x, y);
                        var targetTile = board.Tiles [targetPos.y] [targetPos.x];

                        if ( targetTile.Special != SpecialTileType.None && !destroyedPositions.Contains(targetPos) )
                        {
                            powerupsToActivate.Enqueue(targetPos);
                        }
                        destroyedPositions.Add(targetPos);
                    }
                }

                board.Tiles [currentPowerupPos.y] [currentPowerupPos.x].Special = SpecialTileType.None;
            }

            var regularMatches = allMatches.Where(m => m.MatchedTiles.All(pos => board.Tiles [pos.y] [pos.x].Special == SpecialTileType.None)).ToList();

            foreach ( var match in regularMatches )
            {
                SpecialTileType specialTypeToCreate = SpecialTileType.None;
                Vector2Int posToTransform = Vector2Int.zero;

                if ( match.MatchedTiles.Count >= 4 )
                {
                    if ( match.Direction == MatchDirection.Horizontal )
                        specialTypeToCreate = SpecialTileType.LineClearHorizontal;
                    else if ( match.Direction == MatchDirection.Vertical )
                        specialTypeToCreate = SpecialTileType.LineClearVertical;
                }

                if ( specialTypeToCreate != SpecialTileType.None )
                {
                    if ( specialTileCreationPosition.HasValue && match.MatchedTiles.Contains(specialTileCreationPosition.Value) )
                    {
                        posToTransform = specialTileCreationPosition.Value;
                    } else
                    {
                        posToTransform = match.MatchedTiles.Last();
                    }

                    board.Tiles [posToTransform.y] [posToTransform.x].Special = specialTypeToCreate;
                    transformedTiles.Add(new TransformedTileInfo
                    {
                        Position = posToTransform,
                        NewSpecialType = specialTypeToCreate
                    });
                }

                foreach ( var pos in match.MatchedTiles )
                {
                    if ( pos != posToTransform )
                    {
                        destroyedPositions.Add(pos);
                    }
                }
            }

            foreach ( var pos in destroyedPositions )
            {
                board.Tiles [pos.y] [pos.x].Type = TileType.Empty;
                board.Tiles [pos.y] [pos.x].Id = null;
                board.Tiles [pos.y] [pos.x].Special = SpecialTileType.None;
            }

            return new MatchProcessingResult(destroyedPositions.ToList(), transformedTiles);
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
