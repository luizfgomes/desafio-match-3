using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Views;

namespace Gazeus.DesafioMatch3.Controllers
{
    public class ScoreController
    {
        private readonly ScoreModel _scoreModel;
        private readonly ScoreView _scoreView;
        private const int PointsValue = 10;

        public ScoreController (ScoreModel model, ScoreView view )
        {
            _scoreModel = model;
            _scoreView = view;

            _scoreModel.OnScoreChanged += _scoreView.UpdateScore;
        }

        public void OnTilesMatched (int matchedCount)
        {
            int points = matchedCount * PointsValue;
            _scoreModel.AddScore(points);
        }
    }
}
