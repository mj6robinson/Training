using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Runs;
using Training.TrainingCode.Map;

namespace Training.TrainingCode.Patches
{
    [HarmonyPatch(typeof(NRewardsScreen))]
    class NRewardsScreenPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(OnProceedButtonPressed))]
        public static bool OnProceedButtonPressed(NButton _, ref IRunState ____runState)
        {
            if(____runState.Map is TrainingMap)
            {
                TaskHelper.RunSafely(RunManager.Instance.ProceedFromTerminalRewardsScreen());
                return false;
            }
            return true;
        }
    }
}