using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace Training.TrainingCode.Patches
{
    [HarmonyPatch(typeof(ImageHelper))]
    public static class ImageHelperPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ImageHelper.GetImagePath))]
        public static bool GetImagePath(string innerPath, ref string __result)
        {
            var path = $"res://Training/images/{innerPath}";
            if (ResourceLoader.Exists(path))
            {
                __result = path;
                return false;
            }
            return true;
        }
    }
}