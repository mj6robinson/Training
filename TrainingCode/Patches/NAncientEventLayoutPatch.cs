using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Rooms;
using Training.TrainingCode.Events;
using Training.TrainingCode.Modifiers;
using static Godot.Control;

namespace Training.TrainingCode.Patches
{
    [HarmonyPatch(typeof(NAncientEventLayout))]
    public static class NAncientEventLayoutPatch
    {
        private static Button? CurrentButton;
        private static readonly StyleBoxFlat HighlightStyle = new() { BgColor = Colors.DimGray };

        private static string IconPath(string filename)
        {
            return ImageHelper.GetImagePath("atlases/ui_atlas.sprites/map/icons/" + filename + ".tres");
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(NAncientEventLayout.OnSetupComplete))]
        public static void OnSetupComplete(ref EventModel ____event, ref Control ____contentContainer, ref VBoxContainer ____dialogueContainer)
        {
            if (____event is TrainingEvent trainingEvent)
            {
                ____dialogueContainer.Hide();
                CurrentButton = null;

                var encountersRoot = new VBoxContainer
                {
                    Name = "EncountersRoot",
                    AnchorLeft = 0.01f,
                    AnchorTop = 0.20f,
                    AnchorRight = 0.49f,
                    AnchorBottom = 0.75f
                };
                ____contentContainer.GetParent().AddChild(encountersRoot);

                var encountersFilter = new LineEdit()
                {
                    Name = "EncountersFilter",
                    PlaceholderText = "Filter",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,

                };
                encountersRoot.AddChild(encountersFilter);


                var encountersScrollContainer = new ScrollContainer
                {
                    Name = "EncountersScroll",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ExpandFill,
                    VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                    HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                };
                encountersRoot.AddChild(encountersScrollContainer);

                var encountersContainer = new GridContainer
                {
                    Name = "EncountersContainer",
                    Columns = 3,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };
                encountersScrollContainer.AddChild(encountersContainer);

                encountersFilter.TextChanged += (filter) => Filter(encountersContainer, filter);

                var allEncounters = ModelDb.AllEncounters.Select(encounter => new
                {
                    Title = encounter.Title.GetRawText() + (encounter is BattlewornDummyEventEncounter ? $" ({encounter.AllPossibleMonsters.FirstOrDefault().MinInitialHp} HP)" : ""),
                    Icon = ImageHelper.GetRoomIconPath(ModelDb.EventEncounters.Contains(encounter) ? MapPointType.Unknown : MapPointType.Monster, encounter.RoomType, encounter.RoomType == RoomType.Boss ? encounter.Id : null),
                    Encounter = encounter
                }).ToList();

                var denseVegetation = ModelDb.Encounter<DenseVegetationEventEncounter>();
                allEncounters.Add(new
                {
                    Title = denseVegetation.Title.GetRawText(),
                    Icon = ImageHelper.GetRoomIconPath(MapPointType.Unknown, denseVegetation.RoomType, null),
                    Encounter = denseVegetation as EncounterModel
                });

                var punchOff = ModelDb.Encounter<PunchOffEventEncounter>();
                allEncounters.Add(new
                {
                    Title = punchOff.Title.GetRawText(),
                    Icon = ImageHelper.GetRoomIconPath(MapPointType.Unknown, punchOff.RoomType, null),
                    Encounter = punchOff as EncounterModel
                });

                foreach (var encounter in allEncounters.OrderBy(p => p.Title))
                {
                    var button = new Button
                    {
                        Name = $"{encounter.Title}",
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        SizeFlagsVertical = SizeFlags.ExpandFill,
                        ExpandIcon = true,
                        Icon = ResourceLoader.Load<Texture2D>(encounter.Icon, null, ResourceLoader.CacheMode.Reuse),
                        Text = encounter.Title
                    };
                    encountersContainer.AddChild(button);

                    button.Pressed += () => EncounterClicked(button, encounter.Encounter);
                    if (TrainingModifier.Encounter.Id == encounter.Encounter.Id)
                    {
                        CurrentButton = button;
                        button.AddThemeStyleboxOverride("normal", HighlightStyle);
                    }
                }

                var potionsRoot = new VBoxContainer
                {
                    Name = "PotionsRoot",
                    AnchorLeft = 0.51f,
                    AnchorTop = 0.20f,
                    AnchorRight = 0.99f,
                    AnchorBottom = 0.75f
                };
                ____contentContainer.GetParent().AddChild(potionsRoot);

                var potionsFilter = new LineEdit()
                {
                    Name = "PotionsFilter",
                    PlaceholderText = "Filter",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                potionsRoot.AddChild(potionsFilter);

                var potionScrollContainer = new ScrollContainer
                {
                    Name = "PotionsScroll",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ExpandFill,
                    VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                    HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                };
                potionsRoot.AddChild(potionScrollContainer);

                var potionsContainer = new GridContainer
                {
                    Name = "PotionsContainer",
                    Columns = 10,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };
                potionScrollContainer.AddChild(potionsContainer);
                potionsFilter.TextChanged += (filter) => Filter(potionsContainer, filter);

                foreach (var potion in ModelDb.AllPotions.Where(p => p is not DeprecatedPotion))
                {
                    var button = new Button
                    {
                        Name = $"{potion.Title.GetRawText()}",
                        CustomMinimumSize = Vector2.One * 64,
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        SizeFlagsVertical = SizeFlags.ExpandFill,
                        FocusMode = FocusModeEnum.None
                    };
                    potionsContainer.AddChild(button);

                    var container = new PanelContainer
                    {
                        AnchorsPreset = 15,
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        SizeFlagsVertical = SizeFlags.ExpandFill,
                        MouseFilter = MouseFilterEnum.Ignore
                    };
                    button.AddChild(container);

                    container.AddChild(NPotion.Create(potion));

                    button.Pressed += () => PotionClicked(potion, trainingEvent.Owner);
                    button.MouseEntered += () => OnFocus(button, potion);
                    button.MouseExited += () => OnUnfocus(button);
                }
            }
        }

        public static void EncounterClicked(Button button, EncounterModel encounter)
        {
            CurrentButton?.RemoveThemeStyleboxOverride("normal");
            CurrentButton = button;
            button.AddThemeStyleboxOverride("normal", HighlightStyle);
            TrainingModifier.Encounter = encounter;
        }

        public static void PotionClicked(PotionModel potion, Player? player)
        {
            if (player != null)
            {
                PotionCmd.TryToProcure(potion.ToMutable(), player);
            }
        }

        public static void OnFocus(Button button, PotionModel potion)
        {
            var nHoverTipSet = NHoverTipSet.CreateAndShow(button, potion.HoverTips);
            nHoverTipSet?.SetGlobalPosition(button.GlobalPosition + Vector2.Left * 45f);
            nHoverTipSet?.SetAlignment(button, HoverTipAlignment.Left);
        }

        public static void OnUnfocus(Button button)
        {
            NHoverTipSet.Remove(button);
        }

        public static void Filter(GridContainer container, string filter)
        {
            foreach (var button in container.GetChildrenRecursive<Button>())
            {
                if (button.Name.ToString().Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                {
                    button.Show();
                }
                else
                {
                    button.Hide();
                }
            }
        }
    }
}