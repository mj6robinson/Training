using BaseLib.Config;
using MegaCrit.Sts2.Core.Logging;
using System.Text.Json;

namespace Training.TrainingCode.Config
{
    public class TrainingConfig : SimpleModConfig
    {
        public static string Relics
        {
            get
            {
                try
                {
                    return JsonSerializer.Serialize(SelectedRelics);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to serialize relics: {ex}");
                }
                return string.Empty;
                
            }
            set
            {
                try
                {
                    SelectedRelics = JsonSerializer.Deserialize<List<string>>(value) ?? [];
                }
                catch (Exception ex)
                {
                    SelectedRelics = [];
                    Log.Warn($"Failed to deserialize relics: {ex}");
                }
                
            }
        }

        public static string Cards
        {
            get
            {
                try
                {
                    return JsonSerializer.Serialize(SelectedCards);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to serialize cards: {ex}");
                }
                return string.Empty;
            }
            set
            {
                try
                {
                    SelectedCards = JsonSerializer.Deserialize<Dictionary<string, int>>(value) ?? [];
                }
                catch (Exception ex)
                {
                    SelectedCards = [];
                    Log.Warn($"Failed to deserialize cards: {ex}");
                }
            }
        }

        private static List<string> SelectedRelics = [];

        private static Dictionary<string, int> SelectedCards = [];

        public static bool IsRelicSelected(string relic)
        {
            return SelectedRelics.Contains(relic);
        }

        public static void SelectRelic(string relic, bool selected)
        {
            if (selected && !SelectedRelics.Contains(relic))
            {
                SelectedRelics.Add(relic);
            }
            if (!selected && SelectedRelics.Contains(relic))
            {
                SelectedRelics.Remove(relic);
            }
        }

        public static int GetCardValue(string card)
        {
            return SelectedCards.GetValueOrDefault(card);
        }

        public static void SelectCard(string card, int value)
        {
            SelectedCards[card] = value;
        }
    }
}
