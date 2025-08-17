using System.Collections.Generic;
using Gazeus.DesafioMatch3.Enums;
using Gazeus.DesafioMatch3.Models;

namespace Gazeus.DesafioMatch3.Core
{
    public class Board
    {
        public List<List<Tile>> Tiles { get; private set; }
        public int Width { get; }
        public int Height { get; }

        public Board(int width, int height )
        {
            Width = width;
            Height = height;

            Tiles = new List<List<Tile>>(height);

            for(int y =0; y < height; y++ )
            {
                Tiles.Add(new List<Tile>(width));

                for ( int x = 0; x < width; x++ )
                {
                    Tiles [y].Add(new Tile { Id = null, Type = TileType.Empty });
                }
            }
        }

        public Board Copy ()
        {
            var newBoard = new Board(Width, Height);

            for ( int y = 0; y < Height; y++ )
            {
                for ( int x = 0; x < Width; x++ )
                {
                    Tile originalTile = Tiles [y] [x];

                    newBoard.Tiles [y] [x] = new Tile
                    {
                        Id = originalTile.Id,
                        Type = originalTile.Type,
                        Special = originalTile.Special
                    };
                }
            }

            return newBoard;
        }
    }
}
