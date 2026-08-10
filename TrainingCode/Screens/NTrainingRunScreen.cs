using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using System.Globalization;
using Training.TrainingCode.Acts;
using Training.TrainingCode.Config;
using Training.TrainingCode.Modifiers;

namespace Training.TrainingCode.Screens
{
    public partial class NTrainingRunScreen : NSubmenu, IStartRunLobbyListener, ICharacterSelectButtonDelegate
    {
        public static TrainingConfig Config = new();

        private static readonly string _scenePath = SceneHelper.GetScenePath("screens/custom_run/custom_run_screen");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private MegaLabel _disclaimer;

        private NCharacterSelectButton? _selectedButton;

        private Control _charButtonContainer;

        private NConfirmButton _confirmButton;

        private NBackButton _backButton;

        private NAscensionPanel _ascensionPanel;

        private LineEdit _relicsFilterBox;

        private GridContainer _relicsContainer;

        private NSearchBar _searchBar;

        private NCardPoolFilter _ironcladFilter;

        private NCardPoolFilter _silentFilter;

        private NCardPoolFilter _defectFilter;

        private NCardPoolFilter _regentFilter;

        private NCardPoolFilter _necrobinderFilter;

        private NCardPoolFilter _colorlessFilter;

        private NCardPoolFilter _ancientsFilter;

        private NCardPoolFilter _miscPoolFilter;

        private NCardViewSortButton _typeSorter;

        private NCardTypeTickbox _attackFilter;

        private NCardTypeTickbox _skillFilter;

        private NCardTypeTickbox _powerFilter;

        private NCardTypeTickbox _otherTypeFilter;

        private NCardViewSortButton _raritySorter;

        private NCardRarityTickbox _commonFilter;

        private NCardRarityTickbox _uncommonFilter;

        private NCardRarityTickbox _rareFilter;

        private NCardRarityTickbox _otherFilter;

        private NCardViewSortButton _costSorter;

        private NCardCostTickbox _zeroFilter;

        private NCardCostTickbox _oneFilter;

        private NCardCostTickbox _twoFilter;

        private NCardCostTickbox _threePlusFilter;

        private NCardCostTickbox _xFilter;

        private NCardViewSortButton _alphabetSorter;

        private NLibraryStatTickbox _viewMultiplayerCards;

        protected List<CardModel> _cards = ModelDb.AllCards.Select(card => card.ToMutable()).Union(ModelDb.AllCards.Where(card => card.IsUpgradable).Select(card => { var upgrade = card.ToMutable(); upgrade.UpgradeInternal(); return upgrade; })).ToList();

        private readonly Dictionary<Type, List<Func<CardModel, bool>>> _filter = [];

        private GridContainer _cardsContainer;

        private StartRunLobby _lobby;

        private MultiplayerUiMode _uiMode;

        public StartRunLobby Lobby => _lobby;

        private static int GetCardRarityComparisonValue(CardModel a)
        {
            if (a.Rarity <= CardRarity.Ancient)
            {
                return (int)a.Rarity;
            }

            return a.Rarity switch
            {
                CardRarity.Status => 6,
                CardRarity.Curse => 7,
                CardRarity.Event => 8,
                CardRarity.Quest => 9,
                CardRarity.Token => 10,
                _ => throw new ArgumentOutOfRangeException(nameof(a), a, null),
            };
        }

