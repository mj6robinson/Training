using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Training.TrainingCode.Acts;
using Training.TrainingCode.Modifiers;
using static Godot.Control;

namespace Training.TrainingCode.Events
{
    public class TrainingEvent : CustomAncientModel
    {
        private static string ScenePath = "user://training_event.tscn";
        private static PackedScene? PackedScene = null;

        public override string? CustomScenePath
        {
            get
            {
                GenerateScene();
                return ScenePath;
            }
        }

        public override string? CustomMapIconPath => "res://images/packed/map/ancients/ancient_node_neow.png";

        public override string? CustomMapIconOutlinePath => "res://images/packed/map/ancients/ancient_node_neow_outline.png";

        public override string? CustomRunHistoryIconPath => "res://images/ui/run_history/neow.png";

        public override string? CustomRunHistoryIconOutlinePath => "res://images/ui/run_history/neow_outline.png";

        public override IEnumerable<EventOption> AllPossibleOptions => [
            new EventOption(this, BeginTraining, new LocString("events", "TRAINING-BEGIN.title"), new LocString("events", "TRAINING-BEGIN.description"), "BeginTraining", []),
            new EventOption(this, ExitTraining, new LocString("events", "TRAINING-END.title"), new LocString("events", "TRAINING-END.description"), "ExitTraining", [])
            ];

        protected override OptionPools MakeOptionPools => new([]);

        public override bool IsShared => true;

        protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        {
            return [.. AllPossibleOptions];
        }

        public override bool IsValidForAct(ActModel act)
        {
            return act is TrainingAct;
        }

        private Task BeginTraining()
        {            
            EnterCombatWithoutExitingEvent(TrainingModifier.Encounter.MutableClone() as EncounterModel, [], true);
            return Task.CompletedTask;
        }

        private Task ExitTraining()
        {
            return NGame.Instance.ReturnToMainMenu();
        }

        public override Task Resume(AbstractRoom exitedRoom)
        {
            BeforeEventStarted(false);
            SetInitialEventState(false);
            RunManager.Instance.CombatReplayWriter.RecordInitialState(RunManager.Instance.ToSave(null));
            return base.Resume(exitedRoom);
        }

        protected override void OnEventFinished()
        {
            RunManager.Instance.Abandon();
            base.OnEventFinished();            
        }

        private static void GenerateScene()
        {
            if (PackedScene == null)
            {
                var rootNode = new Control();
                rootNode.SetAnchorsPreset(LayoutPreset.FullRect);
                PackedScene = new PackedScene();
                var err = PackedScene.Pack(rootNode);
                if (err == Error.Ok)
                {
                    ResourceSaver.Save(PackedScene, ScenePath);
                }
                else
                {
                    GD.PrintErr("Failed to pack the scene.");
                    ScenePath = "res://scenes/events/background_scenes/neow.tscn";
                }
            }
        }
    }
}
