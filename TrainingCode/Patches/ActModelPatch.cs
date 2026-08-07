using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using Training.TrainingCode.Acts;
using Training.TrainingCode.Map;

namespace Training.TrainingCode.Patches
{
    [HarmonyPatch(typeof(ActModel))]
    class ActModelPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ActModel.CreateMap))]
        public static bool CreateMap(ref ActModel __instance, ref ActMap __result)
        {
            if (__instance is TrainingAct)
            {
                __result = new TrainingMap();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(FilePathIdentifier), MethodType.Getter)]
        public static bool FilePathIdentifier(ref ActModel __instance, ref string __result)
        {
            if (__instance is TrainingAct)
            {
                __result = "glory";
                return false;
            }
            return true;
        }
    }
}