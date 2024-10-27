using System.Text.Json;
using System.Text.Json.Serialization;

namespace tft_trait_tracker
{
    public class Program
    {
        private static List<Champion>? allChampions;
        private static List<Trait>? allTraits;

        public static void Main(string[] args)
        {
            LoadData();

            int minCost = 1;
            int maxCost = 5;
            int maxNumChampions = 7;
            int minTraits = 7;
            var currentChampions = new List<string>();
            var emblemTraits = new List<string>();

            var validCompositions = FindCompositions(minCost, maxCost, maxNumChampions, minTraits, currentChampions, emblemTraits);

            foreach (var composition in validCompositions)
            {
                Console.WriteLine($"Champions: {string.Join(", ", composition.Champions!.Select(c => c.Name))} - Traits: {string.Join(", ", composition.Traits!)}");
            }
        }

        private static void LoadData()
        {
            var json = File.ReadAllText("set12.json");
            var setData = JsonSerializer.Deserialize<JsonFile>(json, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            allChampions = setData!.Champions;
            allTraits = setData!.Traits;
        }

        private static bool IsTraitActive(int traitCount, string traitName, List<string> emblemTraits)
        {
            var trait = allTraits!.FirstOrDefault(t => t.Name == traitName);
            if (trait == null || trait.Levels!.SequenceEqual(new List<int> { 1 })) return false;

            int requiredCount = trait.Levels!.First() - (emblemTraits.Contains(traitName) ? 1 : 0);
            return traitCount >= requiredCount;
        }

        private static List<Composition> SortCompositionsByCost(List<Composition> compositions)
        {
            return compositions.OrderBy(c => c.Champions!.Sum(champ => champ.Cost)).ToList();
        }

        private static List<Composition> FindCompositions(int minCost, int maxCost, int maxNumChampions, int minTraits, List<string> currentChampionNames, List<string> emblemTraits)
        {
            var filteredChampions = allChampions!.Where(champ => champ.Cost >= minCost && champ.Cost <= maxCost).ToList();
            var requiredChampions = allChampions!.Where(champ => currentChampionNames.Contains(champ.Name!)).ToList();
            var remainingChampions = filteredChampions.Where(champ => !currentChampionNames.Contains(champ.Name!)).ToList();

            int maxRemainingChampions = maxNumChampions - requiredChampions.Count;

            Console.WriteLine($"Amount of champions: {filteredChampions.Count}");
            Console.WriteLine($"Max number of champions: {maxRemainingChampions}");
            Console.WriteLine($"Complexity: {MathNet.Numerics.SpecialFunctions.Binomial(filteredChampions.Count, maxRemainingChampions)}");

            var validCompositions = new List<Composition>();

            for (int num = 1; num <= maxRemainingChampions; num++)
            {
                foreach (var combo in GetCombinations(remainingChampions, num))
                {
                    var fullCombo = requiredChampions.Concat(combo).ToList();
                    var traitCounts = new Dictionary<string, int>();

                    foreach (var champ in fullCombo)
                    {
                        foreach (var trait in champ.Traits!)
                        {
                            if (traitCounts.ContainsKey(trait))
                                traitCounts[trait]++;
                            else
                                traitCounts[trait] = 1;
                        }
                    }

                    var activeTraits = traitCounts.Where(tc => IsTraitActive(tc.Value, tc.Key, emblemTraits)).Select(tc => tc.Key).ToList();

                    if (activeTraits.Count >= minTraits)
                    {
                        validCompositions.Add(new Composition { Champions = fullCombo, Traits = activeTraits });
                    }
                }
            }

            return SortCompositionsByCost(validCompositions);
        }

        private static IEnumerable<IEnumerable<T>> GetCombinations<T>(List<T> list, int length)
        {
            if (length == 0) yield return new List<T>();
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    foreach (var tailCombo in GetCombinations(list.Skip(i + 1).ToList(), length - 1))
                    {
                        yield return new List<T> { list[i] }.Concat(tailCombo);
                    }
                }
            }
        }
    }

    public class Champion
    {
        public string? Name { get; set; }
        public int Cost { get; set; }
        public List<string>? Traits { get; set; }
    }

    public class Trait
    {
        public string? Name { get; set; }
        public List<int>? Levels { get; set; }
    }

    public class Composition
    {
        public List<Champion>? Champions { get; set; }
        public List<string>? Traits { get; set; }
    }

    public class JsonFile
    {
        public List<Champion>? Champions { get; set; }
        public List<Trait>? Traits { get; set; }
    }
}
