using BaseLib.Config.UI;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
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
using Training.TrainingCode.Acts;
using Training.TrainingCode.Config;
using Training.TrainingCode.Modifiers;
using static Godot.OpenXRCompositionLayer;

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

        private LineEdit _cardsFilterBox;

        private GridContainer _cardsContainer;

        private StartRunLobby _lobby;

        private MultiplayerUiMode _uiMode;

        public StartRunLobby Lobby => _lobby;

        public static IEnumerable<string> AssetPaths => new Array<string>([_scenePath, "res://scenes/screens/char_select/char_select_button.tscn", "res://scenes/screens/custom_run/modifier_tickbox.tscn"]);

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



            //var cardsFilter = new LineEdit()
            //{
            //    Name = "CardsFilter",
            //    AnchorLeft = 0,
            //    AnchorTop = 0,
            //    AnchorRight = 1,
            //    OffsetLeft = 0,
            //    OffsetTop = 150,
            //    OffsetRight = -200,
            //    PlaceholderText = "Filter"
            //};
            var cardBox = new HBoxContainer()
            {
                Name = "CardBox",
                AnchorLeft = 0,
                AnchorTop = 0,
                AnchorBottom = 1,
                AnchorRight = 1,
                OffsetLeft = 50,
                OffsetTop = 0,
                OffsetRight = -50,
                OffsetBottom = -200,
            };
            customRun.GetNode("RightContainer").AddChild(cardBox);

            var cardLibrary = NCardLibrary.Create();
            var sideBar = cardLibrary.GetNode<Control>("Sidebar");
            cardLibrary.RemoveChild(sideBar);
            sideBar.AnchorTop = 0;
            sideBar.AnchorBottom = 1;
            sideBar.OffsetRight = 288;
            sideBar.SetAnchorsPreset(LayoutPreset.FullRect);
            sideBar.GrowVertical = GrowDirection.Both;
            cardBox.AddChild(sideBar);

            var styleBox = new StyleBoxFlat
            {
                BgColor = new Color(0, 0, 0, 0),
                BorderColor = new Color(1, 1, 1, 1) 
            };
            styleBox.SetBorderWidthAll(2);

            var cardsScrollContainer = new ScrollContainer
            {
                Name = "CardsScroll",
                //AnchorLeft = 0,
                //AnchorRight = 1,
                AnchorTop = 0,
                AnchorBottom = 1,
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };
            cardsScrollContainer.AddThemeStyleboxOverride("panel", styleBox);
            cardBox.AddChild(cardsScrollContainer);

            var cardsContainer = new GridContainer
            {
                Name = "CardsContainer",
                Columns = 1,
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
                OffsetRight = 0,
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

        private void FilterCards(string filter)
        {
            foreach (NTrainingCardHolder holder in _cardsContainer.GetChildrenRecursive<PanelContainer>().Where(holder => holder.HasMeta("controller")).Select(holder => (NTrainingCardHolder)holder.GetMeta("controller")))
            {
                holder.Filter(filter);
            }
        }

        private void DefaultCards()
        {
            foreach (NTrainingCardHolder holder in _cardsContainer.GetChildrenRecursive<PanelContainer>().Where(holder => holder.HasMeta("controller")).Select(holder => (NTrainingCardHolder)holder.GetMeta("controller")))
            {
                holder.SetValue(_lobby.LocalPlayer.character.StartingDeck.Count(card => card.Id == holder.CardModel.Id));
            }
        }

        private void ClearCards()
        {
            foreach (NTrainingCardHolder holder in _cardsContainer.GetChildrenRecursive<PanelContainer>().Where(holder => holder.HasMeta("controller")).Select(holder => (NTrainingCardHolder)holder.GetMeta("controller")))
            {
                holder.SetValue(0);
            }
        }

        private void RefreshCards()
        {
            foreach (var card in ModelDb.AllCards)
            {
                var holder = new NTrainingCardHolder(new Tuple<CardModel, bool>(card, false));
                _cardsContainer.AddChild(holder.RootNode);
                if (card.IsUpgradable)
                {
                    var holderPlus = new NTrainingCardHolder(new Tuple<CardModel, bool>(card, true));
                    _cardsContainer.AddChild(holderPlus.RootNode);
                }
            }
        }

        private List<Tuple<CardModel, bool>> GetSelectedCards()
        {
            return [.. _cardsContainer.GetChildrenRecursive<PanelContainer>().Where(holder => holder.HasMeta("controller")).SelectMany(holder => ((NTrainingCardHolder)holder.GetMeta("controller")).GetCards())];
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
                _relicsContainer.AddChild(holder);
                if (TrainingConfig.IsRelicSelected(holder.Relic.Model.Title.GetRawText())) RelicClicked(holder);
                holder?.Connect(NClickableControl.SignalName.Released, Callable.From<NRelicBasicHolder>(RelicClicked));
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
            TrainingConfig.SelectRelic(relicHolder.Relic.Model.Title.GetRawText(), selected);
            NTrainingRunScreen.Config.Save();
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

            //_cardsFilterBox = GetNode<LineEdit>("RightContainer/CardsFilter");
            //_cardsFilterBox.TextChanged += FilterCards;
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
            clearAllButton.Initialize("Clear All", () => { ClearCards(); ClearRelics(); });
            buttonsContainer?.AddChildSafely(clearAllButton);

            var setDefaults = new NConfigButton();
            setDefaults.Initialize("Default Deck", () => { DefaultCards(); ClearRelics(); });
            buttonsContainer?.AddChildSafely(setDefaults);

            RefreshRelics();
            RefreshCards();
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

            foreach (LobbyPlayer player in _lobby.Players)
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

        public void PlayerConnected(LobbyPlayer player)
        {
            RefreshButtonSelectionForPlayer(player);
        }

        public void PlayerChanged(LobbyPlayer player, bool isRandomCharacterResolution)
        {
            if (isRandomCharacterResolution)
            {
                throw new InvalidOperationException("Random character is not currently allowed in training!");
            }
            RefreshButtonSelectionForPlayer(player);
        }

        private void RefreshButtonSelectionForPlayer(LobbyPlayer player)
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

        public void RemotePlayerDisconnected(LobbyPlayer player)
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
