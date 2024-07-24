using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace RPG_Project
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game();

            game.Run();
        }
    }
}
