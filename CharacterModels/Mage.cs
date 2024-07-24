namespace RPG_Project.CharacterModels
{
    public class Mage : HeroModel
    {
        public Mage()
        {
            Strength = 2;
            Agility = 1;
            Intelligence = 3;
            Range = 3;
            Symbol = '*';
            SetUp();
        }
    }
}
