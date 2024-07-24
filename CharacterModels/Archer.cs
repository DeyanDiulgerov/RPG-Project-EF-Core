namespace RPG_Project.CharacterModels
{
    public class Archer : HeroModel
    {
        public Archer()
        {
            Strength = 2;
            AddedStrength = 0;
            Agility = 4;
            AddedAgility = 0;
            Intelligence = 0;
            AddedIntelligence = 0;
            Range = 2;
            Symbol = '#';
            SetUp();
        }
    }
}
