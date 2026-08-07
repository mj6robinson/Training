using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace Training.TrainingCode.Map
{
    
    internal class TrainingMap : ActMap
    {
        
        public override MapPoint BossMapPoint { get; }

        public override MapPoint StartingMapPoint { get; }

        protected override MapPoint?[,] Grid { get; }

        public TrainingMap()
        {
            Grid = new MapPoint[0, 1];
            BossMapPoint = new MapPoint(0, 1)
            {
                PointType = MapPointType.Boss
            };
            StartingMapPoint = new MapPoint(0, 0)
            {
                PointType = MapPointType.Ancient
            };
        }

    }
}
