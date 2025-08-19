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

            (_currentBoard.Tiles [toY] [toX], _currentBoard.Tiles [fromY] [fromX]) =
                (_currentBoard.Tiles [fromY] [fromX], _currentBoard.Tiles [toY] [toX]);

            var allMatches = _matchFinder.FindCompleteMatches(_currentBoard);

            while ( allMatches.Count > 0 )
            {
                var movePosition = new Vector2Int(toX, toY);
                MatchProcessingResult processingResult = ProcessMatches(_currentBoard, allMatches, movePosition);

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

        private MatchProcessingResult ProcessMatches ( Board board, List<MatchInfos> allMatches, Vector2Int movePosition )
        {
            var destroyedPositions = new List<Vector2Int>();
            var transformedTiles = new List<TransformedTileInfo>();

            // Cada matchInfos é processado individualmente
            foreach ( var match in allMatches )
            {
                var group = new HashSet<Vector2Int>(match.MatchedTiles);
                int comboSize = group.Count;

                SpecialTileType specialTypeToCreate = SpecialTileType.None;

                int width = group.Max(p => p.x) - group.Min(p => p.x) + 1;
                int height = group.Max(p => p.y) - group.Min(p => p.y) + 1;
                bool isLine = width == 1 || height == 1;

                if ( isLine )
                {
                    // Linhas normais.
                    if ( comboSize == 3 )
                    {
                        Debug.Log($"Normal de 3 em {string.Join(",", group)}");
                    } else if ( comboSize == 4 )
                    {
                        if ( width > 1 )
                        {
                            specialTypeToCreate = SpecialTileType.LineClearHorizontal;

                            Debug.Log("Linha de 4 cria limpa linha H");
                        } else
                        {
                            specialTypeToCreate = SpecialTileType.LineClearVertical;
                            Debug.Log("Coluna de 4 cria limpa linha V");
                        }
                    } else if ( comboSize >= 5 )
                    {
                        
                        if ( width > 1 )
                            specialTypeToCreate = SpecialTileType.LineClearHorizontal;
                        
                        else
                            specialTypeToCreate = SpecialTileType.LineClearVertical;
                        Debug.Log("Linha/coluna de 5 detectada futuro super power-up");
                    }
                } else
                {
                    // Combinações especiais!
                    bool isT = false;
                    foreach ( var pos in group )
                    {
                        int neighbors = 0;
                        if ( group.Contains(new Vector2Int(pos.x, pos.y + 1)) )
                            neighbors++;
                        if ( group.Contains(new Vector2Int(pos.x, pos.y - 1)) )
                            neighbors++;
                        if ( group.Contains(new Vector2Int(pos.x - 1, pos.y)) )
                            neighbors++;
                        if ( group.Contains(new Vector2Int(pos.x + 1, pos.y)) )
                            neighbors++;
                        if ( neighbors >= 3 )
                        { isT = true; break; }
                    }
                    string comboType = isT ? "T" : "L";
                    Debug.Log($"[MATCH] Combo em {comboType} detectado ({comboSize} peças) – ainda sem efeito.");
                }

                Vector2Int specialTileCreationPos = new Vector2Int(-1, -1);
                if ( specialTypeToCreate != SpecialTileType.None )
                {
                    if ( group.Contains(movePosition) )
                        specialTileCreationPos = movePosition;
                    else
                    {
                        int centerX = (group.Min(p => p.x) + group.Max(p => p.x)) / 2;
                        int centerY = (group.Min(p => p.y) + group.Max(p => p.y)) / 2;
                        specialTileCreationPos = new Vector2Int(centerX, centerY);
                    }
                }

                foreach ( var pos in group )
                {
                    if ( pos == specialTileCreationPos )
                    {
                        board.Tiles [pos.y] [pos.x].Special = specialTypeToCreate;
                        transformedTiles.Add(new TransformedTileInfo
                        {
                            Position = pos,
                            NewSpecialType = specialTypeToCreate
                        });
                        Debug.Log($"[POWER-UP] Criado {specialTypeToCreate} em ({pos.x},{pos.y})");
                    } else
                    {
                        board.Tiles [pos.y] [pos.x].Type = TileType.Empty;
                        board.Tiles [pos.y] [pos.x].Id = null;
                        board.Tiles [pos.y] [pos.x].Special = SpecialTileType.None;
                        destroyedPositions.Add(pos);
                    }
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
