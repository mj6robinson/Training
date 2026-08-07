using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using Training.TrainingCode.Encounters;
using Training.TrainingCode.Events;

namespace Training.TrainingCode.Acts
{
    public class TrainingAct : CustomActModel
    {
        public TrainingAct() : base(1, false)
        {
        }

        public override IEnumerable<AncientEventModel> AllAncients => [ModelDb.AncientEvent<TrainingEvent>()];
        
        public override IEnumerable<EncounterModel> BossDiscoveryOrder => [Boss];

        public override IEnumerable<EventModel> AllEvents => [];

        public readonly EncounterModel Boss = ModelDb.Encounter<TrainingEncounter>();

        protected override string CustomMapTopBgPath => "res://images/packed/map/map_bgs/glory/map_top_glory.png";

        protected override string CustomMapMidBgPath => "res://images/packed/map/map_bgs/glory/map_middle_glory.png";

        protected override string CustomMapBotBgPath => "res://images/packed/map/map_bgs/glory/map_bottom_glory.png";

        protected override string CustomRestSiteBackgroundPath => "res://images/packed/map/map_bgs/glory/map_top_glory.png";

        public override IEnumerable<EncounterModel> GenerateAllEncounters()
        {
            return [Boss];
        }
        
        public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState)
        {
            return [.. AllAncients];
        }

        public override bool IsUnlocked(UnlockState unlockState)
        {
            return false;
        }

        public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
        {
            return new MapPointTypeCounts(0, 0);
        }
    }
}
