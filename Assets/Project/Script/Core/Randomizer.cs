using UnityEngine;
using Gazeus.DesafioMatch3.Core.Abstractions;

namespace Gazeus.DesafioMatch3.Core
{
    public class Randomizer : IRandomizer
    {
        public int Range ( int minInclusive, int maxExclusive )
        {
            return Random.Range(minInclusive, maxExclusive);
        }
    }
}