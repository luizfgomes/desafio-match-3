using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.Enums;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.Views
{
    public class BoardView : MonoBehaviour
    {
        public event Action<int, int> TileClicked;

        [SerializeField] private GridLayoutGroup _boardContainer;
        [SerializeField] private TilePrefabRepository _tilePrefabRepository;
        [SerializeField] private TileSpotView _tileSpotPrefab;
        [SerializeField] private Sprite _linePowerupSprite;
        [SerializeField] private Sprite _bombPowerupSprite;

        private GameObject[][] _tiles;
        private TileSpotView[][] _tileSpots;

        public void CreateBoard(List<List<Tile>> board)
        {
            _boardContainer.constraintCount = board[0].Count;
            _tiles = new GameObject[board.Count][];
            _tileSpots = new TileSpotView[board.Count][];

            for (int y = 0; y < board.Count; y++)
            {
                _tiles[y] = new GameObject[board[0].Count];
                _tileSpots[y] = new TileSpotView[board[0].Count];

                for (int x = 0; x < board[0].Count; x++)
                {
                    TileSpotView tileSpot = Instantiate(_tileSpotPrefab);
                    tileSpot.transform.SetParent(_boardContainer.transform, false);
                    tileSpot.SetPosition(x, y);
                    tileSpot.Clicked += TileSpot_Clicked;

                    _tileSpots[y][x] = tileSpot;

                    TileType tileType = board [y] [x].Type;
                    if ( tileType != TileType.Empty )
                    {
                        int tileTypeIndex = (int) tileType;

                        GameObject tilePrefab = _tilePrefabRepository.TileTypePrefabList [tileTypeIndex];

                        GameObject tile = Instantiate(tilePrefab);
                        tileSpot.SetTile(tile);
                        _tiles [y] [x] = tile;
                    }
                }
            }
        }

        public Tween CreateTile(List<AddedTileInfo> addedTiles)
        {
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < addedTiles.Count; i++)
            {
                AddedTileInfo addedTileInfo = addedTiles[i];
                Vector2Int position = addedTileInfo.Position;

                TileSpotView tileSpot = _tileSpots[position.y][position.x];

                int tileTypeIndex = (int) addedTileInfo.Type;

                GameObject tilePrefab = _tilePrefabRepository.TileTypePrefabList [tileTypeIndex];

                GameObject tile = Instantiate(tilePrefab);
                tileSpot.SetTile(tile);
                _tiles [position.y] [position.x] = tile;

                tile.transform.localScale = Vector2.zero;
                sequence.Join(tile.transform.DOScale(1.0f, 0.2f));
            }

            return sequence;
        }

        public Tween DestroyTiles(List<Vector2Int> matchedPosition)
        {
            for (int i = 0; i < matchedPosition.Count; i++)
            {
                Vector2Int position = matchedPosition[i];
                Destroy(_tiles[position.y][position.x]);
                _tiles[position.y][position.x] = null;
            }

            return DOVirtual.DelayedCall(0.2f, () => { });
        }

        public Tween MoveTiles(List<MovedTileInfo> movedTiles)
        {
            GameObject[][] tiles = new GameObject[_tiles.Length][];
            for (int y = 0; y < _tiles.Length; y++)
            {
                tiles[y] = new GameObject[_tiles[y].Length];
                for (int x = 0; x < _tiles[y].Length; x++)
                {
                    tiles[y][x] = _tiles[y][x];
                }
            }

            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < movedTiles.Count; i++)
            {
                MovedTileInfo movedTileInfo = movedTiles[i];

                Vector2Int from = movedTileInfo.From;
                Vector2Int to = movedTileInfo.To;

                sequence.Join(_tileSpots[to.y][to.x].AnimatedSetTile(_tiles[from.y][from.x]));

                tiles[to.y][to.x] = _tiles[from.y][from.x];
            }

            _tiles = tiles;

            return sequence;
        }

        public Tween SwapTiles(int fromX, int fromY, int toX, int toY)
        {
            Sequence sequence = DOTween.Sequence();

            sequence.Append(_tileSpots[fromY][fromX].AnimatedSetTile(_tiles[toY][toX]));
            sequence.Join(_tileSpots[toY][toX].AnimatedSetTile(_tiles[fromY][fromX]));

            (_tiles[toY][toX], _tiles[fromY][fromX]) = (_tiles[fromY][fromX], _tiles[toY][toX]);

            return sequence;
        }

        public Tween LinePowerup ( List<TransformedTileInfo> transformedTiles )
        {
            Sequence sequence = DOTween.Sequence();

            for ( int i = 0; i < transformedTiles.Count; i++ )
            {
                var transformedTileInfo = transformedTiles[i];
                var position = transformedTileInfo.Position;
                var tile = _tiles [position.y] [position.x];
                var specialTileObject = new GameObject("LinePowerup", typeof(Image));
                Transform? specialTileTransform;

                specialTileObject.transform.SetParent(tile.transform, false); 

                var specialTileImage = specialTileObject.GetComponent<Image>();
                specialTileImage.sprite = _linePowerupSprite;

                RectTransform rectTransform = specialTileImage.rectTransform;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;

                specialTileTransform = specialTileObject.transform;
                specialTileTransform.localScale = Vector3.zero;

                if ( transformedTileInfo.NewSpecialType == SpecialTileType.LineClearHorizontal )
                {
                    specialTileTransform.localRotation = Quaternion.Euler(0, 0, 0);
                } else if ( transformedTileInfo.NewSpecialType == SpecialTileType.LineClearVertical )
                {
                    specialTileTransform.localRotation = Quaternion.Euler(0, 0, 90);
                }

                sequence.Join(specialTileTransform.DOScale(1.0f, 0.2f));
            }
            return sequence;
        }

        public Tween BombPowerup ( List<TransformedTileInfo> transformedTiles )
        {
            Sequence sequence = DOTween.Sequence();

            for ( int i = 0; i < transformedTiles.Count; i++ )
            {
                var transformedTileInfo = transformedTiles [i];
                var position = transformedTileInfo.Position;
                var tile = _tiles [position.y] [position.x];
                var specialTileObject = new GameObject("BombPowerup", typeof(Image));

                specialTileObject.transform.SetParent(tile.transform, false);

                var specialTileImage = specialTileObject.GetComponent<Image>();
                specialTileImage.sprite = _bombPowerupSprite;

                RectTransform rectTransform = specialTileImage.rectTransform;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;

                specialTileObject.transform.localScale = Vector3.zero;

                sequence.Join(specialTileObject.transform.DOScale(1.0f, 0.2f));
            }
            return sequence;
        }

        #region Events
        private void TileSpot_Clicked(int x, int y)
        {
            TileClicked(x, y);
        }
        #endregion
    }
}
