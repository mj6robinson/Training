using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Training.TrainingCode.Modifiers
{
    public class TrainingModifier : CustomModifierModel
    {
        public override bool ClearsPlayerDeck => true;

        public List<Tuple<CardModel, bool>> Cards = [];

        public List<RelicModel> Relics = [];

        public static EncounterModel Encounter = ModelDb.AllEncounters.First();

        protected override string IconPath => $"res://Training/images/modifiers/{Id.Entry.ToLowerInvariant()}.png";

        public override ModifierAlignment Alignment => ModifierAlignment.None;

        protected override void AfterRunCreated(RunState runState)
        {
            foreach (var player in RunState.Players)
            {
                foreach (var card in Cards)
                {
                    var newCard = RunState.CreateCard(card.Item1, player);
                    if (card.Item2) newCard.UpgradeInternal();
                    CardPileCmd.Add(newCard, PileType.Deck);
                }
            }
            base.AfterRunCreated(runState);            
        }

        public override Task AfterActEntered()
        {
            foreach (var player in RunState.Players)
            {
                foreach (var relic in Relics)
                {
                    RelicCmd.Obtain(relic.ToMutable(), player);
                }
            }

            return base.AfterActEntered();
        }

        public override bool ShouldDie(Creature creature)
        {
            return !creature.IsPlayer && base.ShouldDie(creature);
        }

        public override Task AfterPreventingDeath(Creature creature)
        {
            return CombatManager.Instance.EndCombatInternal();
        }

        public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
        {
            rewards.Clear();
            return true;
        }
    }
}
