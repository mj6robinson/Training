using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Training.TrainingCode.Events;
using static Godot.Control;

namespace Training.TrainingCode.Patches
{
    [HarmonyPatch(typeof(EventModel))]
    class EventModelPatch
    {
        private static string ScenePath = "user://training_event.tscn";
        private static PackedScene? PackedScene = null;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(BackgroundScenePath), MethodType.Getter)]
        public static bool BackgroundScenePath(ref EventModel __instance, ref string __result)
        {
            if (__instance is TrainingEvent)
            {
                GenerateScene();
                __result = "res://scenes/rooms/event_room.tscn";
                return false;
            }
            return true;
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
                    ScenePath = "res://scenes/rooms/event_room.tscn";
                }
            }
        }
    }
}