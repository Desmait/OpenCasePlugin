using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json;

namespace OpenCasePlugin
{
    public class OpenCasePlugin : BasePlugin
    {
        public override string ModuleName => "Open Case Plugin";
        public override string ModuleVersion => "1.0.1";

        private string JsonDirectoryPath => Path.Combine(ModuleDirectory, "json");

        private static readonly string[] CaseNames = new string[]
        {
            "Gallery", "Kilowatt", "Revolution", "Recoil", "Dreams & Nightmares", "Operation Riptide", "Snakebite",
            "Operation Broken Fang", "Fracture", "Prisma 2", "Shattered Web", "CS20", "Prisma", "Danger Zone", "Clutch", 
            "Spectrum 2", "Operation Hydra", "Spectrum", "Glove", "Gamma 2", "Gamma", "Chroma 3", "Operation Wildfire",
            "Revolver", "Shadow", "Falchion", "Chroma 2", "Chroma", "Operation Vanguard", "Operation Breakout",
            "Huntsman Weapon", "Operation Phoenix", "CS:GO Weapon Case 3", "Winter Offensive", "eSports 2013 Winter",
            "CS:GO Weapon Case 2", "Operation Bravo", "CS:GO Weapon Case", "eSports 2013"
        };

        private static readonly Dictionary<string, string> RarityColors = new Dictionary<string, string>
        {
            { "Mil-Spec Skins", $"{ChatColors.Blue}" },
            { "Restricted Skins", $"{ChatColors.Purple}" },
            { "Classified Skins", $"{ChatColors.Magenta}" },
            { "Covert Skins", $"{ChatColors.Red}" },
            { "Rare Special Items", $"{ChatColors.Gold}" }
        };

        private static readonly Dictionary<string, double> RarityProbabilities = new Dictionary<string, double>
        {
            { "Mil-Spec Skins", 79.92 },
            { "Restricted Skins", 15.98 },
            { "Classified Skins", 3.2 },
            { "Covert Skins", 0.64 },
            { "Rare Special Items", 0.26 }
        };

        public override void Load(bool hotReload)
        {
            base.Load(hotReload);

            Directory.CreateDirectory(JsonDirectoryPath);
        }

        [ConsoleCommand("css_case", "Case commands")]
        public void OnCaseCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;

            string[] args = command.ArgString.Split(' ');
            if (args.Length == 0)
            {
                string[] helpMessages = new string[]
                {
                    "Available commands:",
                    "!case list - displays the list of cases.",
                    "!case open [CASE_NAME] - opens a specific case."
                };

                foreach (var message in helpMessages)
                {
                    player.PrintToChat(message);
                }

                return;
            }

            switch (args[0].ToLower())
            {
                case "list":
                    string caseListMessage = string.Join("\n", CaseNames.Select(caseName => $"- \x04{caseName}"));
                    string[] caseListMessages = caseListMessage.Split('\n');

                    player.PrintToChat("Available cases:");
                    foreach (var message in caseListMessages)
                    {
                        player.PrintToChat(message);
                    }
                    break;

                case "open":
                    if (args.Length < 2)
                    {
                        player.PrintToChat("Usage: !case open \x04[CASE_NAME]");
                        return;
                    }

                    string caseName = string.Join(" ", args.Skip(1));
                    var drop = GetRandomDrop(caseName);

                    if (drop != null)
                    {
                        string colorCode = RarityColors.TryGetValue(drop.Rarity, out var color) ? color : "\x01";
                        Server.PrintToChatAll($" \x09 • \x04{player.PlayerName}\x01 has opened a container and found: {colorCode}{drop.name}");
                    }
                    else
                    {
                        player.PrintToChat("\x02Something went wrong while opening the case.");
                    }
                    break;

                default:
                    player.PrintToChat("Unknown command. Use '!case list' or '!case open [CASE_NAME]'.");
                    break;
            }
        }

        private CaseItem? GetRandomDrop(string caseName)
        {
            try
            {
                string jsonFileName = CaseNameToJson(caseName);
                string filePath = Path.Combine(JsonDirectoryPath, jsonFileName);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"JSON file not found: {filePath}");
                    return null;
                }

                string jsonString = File.ReadAllText(filePath);
                var caseData = JsonSerializer.Deserialize<CaseData>(jsonString);

                if (caseData?.content == null)
                {
                    Console.WriteLine($"Invalid case data for: {caseName}");
                    return null;
                }

                string selectedRarity = GetRandomRarity();
                if (!caseData.content.TryGetValue(selectedRarity, out var items) || items.Count == 0)
                {
                    Console.WriteLine($"No items found for rarity: {selectedRarity}");
                    return null;
                }

                var randomItem = items[new Random().Next(items.Count)];

                if (randomItem.can_be_stattrak && new Random().Next(10) == 0)
                {
                    randomItem.name = selectedRarity == "Rare Special Items" && randomItem.name.StartsWith("★")
                        ? $"★ StatTrak™{randomItem.name[1..]}"
                        : $"StatTrak™ {randomItem.name}";
                }

                return new CaseItem { name = randomItem.name, Rarity = selectedRarity };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRandomDrop: {ex.Message}");
                return null;
            }
        }

        private string GetRandomRarity()
        {
            double random = new Random().NextDouble() * 100;
            double cumulativeProbability = 0;

            foreach (var rarity in RarityProbabilities)
            {
                cumulativeProbability += rarity.Value;
                if (random <= cumulativeProbability) return rarity.Key;
            }

            return RarityProbabilities.Keys.Last();
        }

        private string CaseNameToJson(string caseName)
        {
            return $"{caseName.ToLower().Replace(" ", "_")}_case.json";
        }
    }

    public class CaseData
    {
        public string name { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public Dictionary<string, List<SkinItem>> content { get; set; } = new Dictionary<string, List<SkinItem>>();
    }

    public class SkinItem
    {
        public string name { get; set; } = "";
        public bool can_be_stattrak { get; set; }
    }

    public class CaseItem
    {
        public string name { get; set; } = "";
        public string Rarity { get; set; } = "";
    }
}