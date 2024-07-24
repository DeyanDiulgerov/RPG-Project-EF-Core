using System;

namespace RPG_Project.CharacterModels
{
    public class EnemyMonster : HeroModel
    {
        private readonly Random random = new Random();
        public EnemyMonster()
        {
            Strength = random.Next(1, 4);
            Agility = random.Next(1, 4);
            Intelligence = random.Next(1, 4);
            Range = 1;
            Symbol = '◙';
            Row = 0;
            Col = 0;
            SetUp();
        }
    }
}
