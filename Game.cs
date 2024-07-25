using RPG_Project.CharacterModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Data.SqlClient;
using RPG_Project.DataConnection;

namespace RPG_Project
{
    class Game
    {
        //Variables
        public int globalMonsterId = 1;
        public int rows = 10;
        public int cols = 10;
        private char[][] matrix;
        public RpgDbContext context { get; set; }
        public string characterRace { get; set; }
        public HeroModel ourHero { get; set; }
        private bool failedAttack { get; set; }
        private Dictionary<int, EnemyMonster> monsterIdAndStats { get; set; }
        private readonly Random random = new Random();

        //Private functions
        private void InitVariable()
        {
            this.context = new RpgDbContext();
            this.failedAttack = false;
            this.ourHero = new HeroModel();
            this.monsterIdAndStats = new Dictionary<int, EnemyMonster>();
            this.characterRace = "";
            this.matrix = new char[rows][];
        }
        public Game()
        {
            this.InitVariable();
        }
        /*----------------------------------------------------------------------------*/
        //All Helper Methods are at the bottom of the Game Class
        public void Run()
        {
            //!!!1)First Section!!!
            //!!!- Welcome + Character select section -!!!
            Console.WriteLine("Welcome!");
            Console.WriteLine("Press any key to play");
            Console.ReadLine();
            Console.Clear();
            //All Helper Methods are at the bottom of the Game Class
            PrintChooseCharacterConditions();
            int numCharacter = 0;
            while (!int.TryParse(Console.ReadLine(), out numCharacter)
               || (numCharacter > 3 || numCharacter <= 0))
            {
                Console.Clear();
                PrintChooseCharacterConditions();
            }
            SetOurHeroRace(numCharacter);
            ourHero.Row = 1;
            ourHero.Col = 1;

            Console.WriteLine("Would you like to buff up your stats before starting?");
            Console.WriteLine("Answer with Y for yes or N for no");
            string answer = Console.ReadLine().ToUpper();
            int availablePoints = 3;
            while (answer != "Y" && answer != "N")
            {
                Console.WriteLine("Please only answer with Y for yes or N for no");
                answer = Console.ReadLine().ToUpper();
            }
            if (answer == "Y")
            {
                int usedPoints = BuffUpYourStats(availablePoints, "Strength");
                ourHero.Strength += usedPoints;
                ourHero.AddedStrength += usedPoints;
                availablePoints -= usedPoints;
                if (availablePoints > 0)
                {
                    usedPoints = BuffUpYourStats(availablePoints, "Agility");
                    ourHero.Agility += usedPoints;
                    ourHero.AddedAgility += usedPoints;
                    availablePoints -= usedPoints;
                }
                if (availablePoints > 0)
                {
                    usedPoints = BuffUpYourStats(availablePoints, "Intelligence");
                    ourHero.Intelligence += usedPoints;
                    ourHero.AddedIntelligence += usedPoints;
                    availablePoints -= usedPoints;
                }
            }
            //Final attributes after adding and calculating bonus stats
            ourHero.SetUp();

            //Adds the new hero + all stats + DateTime 
            ConnectToMSSQLAndAddNewCharacterInfoToDb();

            Console.Clear();
            PrintCharacterStats(characterRace);
            Console.WriteLine("Time to play the game");
            Console.WriteLine("Press any key to start the game");
            Console.ReadLine();
            /*----------------------------------------------------------------------------*/
            //!!!2)Second Section !!!
            //!!!- InGame section-!!!
            //Create Matrix
            for (int r = 0; r < rows; r++)
            {
                char[] newRow = new char[cols];
                for (int c = 0; c < cols; c++)
                {
                    newRow[c] = '▒';
                    if (r == 1 && c == 1)
                        newRow[c] = ourHero.Symbol;
                }
                matrix[r] = newRow;
            }

            //Playing the game - while loop ends when health reaches 0 or counter reaches 0
            while (true)
            {
                Console.Clear();
                //In case player pressed attack with no enemies around
                //easy remove if enemies still need to be spawned despite the failed attack
                if (failedAttack == false)
                    AddEnemyOnARandomEmptyMatrixPosition();
                else
                    failedAttack = false;

                //Player Actions:
                PrintMatrixGameState();
                PrintAttackMoveConditions();
                int decision = 0;
                while (!int.TryParse(Console.ReadLine(), out decision)
                    || (decision != 1 && decision != 2))
                {
                    Console.Clear();
                    PrintMatrixGameState();
                    PrintAttackMoveConditions();
                }
                if (decision == 2)//Move
                {
                    PrintMovementLetters();
                    string movement = Console.ReadLine().ToUpper();
                    while (!IsValidMovementLetter(movement))
                    {
                        Console.Clear();
                        PrintMatrixGameState();
                        PrintMovementLetters();
                        movement = Console.ReadLine().ToUpper();
                    }
                    CheckIsMoveValidAndMoveCharactersPosition(movement);
                }
                else if (decision == 1)//Attack
                {
                    Tuple<int, List<EnemyMonster>> monsterInfo = CheckIfMonstersExistInYourRange();
                    int attackedMonsterId = monsterInfo.Item1;
                    List<EnemyMonster> monstersInRange = monsterInfo.Item2;
                    if (attackedMonsterId == -1)//no enemies in our range
                        continue;
                    var dueledMonster = monstersInRange.FirstOrDefault(x => x.Id == attackedMonsterId);
                    dueledMonster.Health -= ourHero.Damage;
                    if (dueledMonster.Health <= 0)
                    {
                        matrix[dueledMonster.Row][dueledMonster.Col] = '▒';
                        monsterIdAndStats.Remove(dueledMonster.Id);
                        monstersInRange.Remove(dueledMonster);
                    }
                }
                //Monster Actions:
                //You can comment these methods if you want to
                //valid check the movement/inputs of the game without being attacked
                MonstersCheckNeighbourCellsToAttack();
                if (IsOurHeroHealthZero() == true)
                    break;
                AllMonstersMoveTowardsOurHero();
            }
        }
        //Helper / Repetitive Methods
        //3) Global Methods:......
        //EF Core DB:
        public void ConnectToMSSQLAndAddNewCharacterInfoToDb()
        {
            var newCharacter = new HeroModel()
            {
                Class = ourHero.Class,
                Strength = ourHero.Strength,
                AddedStrength = ourHero.AddedStrength,
                Agility = ourHero.Agility,
                AddedAgility = ourHero.AddedAgility,
                Intelligence = ourHero.AddedIntelligence,
                Health = ourHero.Health,
                Mana = ourHero.Mana,
                Damage = ourHero.Damage,
                TimeOfCreation = DateTime.Now
            };
            Console.WriteLine("Loading:...");
            try
            {
                context.Heroes.Add(newCharacter);
                context.SaveChanges();
                Console.WriteLine("Hero Added to Db");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //2)...InGame Methods:......
        //2.1) InGame Helper Action Methods
        public Tuple<int, List<EnemyMonster>> CheckIfMonstersExistInYourRange()
        {
            List<EnemyMonster> monstersInRange = CheckNeigboursWithGivenRangeForEnemyMonsters();
            if (monstersInRange.Count == 0)
            {
                failedAttack = true;
                Console.WriteLine("No available targets in your range");
                Console.WriteLine("You can only move");
                Console.WriteLine("Enter any key to continue");
                Console.ReadLine();
                return Tuple.Create(-1, monstersInRange);
            }
            foreach (EnemyMonster monster in monstersInRange)
            {
                Console.WriteLine($"{monster.Id}) Target monster with Id: {monster.Id}");
                Console.WriteLine($"----has {monster.Health} remaining Health");
            }
            Console.Write($"Write the Id of the monster you want to attack: ");
            int attackedMonsterId = 0;
            while (!int.TryParse(Console.ReadLine(), out attackedMonsterId))
            {
                PrintAllAvailableMonstersInRange(monstersInRange);
            }
            while (monstersInRange.All(x => x.Id != attackedMonsterId))
            {
                PrintAllAvailableMonstersInRange(monstersInRange);
                attackedMonsterId = 0;
                while (!int.TryParse(Console.ReadLine(), out attackedMonsterId))
                {
                    PrintAllAvailableMonstersInRange(monstersInRange);
                }
            }
            return Tuple.Create(attackedMonsterId, monstersInRange);
        }
        public EnemyMonster GetMonsterInfoFromOurMapUsingItsPosition(int row, int col)
        {
            EnemyMonster monster =
                             monsterIdAndStats
                            .FirstOrDefault(x => x.Value.Row == row && x.Value.Col == col)
                            .Value;
            return monster;
        }
        public void AllMonstersMoveTowardsOurHero()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            int heroRow = ourHero.Row;
            int heroCol = ourHero.Col;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (matrix[r][c] == '◙')
                    {
                        matrix[r][c] = '▒';

                        EnemyMonster monster =
                             monsterIdAndStats
                            .FirstOrDefault(x => x.Value.Row == r && x.Value.Col == c)
                            .Value;

                        if (r > heroRow)
                            monster.Row--;
                        else if (r < heroRow)
                            monster.Row++;
                        //else(r == heroRow) do nothing

                        if (c > heroCol)
                            monster.Col--;
                        else if (c < heroCol)
                            monster.Col++;
                        //else(c == heroCol) do nothing

                        //Check if new position is occupied by hero/monster
                        if ((monster.Row == heroRow && monster.Col == heroCol)
                          || matrix[monster.Row][monster.Col] == '◙')
                        {
                            monster.Row = r;
                            monster.Col = c;
                        }
                        matrix[monster.Row][monster.Col] = '◙';
                    }
                }
            }
        }
        public void MonstersCheckNeighbourCellsToAttack()
        {
            //monster range is always 1
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            for (int r = ourHero.Row - 1; r <= ourHero.Row + 1; r++)
            {
                for (int c = ourHero.Col - 1; c <= ourHero.Col + 1; c++)
                {
                    if (IsOutOfBoundsOfMatrix(r, c))
                        continue;
                    if (matrix[r][c] == '◙')
                    {
                        EnemyMonster duelMonster = GetMonsterInfoFromOurMapUsingItsPosition(r, c);
                        ourHero.Health -= duelMonster.Damage;
                    }
                }
            }
        }
        public void CheckIsMoveValidAndMoveCharactersPosition(string movement)
        {
            int oldRow = ourHero.Row;
            int oldCol = ourHero.Col;
            if (ourHero.Row == 0 && ourHero.Col == 0)
            {
                while (movement != "S" && movement != "D" && movement != "X")
                {
                    Console.Clear();
                    PrintMatrixGameState();
                    if (IsValidMovementLetter(movement))
                        Console.WriteLine("You cannot move outside of the field");
                    Console.WriteLine("This turn you can only press:");
                    Console.WriteLine("S = down, D = right, X = downRight");
                    movement = Console.ReadLine().ToUpper();
                }
                MoveCharactersPositionsOnlyValidMoves(movement);
            }
            else if (ourHero.Row == 0 && ourHero.Col == cols - 1)
            {
                while (movement != "S" && movement != "A" && movement != "Z")
                {
                    Console.Clear();
                    PrintMatrixGameState();
                    if (IsValidMovementLetter(movement))
                        Console.WriteLine("You cannot move outside of the field");
                    Console.WriteLine("This turn you can only press:");
                    Console.WriteLine("S = down, A = left, Z = downLeft");
                    movement = Console.ReadLine().ToUpper();
                }
                MoveCharactersPositionsOnlyValidMoves(movement);
            }
            else if (ourHero.Row == rows - 1 && ourHero.Col == 0)
            {
                while (movement != "D" && movement != "W" && movement != "E")
                {
                    Console.Clear();
                    PrintMatrixGameState();
                    if (IsValidMovementLetter(movement))
                        Console.WriteLine("You cannot move outside of the field");
                    Console.WriteLine("This turn you can only press:");
                    Console.WriteLine("D = right, W = up, E = upRight");
                    movement = Console.ReadLine().ToUpper();
                }
                MoveCharactersPositionsOnlyValidMoves(movement);
            }
            else if (ourHero.Row == rows - 1 && ourHero.Col == cols - 1)
            {
                while (movement != "W" && movement != "A" && movement != "Q")
                {
                    Console.Clear();
                    PrintMatrixGameState();
                    if (IsValidMovementLetter(movement))
                        Console.WriteLine("You cannot move outside of the field");
                    Console.WriteLine("This turn you can only press:");
                    Console.WriteLine("W = up, A = left, Q = upLeft");
                    movement = Console.ReadLine().ToUpper();
                }
                MoveCharactersPositionsOnlyValidMoves(movement);
            }
            else if (ourHero.Row == 0)
            {
                while (movement != "A" && movement != "D" && movement != "S"
                    && movement != "Z" && movement != "X")
                {
                    Console.Clear();
                    PrintMatrixGameState();
                    if (IsValidMovementLetter(movement))
                        Console.WriteLine("You cannot move outside of the field");
                    Console.WriteLine("This turn you can only press: ");
                    Console.WriteLine("A = left, D = right, S = down, X = downRight, Z = downLeft");
                    movement = Console.ReadLine().ToUpper();
                }
                MoveCharactersPositionsOnlyValidMoves(movement);
            }
            else if (ourHero.Row == rows - 1)
            {
                while (movement != "A" && movement != "D" && movement != "W"
                    && movement != "Q" && movement != "E")
                {
                    Console.Clear();
                    PrintMatrixGameState();
                    if (IsValidMovementLetter(movement))
                        Console.WriteLine("You cannot move outside of the field");
                    Console.WriteLine("This turn you can only press:");
                    Console.WriteLine("A = left, D = right, W = up, Q = upLeft, E = upRight");
                    movement = Console.ReadLine().ToUpper();
                }
                MoveCharactersPositionsOnlyValidMoves(movement);
            }
            else if (ourHero.Col == 0)
            {
                while (movement != "W" && movement != "S" && movement != "D"
                    && movement != "E" && movement != "X")
                {
                    Console.Clear();
                    PrintMatrixGameState();
                    if (IsValidMovementLetter(movement))
                        Console.WriteLine("You cannot move outside of the field");
                    Console.WriteLine("This turn you can only press:");
                    Console.WriteLine("W = up, S = down, D = right, E = upRight, X = downRight");
                    movement = Console.ReadLine().ToUpper();
                }
                MoveCharactersPositionsOnlyValidMoves(movement);
            }
            else if (ourHero.Col == cols - 1)
            {
                while (movement != "W" && movement != "S" && movement != "A"
                    && movement != "Q" && movement != "Z")
                {
                    Console.Clear();
                    PrintMatrixGameState();
                    if (IsValidMovementLetter(movement))
                        Console.WriteLine("You cannot move outside of the field");
                    Console.WriteLine("This turn you can only press:");
                    Console.WriteLine("W = up, S = down, A = left, Q = upLeft, Z = downLeft");
                    movement = Console.ReadLine().ToUpper();
                }
                MoveCharactersPositionsOnlyValidMoves(movement);
            }
            else//Hero is NOT on a border of the matrix
            {
                MoveCharactersPositionsOnlyValidMoves(movement);
            }

            matrix[oldRow][oldCol] = '▒';
            matrix[ourHero.Row][ourHero.Col] = ourHero.Symbol;
        }
        public void MoveCharactersPositionsOnlyValidMoves(string movement)
        {
            int oldRow = ourHero.Row;
            int oldCol = ourHero.Col;
            matrix[ourHero.Row][ourHero.Col] = '▒';
            switch (movement)
            {
                case "W":
                    ourHero.Row -= ourHero.Range;
                    break;
                case "S":
                    ourHero.Row += ourHero.Range;
                    break;
                case "D":
                    ourHero.Col += ourHero.Range;
                    break;
                case "A":
                    ourHero.Col -= ourHero.Range;
                    break;
                case "E":
                    ourHero.Row -= ourHero.Range;
                    ourHero.Col += ourHero.Range;
                    break;
                case "X":
                    ourHero.Row += ourHero.Range;
                    ourHero.Col += ourHero.Range;
                    break;
                case "Q":
                    ourHero.Row -= ourHero.Range;
                    ourHero.Col -= ourHero.Range;
                    break;
                case "Z":
                    ourHero.Row += ourHero.Range;
                    ourHero.Col -= ourHero.Range;
                    break;
            }
            if (ourHero.Row >= rows)
                ourHero.Row = rows - 1;
            else if (ourHero.Row < 0)
                ourHero.Row = 0;
            if (ourHero.Col >= cols)
                ourHero.Col = cols - 1;
            else if (ourHero.Col < 0)
                ourHero.Col = 0;
            if (matrix[ourHero.Row][ourHero.Col] == '◙')
            {
                Console.WriteLine("You cannot move on top of a monster");
                Console.WriteLine("You attack the monster instead");
                EnemyMonster monster = GetMonsterInfoFromOurMapUsingItsPosition(ourHero.Row, ourHero.Col);

                monster.Health -= ourHero.Damage;
                if (monster.Health <= 0)
                {
                    Console.WriteLine("This monster lost all its health");
                    monsterIdAndStats.Remove(monster.Id);
                    matrix[monster.Row][monster.Col] = '▒';
                }
                else
                    Console.WriteLine($"This monster has {monster.Health} remaining");

                Console.WriteLine("Press any key to continue");
                Console.ReadLine();
                ourHero.Row = oldRow;
                ourHero.Col = oldCol;
            }
            matrix[ourHero.Row][ourHero.Col] = ourHero.Symbol;
        }
        public List<EnemyMonster> CheckNeigboursWithGivenRangeForEnemyMonsters()
        {
            List<EnemyMonster> allMonsters = new List<EnemyMonster>();
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            for (int r = ourHero.Row - ourHero.Range; r <= ourHero.Row + ourHero.Range; r++)
            {
                for (int c = ourHero.Col - ourHero.Range; c <= ourHero.Col + ourHero.Range; c++)
                {
                    if (IsOutOfBoundsOfMatrix(r, c))
                        continue;
                    //Console.WriteLine($"{matrix[r][c]} Row: {r} Col: {c}");
                    if (matrix[r][c] == '◙')
                    {
                        EnemyMonster newMonster = GetMonsterInfoFromOurMapUsingItsPosition(r, c);
                        allMonsters.Add(newMonster);
                    }
                }
            }
            if (allMonsters.Count == 1)
                Console.WriteLine($"There is 1 monster in your range:");
            else if (allMonsters.Count > 1)
                Console.WriteLine($"There are {allMonsters.Count} monsters in your range:");
            return allMonsters;
        }
        public void AddEnemyOnARandomEmptyMatrixPosition()
        {
            List<Position> empty = new List<Position>(EmptyPositionsOnTheMatrix());

            if (empty.Count == 0)
                return;

            Position position = empty[random.Next(empty.Count)];
            EnemyMonster monster = new EnemyMonster();
            matrix[position.Row][position.Col] = monster.Symbol;
            monster.Row = position.Row;
            monster.Col = position.Col;
            monster.Id = globalMonsterId;
            monsterIdAndStats.Add(globalMonsterId, monster);
            globalMonsterId++;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            /*foreach (var kvp in monsterIdAndStats)
            {
                Console.WriteLine($"This monster's Id: {kvp.Key}");
                Console.WriteLine(kvp.Value.Health);
                Console.WriteLine(kvp.Value.Damage);
                Console.WriteLine(kvp.Value.Agility);
                Console.WriteLine(kvp.Value.Intelligence);
                Console.WriteLine(kvp.Value.Range);
                Console.WriteLine(kvp.Value.Strength);
                Console.WriteLine(kvp.Value.Symbol);
                Console.WriteLine($"This monster's row: {kvp.Value.Row}");
                Console.WriteLine($"This monster's col: {kvp.Value.Col}");
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.WriteLine("33333" + "◙!!!!!◙◙" + "3333333");
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }*/
        }
        public IEnumerable<Position> EmptyPositionsOnTheMatrix()
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (matrix[r][c] == '▒')
                        yield return new Position(r, c);
        }
        //2.2) InGame Boolean Methods
        public bool IsOurHeroHealthZero()
        {
            if (ourHero.Health <= 0)
            {
                Console.WriteLine("Game Over");
                Console.WriteLine("You were slain - Health reached 0");
                Console.WriteLine("Thank you for playing");
                return true;
            }
            return false;
        }
        public bool IsValidMovementLetter(string text)
        {
            HashSet<string> allValid = new HashSet<string>()
            {"W", "S", "D", "A", "E", "X", "Q", "Z" };
            return allValid.Contains(text) || allValid.Contains(text.ToUpper());
        }
        public bool IsOutOfBoundsOfMatrix(int row, int col)
        {
            return row < 0 || row >= rows || col < 0 || col >= cols;
        }
        public bool IsOutOfBoundsOfMatrixPLusMinusOnes(int row, int col)
        {
            return row - 1 < 0 || row + 1 >= rows || col - 1 < 0 || col + 1 >= cols;
        }
        //2.3) InGame Print Methods
        public void PrintAllAvailableMonstersInRange(List<EnemyMonster> allMonsters)
        {
            Console.WriteLine("You can only write an Id of a monster");
            Console.WriteLine("Available targets Id's:");
            foreach (var monster in allMonsters)
                Console.Write($"{monster.Id}, ");
            Console.Write($"\nWrite the Id of the monster you want to attack: ");
        }
        public void PrintAttackMoveConditions()
        {
            Console.WriteLine("You can only press 1 or 2");
            Console.WriteLine("Choose Action");
            Console.WriteLine("Press 1 to Attack");
            Console.WriteLine("Press 2 to Move");
        }
        public void PrintMovementLetters()
        {
            Console.WriteLine("You can only press these characters:");
            Console.WriteLine($"Press W to move Up");
            Console.WriteLine($"Press S to move Down");
            Console.WriteLine($"Press D to move Right");
            Console.WriteLine($"Press A to move Left");
            Console.WriteLine($"Press E to move diagonally Up And Right");
            Console.WriteLine($"Press X to move diagonally Down And Right");
            Console.WriteLine($"Press Q to move diagonally Up And Left");
            Console.WriteLine($"Press Z to move diagonally Down And Left");
        }
        public void PrintMatrixGameState()
        {
            Console.WriteLine($"Health: {ourHero.Health}   Mana: {ourHero.Mana}   Damage:{ourHero.Damage}\n\n");
            foreach (var item in matrix)
                Console.WriteLine(String.Join("", item));
        }


        //1)...Character Select Methods:......
        //1.1) Character Select Helper Action Methods
        public int BuffUpYourStats(int totalPoints, string stat)
        {
            if (totalPoints == 1)
                Console.WriteLine($"You have {totalPoints} point to spend");
            else
                Console.WriteLine($"You have {totalPoints} points to spend");

            Console.WriteLine($"How many points would you like to add to {stat}?");
            int statPoints = totalPoints + 1;
            while (statPoints < 0 || statPoints > totalPoints)
            {
                while (!int.TryParse(Console.ReadLine(), out statPoints))
                    Console.WriteLine($"You can only choose from 0 to {totalPoints}");
                if (statPoints < 0 || statPoints > totalPoints)
                    Console.WriteLine($"You can only choose from 0 to {totalPoints}");
            }
            return statPoints;
        }
        public void SetOurHeroRace(int numCharacter)
        {
            if (numCharacter == 1)
            {
                ourHero = new Warrior();
                characterRace = "Warrior";
            }
            else if (numCharacter == 2)
            {
                ourHero = new Archer();
                characterRace = "Archer";
            }
            else if (numCharacter == 3)
            {
                ourHero = new Mage();
                characterRace = "Mage";
            }
            ourHero.Class = characterRace;
        }
        //1.2) Character Select Print Methods
        public void PrintCharacterStats(string characterType)
        {
            Console.WriteLine("Our Hero's Stats:");
            Console.WriteLine($"Your chosen hero: {characterType}");
            Console.WriteLine($"Strength: {ourHero.Strength}");
            Console.WriteLine($"Agility: {ourHero.Agility}");
            Console.WriteLine($"Intelligence: {ourHero.Intelligence}");
            Console.WriteLine($"Range: {ourHero.Range}");
            Console.WriteLine($"Symbol: {ourHero.Symbol}");
            Console.WriteLine($"Health: {ourHero.Health}");
            Console.WriteLine($"Mana: {ourHero.Mana}");
            Console.WriteLine($"Damage: {ourHero.Damage}");
        }
        public void PrintChooseCharacterConditions()
        {
            Console.WriteLine("You can only press 1, 2 or 3");
            Console.WriteLine("Choose a character type: ");
            Console.WriteLine("Press 1 to choose Warrior");
            Console.WriteLine("Press 2 to choose Archer");
            Console.WriteLine("Press 3 to choose Mage");
        }
    }
}
