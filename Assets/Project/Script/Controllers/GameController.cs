using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.Core;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Views;
using Gazeus.DesafioMatch3.Core.Abstractions;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Controllers
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private ScoreView _scoreView;
        [SerializeField] private int _boardHeight = 10;
        [SerializeField] private int _boardWidth = 10;

        private GameService _gameEngine;
        private ScoreModel _scoreModel;
        private ScoreController _scoreController;

        private bool _isAnimating;
        private Vector2Int? _selectedTilePostion;
        //private int _selectedX = -1;
        //private int _selectedY = -1;

        #region Unity
        private void Awake()
        {
            IRandomizer randomizer = new Randomizer();
            BoardFactory factory = new BoardFactory(randomizer);
            _gameEngine = new GameService(factory, randomizer);
            _gameEngine.StartGame(_boardWidth, _boardHeight);

            _boardView.TileClicked += OnTileClick;

            _scoreModel = new ScoreModel();
            _scoreController = new ScoreController(_scoreModel, _scoreView);
            _scoreModel.ResetScore();
        }

        private void OnDestroy()
        {
            _boardView.TileClicked -= OnTileClick;
        }

        private void Start()
        {
            List<List<Tile>> board = _gameEngine.StartGame(_boardWidth, _boardHeight);
            _boardView.CreateBoard(board);
        }
        #endregion

        private void AnimateBoard(List<BoardSequence> boardSequences, int index, Action onComplete)
        {
            BoardSequence boardSequence = boardSequences[index];

            if(boardSequence.MatchedPosition.Count > 0 )
            {
                _scoreController.OnTilesMatched(boardSequence.MatchedPosition.Count);
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_boardView.DestroyTiles(boardSequence.MatchedPosition));
            sequence.Append(_boardView.MoveTiles(boardSequence.MovedTiles));
            sequence.Append(_boardView.CreateTile(boardSequence.AddedTiles));

            index += 1;
            if (index < boardSequences.Count)
            {
                sequence.onComplete += () => AnimateBoard(boardSequences, index, onComplete);
            }
            else
            {
                sequence.onComplete += () => onComplete();
            }
        }

        private void OnTileClick ( int x, int y )
        {
            if ( _isAnimating )
                return;

            Vector2Int clickedPostition = new Vector2Int(x, y);

            if ( !_selectedTilePostion.HasValue )
            {
                SelectTile(clickedPostition);
            } 
            else
            {
                ProcessSecondTileClick(clickedPostition);
            }
        }

        private void SelectTile (Vector2Int position )
        {
            _selectedTilePostion = position;
        }

        private void ProcessSecondTileClick(Vector2Int clickedPostition )
        {
            Vector2Int firstPosition = _selectedTilePostion.Value;

            if ( firstPosition == clickedPostition )
            {
                DeselectTile();
                return;
            }

            if ( Mathf.Abs(firstPosition.x - clickedPostition.x) + Mathf.Abs(firstPosition.y - clickedPostition.y) > 1 )
            {
                DeselectTile();
                SelectTile(clickedPostition);
                return;
            }

            ValidateSwap(firstPosition, clickedPostition);
        }

        private void ValidateSwap (Vector2Int from, Vector2Int to)
        {
            _isAnimating = true;
            DeselectTile();

            _boardView.SwapTiles(from.x, from.y, to.x, to.y).onComplete += () => SwapFinishedAnimation(from, to);
        }

        private void SwapFinishedAnimation (Vector2Int from, Vector2Int to)
        {
            bool isValid = _gameEngine.IsValidMovement(from.x, from.y, to.x, to.y);

            if ( !isValid )
            {
                _boardView.SwapTiles(to.x, to.y, from.x, from.y).onComplete += () => _isAnimating = false;
            }
            else
            {
                List<BoardSequence> swapResult = _gameEngine.SwapTile(from.x, from.y, to.x, to.y);
                AnimateBoard(swapResult, 0, () => _isAnimating = false);
            }
        }

        private void DeselectTile ()
        {
            if ( _selectedTilePostion.HasValue )
            {
                _selectedTilePostion = null;
            }
        }
    }
}
