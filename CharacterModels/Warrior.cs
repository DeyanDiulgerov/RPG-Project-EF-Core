namespace RPG_Project.CharacterModels
{
    public class Warrior : HeroModel
    {
        public Warrior()
        {
            Strength = 3;
            Agility = 3;
            Intelligence = 0;
            Range = 1;
            Symbol = '@';
            SetUp();
        }
    }
}
