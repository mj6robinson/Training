using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using Training.TrainingCode.Screens;

namespace Training.TrainingCode.Patches
{
    [HarmonyPatch(typeof(NMainMenuSubmenuStack))]
    class NMainMenuSubmenuStackPatch
    {
        private static NTrainingRunScreen? _customRunScreen;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(NMainMenuSubmenuStack.GetSubmenuType), [typeof(Type)])]
        public static bool GetSubmenuType(Type type, ref NMainMenuSubmenuStack __instance, ref NSubmenu __result)
        {
            if (type == typeof(NTrainingRunScreen))
            {
                _customRunScreen?.Dispose();

                _customRunScreen = NTrainingRunScreen.Create();
                _customRunScreen.Visible = false;
                __instance.AddChildSafely(_customRunScreen);

                __result = _customRunScreen;
                return false;
            }
            return true;
        }
    }
}