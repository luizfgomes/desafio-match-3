using System;

namespace Gazeus.DesafioMatch3.Models
{
    public class ScoreModel
    {
        public event Action<int> OnScoreChanged;

        private int _score;
        public int Score => _score;

        public void AddScore(int amount )
        {
            _score += amount;
            OnScoreChanged?.Invoke(_score);
        }

        public void ResetScore()
        {
            _score = 0;
            OnScoreChanged?.Invoke(_score);
        }
    }
}
