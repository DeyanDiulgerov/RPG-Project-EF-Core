using System;
using System.ComponentModel.DataAnnotations;

namespace RPG_Project.CharacterModels
{
    public class HeroModel
    {
        [Key]
        public int Id { get; set; }
        public string? Class { get; set; }
        public int Strength { get; set; }
        public int AddedStrength { get; set; }
        public int Agility { get; set; }
        public int AddedAgility { get; set; }
        public int Intelligence { get; set; }
        public int AddedIntelligence { get; set; }
        public int Range { get; set; }
        public char Symbol { get; set; }
        public int Health { get; set; }
        public int Mana { get; set; }
        public int Damage { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        public DateTime TimeOfCreation { get; set; }

        public virtual void SetUp()
        {
            this.Health = this.Strength * 5;
            this.Mana = this.Intelligence * 3;
            this.Damage = this.Agility * 2;
        }
    }
}
