using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Training.TrainingCode.Acts;
using Training.TrainingCode.Modifiers;

namespace Training.TrainingCode.Encounters
{
    public class TrainingEncounter : CustomEncounterModel
    {
        public TrainingEncounter() : base(RoomType.Boss, false)
        {
        }

        public override string BossNodePath => "res://Training/images/" + Id.Entry.ToLowerInvariant();

        public override IEnumerable<MonsterModel> AllPossibleMonsters => [];

        public override bool IsValidForAct(ActModel act)
        {
            return act is TrainingAct;
        }

        protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
        {
            return [];
        }

        public override Task AfterCombatVictory(CombatRoom room)
        {
            return base.AfterCombatVictory(room);
        }
    }
}