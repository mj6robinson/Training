using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using Training.TrainingCode.Screens;

namespace Training.TrainingCode.Patches
{
    [HarmonyPatch(typeof(NSingleplayerSubmenu))]
    class NSingleplayerSubmenuPatch
    {
        private static NSubmenuButton? _trainingButton;
        private static NSingleplayerSubmenu? instance;
        private static readonly AccessTools.FieldRef<NSubmenu, NSubmenuStack> getStack = AccessTools.FieldRefAccess<NSubmenu, NSubmenuStack>("_stack");


        private const string _keyTraining = "TRAINING";

        [HarmonyPostfix]
        [HarmonyPatch(nameof(NSingleplayerSubmenu._Ready))]
        public static void _Ready(NSingleplayerSubmenu __instance, ref NSubmenuButton ____standardButton, ref NSubmenuButton ____dailyButton, ref NSubmenuButton ____customButton)
        {
            instance = __instance;
            _trainingButton = ____customButton.Duplicate() as NSubmenuButton;
            if (_trainingButton != null)
            {
                _trainingButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OpenTrainingScreen));
                var trainingIcon = _trainingButton.GetNode<TextureRect>("Icon");
                trainingIcon.Texture = GD.Load<Texture2D>("res://Training/images/image.png");
                var _bgPanel = _trainingButton.GetNode<Control>("BgPanel");
                var _hsv = _bgPanel.Material.Duplicate() as ShaderMaterial;

                _hsv?.SetShaderParameter("h", 120);
                _hsv?.SetShaderParameter("s", 0.4f);
                _hsv?.SetShaderParameter("v", 0.5f);
                _bgPanel.Material = _hsv;

                ____standardButton.Position += new Vector2(-200, 0);
                ____dailyButton.Position += new Vector2(-200, 0);
                ____customButton.Position += new Vector2(-200, 0);
                _trainingButton.Position += new Vector2(200, 0);
                ____customButton.GetParent().AddChild(_trainingButton);
                _trainingButton.SetIconAndLocalization(_keyTraining);
            }
        }

        private static void OpenTrainingScreen(NButton _)
        {
            var stack = getStack(instance);
            var submenuType = stack.GetSubmenuType<NTrainingRunScreen>();
            submenuType.InitializeSingleplayer();
            stack.Push(submenuType);
        }
    }
}