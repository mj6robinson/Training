using BaseLib.Config;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
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

        public static bool IsRelicSelected(RelicModel relic)
        {
            return SelectedRelics.Contains(relic.ToString());
        }

        public static void SelectRelic(RelicModel relic, bool selected)
        {
            if (selected && !SelectedRelics.Contains(relic.ToString()))
            {
                SelectedRelics.Add(relic.ToString());
            }
            if (!selected && SelectedRelics.Contains(relic.ToString()))
            {
                SelectedRelics.Remove(relic.ToString());
            }
        }

        public static int GetCardValue(CardModel card)
        {
            return SelectedCards.GetValueOrDefault(card.ToSerializable().ToString());
        }

        public static void SelectCard(CardModel card, int value)
        {            
            SelectedCards[card.ToSerializable().ToString()] = value;
        }
    }
}
