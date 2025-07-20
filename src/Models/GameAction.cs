using System;

namespace GameAutomation.Models
{
    public class GameAction
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public TimeSpan Cooldown { get; set; }
        public Action<GameWindow> Execute { get; set; }
        public bool IsFormTransformation { get; set; } = false; // Mark form transformation spells
        public bool RequiresAnimalForm { get; set; } = false; // Mark spells that require animal form
        public bool RequiresHumanForm { get; set; } = false; // Mark spells that require human form
        
        public GameAction(string name, string displayName, TimeSpan cooldown, Action<GameWindow> execute)
        {
            Name = name;
            DisplayName = displayName;
            Cooldown = cooldown;
            Execute = execute;
        }
    }
    
    public static class GameActions
    {
        // Universal action for all classes
        public static GameAction AutoAttack { get; } = new GameAction(
            "AutoAttack",
            "Атака",
            TimeSpan.Zero, // No cooldown
            null! // Will be set by the UI
        );
        
        public static GameAction TpOut { get; } = new GameAction(
            "TpOut",
            "TP Out",
            TimeSpan.FromMinutes(5),
            null! // Will be set by the UI
        );
        
        // Shaman-specific actions
        public static GameAction ShamanStun { get; } = new GameAction(
            "ShamanStun",
            "Стан",
            TimeSpan.FromSeconds(20),
            null! // Will be set by the UI
        );
        
        public static GameAction ShamanImmunity { get; } = new GameAction(
            "ShamanImmunity",
            "ф имун",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        );
        
        public static GameAction ShamanHeal { get; } = new GameAction(
            "ShamanHeal",
            "Хил",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        );
        
        public static GameAction ShamanDetarget { get; } = new GameAction(
            "ShamanDetarget",
            "Детаргет",
            TimeSpan.FromMinutes(5),
            null! // Will be set by the UI
        );
        
        // Form transformation actions
        public static GameAction DruidForm { get; } = new GameAction(
            "DruidForm",
            "Форма",
            TimeSpan.FromSeconds(6),
            null! // Will be set by the UI
        )
        {
            IsFormTransformation = true
        };
        
        public static GameAction TankForm { get; } = new GameAction(
            "TankForm",
            "Форма",
            TimeSpan.FromSeconds(2),
            null! // Will be set by the UI
        )
        {
            IsFormTransformation = true
        };
        
        public static GameAction PriestForm { get; } = new GameAction(
            "PriestForm",
            "Форма",
            TimeSpan.FromSeconds(15),
            null! // Will be set by the UI
        )
        {
            IsFormTransformation = true
        };
        
        // Priest-specific spells
        public static GameAction PriestBeam { get; } = new GameAction(
            "PriestBeam",
            "Луч",
            TimeSpan.FromSeconds(10),
            null! // Will be set by the UI
        )
        {
            RequiresAnimalForm = true // Animal form only
        };
        
        public static GameAction PriestSeal { get; } = new GameAction(
            "PriestSeal",
            "Печать",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        )
        {
            RequiresAnimalForm = false // Any form
        };
        
        public static GameAction PriestSleep { get; } = new GameAction(
            "PriestSleep",
            "Слип",
            TimeSpan.FromSeconds(40),
            null! // Will be set by the UI
        )
        {
            RequiresAnimalForm = false // Any form
        };
        
        public static GameAction PriestDebuff { get; } = new GameAction(
            "PriestDebuff",
            "Дебаф",
            TimeSpan.FromSeconds(3),
            null! // Will be set by the UI
        )
        {
            RequiresAnimalForm = false // Any form
        };
        
        public static GameAction PriestHeal { get; } = new GameAction(
            "PriestHeal",
            "Хил",
            TimeSpan.Zero, // No cooldown
            null! // Will be set by the UI
        )
        {
            RequiresHumanForm = true // Human form only
        };
        
        // Tank-specific spells
        public static GameAction TankStun { get; } = new GameAction(
            "TankStun",
            "Стан",
            TimeSpan.FromSeconds(6),
            null! // Will be set by the UI
        )
        {
            RequiresHumanForm = true // Human form only
        };
        
        public static GameAction TankAgro { get; } = new GameAction(
            "TankAgro",
            "Агр",
            TimeSpan.FromSeconds(3),
            null! // Will be set by the UI
        )
        {
            RequiresAnimalForm = true // Animal form only
        };
        
        public static GameAction TankChi { get; } = new GameAction(
            "TankChi",
            "Чи",
            TimeSpan.Zero, // No cooldown
            null! // Will be set by the UI
        )
        {
            RequiresHumanForm = true // Human form only
        };
        
        // Druid-specific spells
        public static GameAction DruidWound { get; } = new GameAction(
            "DruidWound",
            "Рана",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        )
        {
            RequiresAnimalForm = true // Animal form only
        };
        
        public static GameAction DruidClear { get; } = new GameAction(
            "DruidClear",
            "Чистка",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        )
        {
            RequiresAnimalForm = true // Animal form only
        };
        
        public static GameAction DruidParasitAnimal { get; } = new GameAction(
            "DruidParasitAnimal",
            "Паразит 4",
            TimeSpan.FromSeconds(20),
            null! // Will be set by the UI
        )
        {
            RequiresAnimalForm = true // Animal form only
        };
        
        public static GameAction DruidParasitHuman { get; } = new GameAction(
            "DruidParasitHuman",
            "Паразит 2",
            TimeSpan.FromSeconds(20),
            null! // Will be set by the UI
        )
        {
            RequiresHumanForm = true // Human form only
        };
        
        public static GameAction DruidStun { get; } = new GameAction(
            "DruidStun",
            "Стан",
            TimeSpan.FromSeconds(12),
            null! // Will be set by the UI
        )
        {
            RequiresHumanForm = true // Human form only
        };
        
        public static GameAction DruidSara { get; } = new GameAction(
            "DruidSara",
            "Сара",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        )
        {
            RequiresHumanForm = true // Human form only
        };
        
        // Seeker-specific spells
        public static GameAction SeekerSeal { get; } = new GameAction(
            "SeekerSeal",
            "Печать",
            TimeSpan.FromSeconds(15),
            null! // Will be set by the UI
        );
        
        public static GameAction SeekerSpark { get; } = new GameAction(
            "SeekerSpark",
            "Запал",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        );
        
        public static GameAction SeekerStun { get; } = new GameAction(
            "SeekerStun",
            "Стан",
            TimeSpan.FromSeconds(90),
            null! // Will be set by the UI
        );
        
        public static GameAction SeekerRefresh { get; } = new GameAction(
            "SeekerRefresh",
            "Откат",
            TimeSpan.FromMinutes(3),
            null! // Will be set by the UI
        );
        
        public static GameAction SeekerDisarm { get; } = new GameAction(
            "SeekerDisarm",
            "Дизарм",
            TimeSpan.FromSeconds(15),
            null! // Will be set by the UI
        );
        
        // Assassin-specific spells
        public static GameAction AssassinOtvod { get; } = new GameAction(
            "AssassinOtvod",
            "Отвод",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        );
        
        public static GameAction AssassinStun { get; } = new GameAction(
            "AssassinStun",
            "Стан",
            TimeSpan.FromSeconds(180),
            null! // Will be set by the UI
        );
        
        public static GameAction AssassinPrison { get; } = new GameAction(
            "AssassinPrison",
            "Тюрма",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        );
        
        public static GameAction AssassinSalo { get; } = new GameAction(
            "AssassinSalo",
            "Сало",
            TimeSpan.FromSeconds(8),
            null! // Will be set by the UI
        );
        
        public static GameAction AssassinSleep { get; } = new GameAction(
            "AssassinSleep",
            "Слип",
            TimeSpan.FromSeconds(15),
            null! // Will be set by the UI
        );
        
        public static GameAction AssassinChi { get; } = new GameAction(
            "AssassinChi",
            "Чи",
            TimeSpan.FromSeconds(60),
            null! // Will be set by the UI
        );
        
        public static GameAction AssassinObman { get; } = new GameAction(
            "AssassinObman",
            "Обман",
            TimeSpan.FromSeconds(180),
            null! // Will be set by the UI
        );
        
        public static GameAction AssassinPtp { get; } = new GameAction(
            "AssassinPtp",
            "ПТП",
            TimeSpan.FromSeconds(90),
            null! // Will be set by the UI
        );
        
        // Warrior-specific spells
        public static GameAction WarriorDraki { get; } = new GameAction(
            "WarriorDraki",
            "Драки",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        );
        
        public static GameAction WarriorParal { get; } = new GameAction(
            "WarriorParal",
            "Парал",
            TimeSpan.FromSeconds(15),
            null! // Will be set by the UI
        );
        
        public static GameAction WarriorStun { get; } = new GameAction(
            "WarriorStun",
            "Стан",
            TimeSpan.FromSeconds(15),
            null! // Will be set by the UI
        );
        
        public static GameAction WarriorDisarm { get; } = new GameAction(
            "WarriorDisarm",
            "Дизарм",
            TimeSpan.FromSeconds(60),
            null! // Will be set by the UI
        );
        
        public static GameAction WarriorAlmaz { get; } = new GameAction(
            "WarriorAlmaz",
            "Аура",
            TimeSpan.FromMinutes(10),
            null! // Will be set by the UI
        );
        
        // Mage-specific spells
        public static GameAction MageDebuf { get; } = new GameAction(
            "MageDebuf",
            "Дебаф",
            TimeSpan.FromSeconds(1),
            null! // Will be set by the UI
        );
        
        public static GameAction MageSalo { get; } = new GameAction(
            "MageSalo",
            "Сало",
            TimeSpan.FromSeconds(20),
            null! // Will be set by the UI
        );
        
        public static GameAction MageStun { get; } = new GameAction(
            "MageStun",
            "Гора",
            TimeSpan.FromSeconds(30),
            null! // Will be set by the UI
        );
        
        public static GameAction MagePoison { get; } = new GameAction(
            "MagePoison",
            "Яд",
            TimeSpan.FromSeconds(1),
            null! // Will be set by the UI
        );
        
        // Archer-specific spells
        public static GameAction ArcherStun { get; } = new GameAction(
            "ArcherStun",
            "Стан",
            TimeSpan.FromSeconds(15),
            null! // Will be set by the UI
        );
        
        public static GameAction ArcherParal { get; } = new GameAction(
            "ArcherParal",
            "Рут",
            TimeSpan.FromSeconds(15),
            null! // Will be set by the UI
        );
        
        public static GameAction ArcherBlood { get; } = new GameAction(
            "ArcherBlood",
            "Кровь",
            TimeSpan.FromMinutes(2),
            null! // Will be set by the UI
        );
        
        public static GameAction ArcherRoscol { get; } = new GameAction(
            "ArcherRoscol",
            "Роскол",
            TimeSpan.FromSeconds(15),
            null! // Will be set by the UI
        );
    }
}