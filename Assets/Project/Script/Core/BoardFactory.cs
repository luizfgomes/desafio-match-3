using System.Collections.Generic;
using Gazeus.DesafioMatch3.Core.Abstractions;
using Gazeus.DesafioMatch3.Enums;

namespace Gazeus.DesafioMatch3.Core
{
    public class BoardFactory
    {
        private readonly IRandomizer _randomizer;
        private int _tileCount;

        /// <summary>
        /// --- PT-BR ---
        /// Criada uma abstração para facilitar futuros testes unitários.
        /// Ao injetar um IRandomizer, podemos passar uma implementação mock
        /// durante os testes e garantir resultados previsíveis e consistentes.
        /// 
        /// --- EN ---
        /// Created an abstraction to facilitate future unit tests.
        /// By injecting an IRandomizer, we can pass a mock implementation
        /// during testing and ensure predictable, consistent results.
        /// </summary>
        public BoardFactory ( IRandomizer randomizer )
        {
            _randomizer = randomizer;
        }

        public Board CreateBoard ( int width, int height, List<TileType> tileTypes )
        {
            Board board = new Board(width, height);
            _tileCount = 0;

            for ( int y = 0; y < height; y++ )
            {
                for ( int x = 0; x < width; x++ )
                {
                    List<TileType> noMatchTypes = new List<TileType>(tileTypes);

                    if ( x > 1 && board.Tiles [y] [x - 1].Type == board.Tiles [y] [x - 2].Type )
                    {
                        noMatchTypes.Remove(board.Tiles [y] [x - 1].Type);
                    }

                    if ( y > 1 && board.Tiles [y - 1] [x].Type == board.Tiles [y - 2] [x].Type )
                    {
                        noMatchTypes.Remove(board.Tiles [y - 1] [x].Type);
                    }

                    int randomIndex = _randomizer.Range(0, noMatchTypes.Count);
                    TileType randomType = noMatchTypes [randomIndex];

                    board.Tiles [y] [x].Id = _tileCount++;
                    board.Tiles [y] [x].Type = randomType;
                }
            }
            return board;
        }
    }
}