        protected override Control InitialFocusedControl => _charButtonContainer.GetChild<Control>(0);
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public static NTrainingRunScreen? Create()
        {
            if (TestMode.IsOn)
            {
                return null;
            }

            Config.Load();

            var customRun = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NCustomRunScreen>(PackedScene.GenEditState.Disabled);

            RemoveNode(customRun, "LeftContainer/SeedContainer");
            RemoveNode(customRun, "LeftContainer/RemotePlayerContainer");
            RemoveNode(customRun, "RightContainer/HBoxContainer");
            RemoveNode(customRun, "RightContainer/ModifiersList");
            RemoveNode(customRun, "ReadyAndWaitingPanel");

            var relicsFilter = new LineEdit()
            {
                Name = "RelicsFilter",
                AnchorLeft = 0,
                AnchorRight = 1,
                AnchorTop = 0,
                OffsetTop = 200,
                OffsetLeft = 50,
                OffsetRight = -200,
                PlaceholderText = "Filter"
            };
            customRun.GetNode("LeftContainer").AddChild(relicsFilter);

            var relicsScrollContainer = new ScrollContainer
            {
                Name = "RelicsScroll",
                AnchorLeft = 0,
                AnchorTop = 0.5f,
                AnchorRight = 0,
                AnchorBottom = 0.5f,
                OffsetLeft = 50,
                OffsetTop = -300,
                OffsetRight = 0,
                OffsetBottom = 50,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };
            customRun.GetNode("LeftContainer").AddChild(relicsScrollContainer);

            var relicsContainer = new GridContainer
            {
                Name = "RelicsContainer",
                Columns = 10,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            relicsScrollContainer.AddChild(relicsContainer);

            var cardBox = new HBoxContainer()
            {
                Name = "CardBox",
                AnchorLeft = 0,
                AnchorTop = 0,
                AnchorBottom = 1,
                AnchorRight = 1,
                OffsetLeft = -50,
                OffsetTop = 25,
                OffsetRight = -50,
                OffsetBottom = -175,
            };
            customRun.GetNode("RightContainer").AddChild(cardBox);

            var cardsScrollContainer = new ScrollContainer
            {
                Name = "CardsScroll",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };
            cardBox.AddChild(cardsScrollContainer);

            var cardLibrary = NCardLibrary.Create();
            var sideBar = cardLibrary.GetNode<Control>("Sidebar");
            cardLibrary.RemoveChild(sideBar);

            RemoveNode(sideBar, "MarginContainer/BottomVBox/Upgrades");
            RemoveNode(sideBar, "MarginContainer/BottomVBox/Stats");
            sideBar.CustomMinimumSize = new Vector2(288, 1);
            cardBox.AddChild(sideBar);

            var cardsContainer = new GridContainer
            {
                Name = "CardsContainer",
                Columns = 2,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            cardsContainer.AddThemeConstantOverride("h_separation", 32);
            cardsScrollContainer.AddChild(cardsContainer);

            var buttonContainer = new HBoxContainer()
            {
                Name = "ButtonsContainer",
                AnchorLeft = 0,
                AnchorTop = 1,
                AnchorRight = 1,
                AnchorBottom = 1,
                OffsetLeft = 0,
                OffsetTop = -150,
                OffsetRight = -25,
                OffsetBottom = -50,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            customRun.GetNode("RightContainer").AddChild(buttonContainer);

            var trainingRunScreen = new NTrainingRunScreen()
            {
                Name = "TrainingRunScreen",
                AnchorsPreset = 15,
                AnchorLeft = 0,
                AnchorRight = 1,
                AnchorTop = 0,
                AnchorBottom = 1,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both
            };

            foreach (var child in customRun.GetChildren())
            {
                customRun.RemoveChild(child);
                trainingRunScreen.AddChild(child);
                child.Owner = trainingRunScreen;
            }

            return trainingRunScreen;
        }

        public static void RemoveNode(Node root, string path)
        {
            var node = root.GetNode(path);
            node?.GetParent()?.RemoveChild(node);
        }

        private void FilterCards(Type type, bool selected, Func<CardModel, bool> cardPoolPredicate)
        {
            if (!_filter.ContainsKey(type)) _filter.Add(type, []);
            if (_filter[type] == null) _filter[type] = [];
            if (selected)
            {
                _filter[type].Add(cardPoolPredicate);
            }
            else
            {
                _filter[type].Remove(cardPoolPredicate);
            }
            FilterCards();
        }

        private void FilterCards(Func<CardModel, bool> cardPoolPredicate)
        {
            _filter[typeof(string)] = [cardPoolPredicate];
            FilterCards();
        }

        private void FilterCards()
        {
            foreach (NTrainingCardHolder holder in _cardsContainer.GetChildren().Where(holder => holder.HasMeta("controller")).Select(holder => (NTrainingCardHolder)holder.GetMeta("controller")))
            {
                holder.Filter(_filter);
            }
        }

        private void ClearCards()
        {
            foreach (NTrainingCardHolder holder in _cardsContainer.GetChildren().Where(holder => holder.HasMeta("controller")).Select(holder => (NTrainingCardHolder)holder.GetMeta("controller")))
            {
                holder.SetValue(0);
            }
        }

        private void SortCards(Func<CardModel, CardModel, int> sort)
        {
            _cards.Sort((x, y) => sort(x, y));
            RefreshCards();
        }

        private void RefreshCards(bool redraw = false)
        {
            if (redraw)
            {
                foreach (var child in _cardsContainer.GetChildren().Where(holder => holder.HasMeta("controller")))
                {
                    child.QueueFree();
                }

                foreach (var card in _cards)
                {
                    var holder = new NTrainingCardHolder(card);
                    _cardsContainer.AddChild(holder.RootNode);
                }
            }
            else
            {
                var children = _cardsContainer.GetChildren().Where(c => c.HasMeta("controller")).ToList();
                for (int i = 0; i < _cards.Count; i++)
                {
                    var child = children.First(holder => ((NTrainingCardHolder)holder.GetMeta("controller")).CardModel == _cards[i]);
                    _cardsContainer.MoveChild(child, i);
                }
            }
            FilterCards();
        }

        private List<CardModel> GetSelectedCards()
        {
            return [.. _cardsContainer.GetChildren().Where(holder => holder.HasMeta("controller")).SelectMany(holder => ((NTrainingCardHolder)holder.GetMeta("controller")).GetCards())];
        }

        private void SetDefaults()
        {
            ClearCards();
            ClearRelics();
            foreach (var holder in _cardsContainer.GetChildren().Where(holder => holder.HasMeta("controller")).Select(holder => (NTrainingCardHolder)holder.GetMeta("controller")))
            {
                holder.SetValue(_lobby.LocalPlayer.character.StartingDeck.Count(card =>
                {
                    return card.Id == holder.CardModel.Id && !holder.CardModel.IsUpgraded;
                }));
            }

            foreach (var relic in _lobby.LocalPlayer.character.StartingRelics)
            {
                var holder = _relicsContainer.GetChildrenRecursive<NRelicBasicHolder>().First(holder => holder.Relic.Model.Id == relic.Id);
                if (!holder.HasMeta("selected") || !holder.GetMeta("selected").AsBool()) RelicClicked(holder);
            }
        }

        private void FilterRelics(string filter)
        {
            foreach (var holder in _relicsContainer.GetChildrenRecursive<NRelicBasicHolder>())
            {
                if (holder.Relic.Model.Title.GetRawText().Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                {
                    holder.Show();
                }
                else
                {
                    holder.Hide();
                }
            }
        }

        private void ClearRelics()
        {
            foreach (var holder in _relicsContainer.GetChildrenRecursive<NRelicBasicHolder>())
            {
                if (holder.HasMeta("selected") && holder.GetMeta("selected").AsBool()) RelicClicked(holder);
            }
        }

        private void RefreshRelics()
        {
            foreach (var relic in ModelDb.AllRelics)
            {
                var holder = NRelicBasicHolder.Create(relic);
                if (holder != null)
                {
                    _relicsContainer.AddChild(holder);
                    if (TrainingConfig.IsRelicSelected(holder.Relic.Model)) RelicClicked(holder);
                    holder.Connect(NClickableControl.SignalName.Released, Callable.From<NRelicBasicHolder>(RelicClicked));
                }
            }
        }

        private List<RelicModel> GetSelectedRelics()
        {
            return [.. _relicsContainer.GetChildrenRecursive<NRelicBasicHolder>().Where(holder => holder.HasMeta("selected") && holder.GetMeta("selected").AsBool()).Select(holder => holder.Relic.Model)];
        }

        private void RelicClicked(NRelicBasicHolder relicHolder)
        {
            var selected = !relicHolder.HasMeta("selected") || !relicHolder.GetMeta("selected").AsBool();
            relicHolder.SetMeta("selected", selected);
            TrainingConfig.SelectRelic(relicHolder.Relic.Model, selected);
            Config.Save();
            relicHolder.Relic.Outline.SelfModulate = selected ? new Color(1f, 0.784f, 0f, 0.98f) : new Color(0f, 0f, 0f, 0.501961f);
        }

        public override void _Ready()
        {
            ConnectSignals();
            _disclaimer = GetNode<MegaLabel>("Disclaimer");
            _charButtonContainer = GetNode<Control>("LeftContainer/CharSelectButtons/ButtonContainer");
            _ascensionPanel = GetNode<NAscensionPanel>("LeftContainer/AscensionPanel");

            _confirmButton = GetNode<NConfirmButton>("ConfirmButton");
            _backButton = GetNode<NBackButton>("BackButton");

            _relicsFilterBox = GetNode<LineEdit>("LeftContainer/RelicsFilter");
            _relicsFilterBox.TextChanged += FilterRelics;
            _relicsContainer = GetNode<GridContainer>("LeftContainer/RelicsScroll/RelicsContainer");

            _searchBar = GetNode<NSearchBar>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/SearchBar");
            _searchBar.Connect(NSearchBar.SignalName.QueryChanged, Callable.From<string>(stringFilter => FilterCards(card => card.Title.Contains(stringFilter))));
            _searchBar.Connect(NSearchBar.SignalName.QuerySubmitted, Callable.From<string>(stringFilter => FilterCards(card => card.Title.Contains(stringFilter))));

            _ironcladFilter = GetNode<NCardPoolFilter>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/PoolFilters/IroncladPool");
            _silentFilter = GetNode<NCardPoolFilter>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/PoolFilters/SilentPool");
            _defectFilter = GetNode<NCardPoolFilter>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/PoolFilters/DefectPool");
            _regentFilter = GetNode<NCardPoolFilter>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/PoolFilters/RegentPool");
            _necrobinderFilter = GetNode<NCardPoolFilter>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/PoolFilters/NecrobinderPool");
            _colorlessFilter = GetNode<NCardPoolFilter>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/PoolFilters/ColorlessPool");
            _ancientsFilter = GetNode<NCardPoolFilter>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/PoolFilters/AncientsPool");
            _miscPoolFilter = GetNode<NCardPoolFilter>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/PoolFilters/MiscPool");

            _ironcladFilter.IsSelected = false;
            _ironcladFilter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(filter => FilterCards(filter.GetType(), filter.IsSelected, card => card.Pool is IroncladCardPool)));
            _silentFilter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(filter => FilterCards(filter.GetType(), filter.IsSelected, card => card.Pool is SilentCardPool)));
            _defectFilter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(filter => FilterCards(filter.GetType(), filter.IsSelected, card => card.Pool is DefectCardPool)));
            _regentFilter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(filter => FilterCards(filter.GetType(), filter.IsSelected, card => card.Pool is RegentCardPool)));
            _necrobinderFilter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(filter => FilterCards(filter.GetType(), filter.IsSelected, card => card.Pool is NecrobinderCardPool)));
            _colorlessFilter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(filter => FilterCards(filter.GetType(), filter.IsSelected, card => card.Pool is ColorlessCardPool)));
            _ancientsFilter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(filter => FilterCards(filter.GetType(), filter.IsSelected, card => card.Rarity == CardRarity.Ancient)));
            _miscPoolFilter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(filter => FilterCards(filter.GetType(), filter.IsSelected, card => (card.Rarity - CardRarity.Ancient) > 0)));

            _typeSorter = GetNode<NCardViewSortButton>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CardTypeModule/CardTypeSorter");
            _typeSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NCardViewSortButton>((filter) => SortCards((a, b) => a.Type.CompareTo(b.Type) * (filter.IsDescending ? 1 : -1))));

            _attackFilter = GetNode<NCardTypeTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CardTypeModule/CardTypeToggler/AttackType");
            _skillFilter = GetNode<NCardTypeTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CardTypeModule/CardTypeToggler/SkillType");
            _powerFilter = GetNode<NCardTypeTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CardTypeModule/CardTypeToggler/PowerType");
            _otherTypeFilter = GetNode<NCardTypeTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CardTypeModule/CardTypeToggler/OtherType");

            _attackFilter.IsTicked = false;
            _skillFilter.IsTicked = false;
            _powerFilter.IsTicked = false;
            _otherTypeFilter.IsTicked = false;

            _attackFilter.Connect(NCardTypeTickbox.SignalName.Toggled, Callable.From<NCardTypeTickbox>(filter => FilterCards(filter.GetType(), filter.IsTicked, card => card.Type == CardType.Attack)));
            _skillFilter.Connect(NCardTypeTickbox.SignalName.Toggled, Callable.From<NCardTypeTickbox>(filter => FilterCards(filter.GetType(), filter.IsTicked, card => card.Type == CardType.Skill)));
            _powerFilter.Connect(NCardTypeTickbox.SignalName.Toggled, Callable.From<NCardTypeTickbox>(filter => FilterCards(filter.GetType(), filter.IsTicked, card => card.Type == CardType.Power)));
            _otherTypeFilter.Connect(NCardTypeTickbox.SignalName.Toggled, Callable.From<NCardTypeTickbox>(filter => FilterCards(filter.GetType(), filter.IsTicked, card => (card.Type - CardType.Power) > 0)));

            _raritySorter = GetNode<NCardViewSortButton>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/RarityModule/RaritySorter");
            _raritySorter.Connect(NClickableControl.SignalName.Released, Callable.From<NCardViewSortButton>((filter) => SortCards((a, b) => GetCardRarityComparisonValue(a).CompareTo(GetCardRarityComparisonValue(b) * (filter.IsDescending ? 1 : -1)))));

            _commonFilter = GetNode<NCardRarityTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/RarityModule/RarityToggler/CommonRarity");
            _uncommonFilter = GetNode<NCardRarityTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/RarityModule/RarityToggler/UncommonRarity");
            _rareFilter = GetNode<NCardRarityTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/RarityModule/RarityToggler/RareRarity");
            _otherFilter = GetNode<NCardRarityTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/RarityModule/RarityToggler/OtherRarity");

            _commonFilter.IsTicked = false;
            _uncommonFilter.IsTicked = false;
            _rareFilter.IsTicked = false;
            _otherFilter.IsTicked = false;

            _commonFilter.Connect(NTickbox.SignalName.Toggled, Callable.From<NCardRarityTickbox>((filter) => FilterCards(filter.GetType(), filter.IsTicked, card => card.Rarity == CardRarity.Common)));
            _uncommonFilter.Connect(NTickbox.SignalName.Toggled, Callable.From<NCardRarityTickbox>((filter) => FilterCards(filter.GetType(), filter.IsTicked, card => card.Rarity == CardRarity.Uncommon)));
            _rareFilter.Connect(NTickbox.SignalName.Toggled, Callable.From<NCardRarityTickbox>((filter) => FilterCards(filter.GetType(), filter.IsTicked, card => card.Rarity == CardRarity.Rare)));
            _otherFilter.Connect(NTickbox.SignalName.Toggled, Callable.From<NCardRarityTickbox>((filter) => FilterCards(filter.GetType(), filter.IsTicked, card => (card.Rarity - CardRarity.Rare) > 0)));

            _costSorter = GetNode<NCardViewSortButton>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CostModule/CostSorter");
            _costSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NCardViewSortButton>((filter) => SortCards((a, b) => a.EnergyCost.GetResolved().CompareTo(b.EnergyCost.GetResolved() * (filter.IsDescending ? 1 : -1)))));

            _zeroFilter = GetNode<NCardCostTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CostModule/CostToggler/Cost0");
            _oneFilter = GetNode<NCardCostTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CostModule/CostToggler/Cost1");
            _twoFilter = GetNode<NCardCostTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CostModule/CostToggler/Cost2");
            _threePlusFilter = GetNode<NCardCostTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CostModule/CostToggler/Cost3+");
            _xFilter = GetNode<NCardCostTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/CostModule/CostToggler/CostX");

            _zeroFilter.IsTicked = false;
            _oneFilter.IsTicked = false;
            _twoFilter.IsTicked = false;
            _threePlusFilter.IsTicked = false;
            _xFilter.IsTicked = false;

            _zeroFilter.Connect(NClickableControl.SignalName.Released, Callable.From<NCardCostTickbox>((filter) => FilterCards(filter.GetType(), filter.IsTicked, card => card.EnergyCost.Canonical == 0)));
            _oneFilter.Connect(NClickableControl.SignalName.Released, Callable.From<NCardCostTickbox>((filter) => FilterCards(filter.GetType(), filter.IsTicked, card => card.EnergyCost.Canonical == 1)));
            _twoFilter.Connect(NClickableControl.SignalName.Released, Callable.From<NCardCostTickbox>((filter) => FilterCards(filter.GetType(), filter.IsTicked, card => card.EnergyCost.Canonical == 2)));
            _threePlusFilter.Connect(NClickableControl.SignalName.Released, Callable.From<NCardCostTickbox>((filter) => FilterCards(filter.GetType(), filter.IsTicked, card => card.EnergyCost.Canonical >= 3)));
            _xFilter.Connect(NClickableControl.SignalName.Released, Callable.From<NCardCostTickbox>((filter) => FilterCards(filter.GetType(), filter.IsTicked, card => card.EnergyCost.CostsX || card.HasStarCostX)));

            _alphabetSorter = GetNode<NCardViewSortButton>("RightContainer/CardBox/Sidebar/MarginContainer/TopVBox/AlphabetSorter");
            _alphabetSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NCardViewSortButton>((filter) => SortCards((a, b) => string.Compare(a.Title, b.Title, LocManager.Instance.CultureInfo, CompareOptions.None) * (filter.IsDescending ? 1 : -1))));

            _viewMultiplayerCards = GetNode<NLibraryStatTickbox>("RightContainer/CardBox/Sidebar/MarginContainer/BottomVBox/MultiplayerCards");
            _viewMultiplayerCards.IsTicked = true;
            _viewMultiplayerCards.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>((filter) => FilterCards(filter.GetType(), !filter.IsTicked, card => card.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly)));

            _typeSorter.SetLabel(new LocString("gameplay_ui", "SORT_TYPE").GetRawText());
            _raritySorter.SetLabel(new LocString("gameplay_ui", "SORT_RARITY").GetRawText());
            _costSorter.SetLabel(new LocString("gameplay_ui", "SORT_COST").GetRawText());
            _alphabetSorter.SetLabel(new LocString("gameplay_ui", "SORT_ALPHABET").GetRawText());
            _commonFilter.SetLabel(new LocString("card_library", "RARITY_COMMON").GetRawText());
            _uncommonFilter.SetLabel(new LocString("card_library", "RARITY_UNCOMMON").GetRawText());
            _rareFilter.SetLabel(new LocString("card_library", "RARITY_RARE").GetRawText());
            _otherFilter.SetLabel(new LocString("card_library", "RARITY_OTHER").GetRawText());
            _viewMultiplayerCards.SetLabel(new LocString("card_library", "VIEW_MULTIPLAYER_CARDS").GetRawText());
            _colorlessFilter.Loc = new LocString("card_library", "POOL_COLORLESS_TIP");
            _ancientsFilter.Loc = new LocString("card_library", "POOL_ANCIENT_TIP");
            _miscPoolFilter.Loc = new LocString("card_library", "POOL_MISC_TIP");
            _attackFilter.Loc = new LocString("card_library", "TYPE_ATTACK_TIP");
            _skillFilter.Loc = new LocString("card_library", "TYPE_SKILL_TIP");
            _powerFilter.Loc = new LocString("card_library", "TYPE_POWER_TIP");
            _otherTypeFilter.Loc = new LocString("card_library", "TYPE_OTHER_TIP");
            _commonFilter.Loc = new LocString("card_library", "RARITY_COMMON_TIP");
            _uncommonFilter.Loc = new LocString("card_library", "RARITY_UNCOMMON_TIP");
            _rareFilter.Loc = new LocString("card_library", "RARITY_RARE_TIP");
            _otherFilter.Loc = new LocString("card_library", "RARITY_OTHER_TIP");
            _zeroFilter.Loc = new LocString("card_library", "COST_ZERO_TIP");
            _oneFilter.Loc = new LocString("card_library", "COST_ONE_TIP");
            _twoFilter.Loc = new LocString("card_library", "COST_TWO_TIP");
            _threePlusFilter.Loc = new LocString("card_library", "COST_THREE_TIP");
            _xFilter.Loc = new LocString("card_library", "COST_X_TIP");

            _cardsContainer = GetNode<GridContainer>("RightContainer/CardBox/CardsScroll/CardsContainer");

            _confirmButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnEmbarkPressed));
            _ascensionPanel.Connect(NAscensionPanel.SignalName.AscensionLevelChanged, Callable.From(OnAscensionPanelLevelChanged));
            _disclaimer.SetTextAutoSize(new LocString("main_menu_ui", "TRAINING_RUN_SCREEN.disclaimer").GetFormattedText());
            GetNode<MegaLabel>("LeftContainer/CustomModeTitle").SetTextAutoSize(new LocString("main_menu_ui", "TRAINING_RUN_SCREEN.TRAINING_MODE_TITLE").GetFormattedText());
            InitCharacterButtons();
            NControllerManager.Instance?.Connect(NControllerManager.SignalName.MouseDetected, Callable.From(UpdateControllerButton));
            NControllerManager.Instance?.Connect(NControllerManager.SignalName.ControllerDetected, Callable.From(UpdateControllerButton));
            NInputManager.Instance?.Connect(NInputManager.SignalName.InputRebound, Callable.From(UpdateControllerButton));

            var buttonsContainer = GetNode<HBoxContainer>("RightContainer/ButtonsContainer");
            var clearAllButton = new NConfigButton();
            clearAllButton.Initialize("Clear All", () =>
            {
                ClearCards(); ClearRelics();
            });
            buttonsContainer?.AddChildSafely(clearAllButton);

            var defaults = new NConfigButton();
            defaults.Initialize("Defaults", () =>
            {
                SetDefaults();
            });
            buttonsContainer?.AddChildSafely(defaults);

            RefreshRelics();
            RefreshCards(true);
        }

        public override void OnSubmenuOpened()
        {
            base.OnSubmenuOpened();
            foreach (var item in _charButtonContainer.GetChildren().OfType<NCharacterSelectButton>())
            {
                if (!item.IsLocked)
                {
                    item.Enable();
                    item.Reset();
                }
                else
                {
                    item.UnlockIfPossible();
                }
            }

            _confirmButton.Enable();
            _charButtonContainer.GetChild<NCharacterSelectButton>(0).Select();
            if (_lobby.NetService.Type == NetGameType.Client)
            {
                _ascensionPanel.SetAscensionLevel(_lobby.Ascension);
            }

            foreach (var player in _lobby.Players)
            {
                RefreshButtonSelectionForPlayer(player);
            }

        }

        public override void OnSubmenuClosed()
        {
            base.OnSubmenuClosed();
            _confirmButton.Disable();
            if (_lobby.NetService.Type.IsMultiplayer())
            {
                PlatformUtil.SetRichPresence("MAIN_MENU", null, null);
            }

            CleanUpLobby(disconnectSession: true);
        }

        public void InitializeSingleplayer()
        {
            _lobby = new StartRunLobby(GameMode.None, new NetSingleplayerGameService(), this, 1);
            _ascensionPanel.Initialize(MultiplayerUiMode.Singleplayer);
            _lobby.AddLocalHostPlayer(new UnlockState(SaveManager.Instance.Progress), 0);
            _uiMode = MultiplayerUiMode.Singleplayer;
            UpdateControllerButton();
            AfterInitialized();
        }

        public void BeginRun(string seed, List<ActModel> acts, IReadOnlyList<ModifierModel> modifiers)
        {
            var act = ModelDb.Act<TrainingAct>();
            if (ModelDb.Modifier<TrainingModifier>().ToMutable() is TrainingModifier trainingModifier)
            {
                trainingModifier.Relics = GetSelectedRelics();
                trainingModifier.Cards = GetSelectedCards();
                modifiers = [trainingModifier];
            }

            NAudioManager.Instance?.StopMusic();
            _confirmButton.Disable();
            TaskHelper.RunSafely(StartNewSingleplayerRun(seed, [act], modifiers));
        }

        private async Task StartNewSingleplayerRun(string seed, List<ActModel> acts, IReadOnlyList<ModifierModel> modifiers)
        {
            try
            {
                Log.Info($"Embarking on a TRAINING {_lobby.LocalPlayer.character.Id.Entry} run. Ascension: {_lobby.Ascension} Seed: {_lobby.Seed} Modifiers: {GetModifiersString()}");
                SfxCmd.Play(_lobby.LocalPlayer.character.CharacterTransitionSfx);
                if (NGame.Instance != null)
                {
                    await NGame.Instance.Transition.FadeOut(0.8f, _lobby.LocalPlayer.character.CharacterSelectTransitionPath);
                    await NGame.Instance.StartNewSingleplayerRun(_lobby.LocalPlayer.character, shouldSave: false, acts, modifiers, seed, GameMode.None, _lobby.Ascension);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception starting training singleplayer run : {ex}");
                CleanUpLobby(disconnectSession: true, NetError.InternalError);
                if (NGame.Instance != null)
                {
                    await NGame.Instance.ReturnToMainMenuWithInternalError(ex);
                }
                return;
            }

            CleanUpLobby(disconnectSession: false);
        }

        private string GetModifiersString()
        {
            return string.Join(",", _lobby.Modifiers.Select(m => m.Id));
        }

        private void OnEmbarkPressed(NButton _)
        {
            _confirmButton.Disable();
            _backButton.Disable();
            _lobby.SetReady(ready: true);
            foreach (NCharacterSelectButton item in _charButtonContainer.GetChildren().OfType<NCharacterSelectButton>())
            {
                item.Disable();
            }

            if (_lobby.NetService.Type.IsMultiplayer() && !_lobby.IsAboutToBeginGame())
            {
                //_unreadyButton.Enable();
                //_readyAndWaitingContainer.Visible = true;
            }
        }

        public override void _Process(double delta)
        {
            if (_lobby != null && _lobby.NetService.IsConnected)
            {
                _lobby.NetService.Update();
            }
        }

        private void CleanUpLobby(bool disconnectSession, NetError error = NetError.Quit)
        {
            _lobby.CleanUp(disconnectSession, error);
            _lobby = null;
        }

        private void OnAscensionPanelLevelChanged()
        {
            if (_lobby.NetService.Type != NetGameType.Client && _lobby.Ascension != _ascensionPanel.Ascension)
            {
                _lobby.SyncAscensionChange(_ascensionPanel.Ascension);
            }
        }

        public void AscensionChanged()
        {
            if (_lobby.NetService.Type == NetGameType.Client)
            {
                _ascensionPanel.Visible = _lobby.Ascension > 0;
            }

            _ascensionPanel.SetAscensionLevel(_lobby.Ascension);
        }

        private void AfterInitialized()
        {
            if (NGame.Instance != null)
            {
                NGame.Instance.RemoteCursorContainer.Initialize(_lobby.InputSynchronizer, _lobby.Players.Select(p => p.id));
                NGame.Instance.ReactionContainer.InitializeNetworking(_lobby.NetService);
                NGame.Instance.TimeoutOverlay.Initialize(_lobby.NetService, isGameLevel: true);
            }

            MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Network] = ((_lobby.NetService.Type == NetGameType.Singleplayer) ? LogLevel.Info : LogLevel.Debug);
            MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.Actions] = ((_lobby.NetService.Type == NetGameType.Singleplayer) ? LogLevel.Info : LogLevel.VeryDebug);
            MegaCrit.Sts2.Core.Logging.Logger.logLevelTypeMap[LogType.GameSync] = ((_lobby.NetService.Type == NetGameType.Singleplayer) ? LogLevel.Info : LogLevel.VeryDebug);
            if (NGame.Instance != null)
            {
                NGame.Instance.DebugSeedOverride = null;
            }
        }

        private void UpdateControllerButton()
        {

        }

        private void InitCharacterButtons()
        {
            foreach (CharacterModel allCharacter in ModelDb.AllCharacters)
            {
                var nCharacterSelectButton = PreloadManager.Cache.GetScene("res://scenes/screens/char_select/char_select_button.tscn").Instantiate<NCharacterSelectButton>(PackedScene.GenEditState.Disabled);
                nCharacterSelectButton.Name = allCharacter.Id.Entry + "_button";
                _charButtonContainer.AddChildSafely(nCharacterSelectButton);
                nCharacterSelectButton.Init(allCharacter, this);
            }

            for (int i = 0; i < _charButtonContainer.GetChildCount(); i++)
            {
                var child = _charButtonContainer.GetChild<Control>(i);
                child.FocusNeighborLeft = (i > 0) ? _charButtonContainer.GetChild<Control>(i - 1).GetPath() : child.GetPath();
                child.FocusNeighborRight = (i < _charButtonContainer.GetChildCount() - 1) ? _charButtonContainer.GetChild<Control>(i + 1).GetPath() : child.GetPath();
                child.FocusNeighborBottom = child.GetPath();
            }
        }

        public void PlayerConnected(StartRunLobbyPlayer player)
        {
            RefreshButtonSelectionForPlayer(player);
        }

        public void PlayerChanged(StartRunLobbyPlayer player, bool isRandomCharacterResolution)
        {
            if (isRandomCharacterResolution)
            {
                throw new InvalidOperationException("Random character is not currently allowed in training!");
            }
            RefreshButtonSelectionForPlayer(player);
        }

        private void RefreshButtonSelectionForPlayer(StartRunLobbyPlayer player)
        {
            if (player.id == _lobby.LocalPlayer.id)
            {
                return;
            }

            foreach (NCharacterSelectButton item in _charButtonContainer.GetChildren().OfType<NCharacterSelectButton>())
            {
                if (item.RemoteSelectedPlayers.Contains(player.id) && player.character != item.Character)
                {
                    item.OnRemotePlayerDeselected(player.id);
                }
                else if (player.character == item.Character)
                {
                    item.OnRemotePlayerSelected(player.id);
                }
            }
        }

        public void SeedChanged()
        {

        }

        public void ModifiersChanged()
        {

        }

        public void MaxAscensionChanged()
        {
            _ascensionPanel.SetMaxAscension(_lobby.MaxAscension);
        }

        public void RemotePlayerDisconnected(StartRunLobbyPlayer player)
        {
        }

        public void LocalPlayerDisconnected(NetErrorInfo info)
        {
        }

        public void SelectCharacter(NCharacterSelectButton charSelectButton, CharacterModel characterModel)
        {
            if (_lobby == null)
            {
                throw new InvalidOperationException("Cannot select character while loading!");
            }

            SfxCmd.Play(characterModel.CharacterSelectSfx);
            _selectedButton = charSelectButton;
            foreach (NCharacterSelectButton item in _charButtonContainer.GetChildren().OfType<NCharacterSelectButton>())
            {
                if (item != _selectedButton)
                {
                    item.Deselect();
                }
            }

            _lobby.SetLocalCharacter(characterModel);
        }
    }
}
