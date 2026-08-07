using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;

namespace Training.TrainingCode.Patches
{
    [HarmonyPatch(typeof(NPauseMenu))]
    class NPauseMenuPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(NPauseMenu._Ready))]
        public static void _Ready(ref NPauseMenuButton ____giveUpButton)
        {
            ____giveUpButton.Hide();
        }
    }
}