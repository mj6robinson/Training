using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Runs;

namespace Training.TrainingCode.Patches
{
    [HarmonyPatch(typeof(NPauseMenu))]
    class NPauseMenuPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(NPauseMenu.Initialize))]
        public static void Initialize(IRunState runState, ref NPauseMenuButton ____giveUpButton)
        {
            if (runState.GameMode == GameMode.None)
            {
                ____giveUpButton.Hide();
            }
        }
    }
}