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
            int points = 0;
            int basePoints = matchedCount * PointsValue;

            if (matchedCount >= 5)
            {
                points = basePoints + 50;
            } 
            else if (matchedCount >= 4)
            {
                points = basePoints + 20;
            } 
            else
            {
                points = basePoints;
            }

            _scoreModel.AddScore(points);
        }
    }
}
