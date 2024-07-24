using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPG_Project
{
    public class Direction
    {
        public readonly static Direction Up = new Direction(-1, 0);
        public readonly static Direction Down = new Direction(1, 0);
        public readonly static Direction Right = new Direction(0, 1);
        public readonly static Direction Left = new Direction(0, -1);
        public readonly static Direction UpAndRight = new Direction(-1, 1);
        public readonly static Direction DownAndRight = new Direction(1, 1);
        public readonly static Direction UpAndLeft = new Direction(-1, -1);
        public readonly static Direction DownAndLeft = new Direction(1, -1);

        public int RowOffset { get; }
        public int ColOffset { get; }

        private Direction(int rowOffset, int colOffset)
        {
            RowOffset = rowOffset;
            ColOffset = colOffset;
        }
    }
}
