using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using Training.TrainingCode.Config;
using static Godot.Control;

namespace Training.TrainingCode.Screens
{
    public partial class NTrainingCardHolder : RefCounted
    {
        public PanelContainer RootNode { get; private set; }

        public CardModel CardModel => _cardModel.Item1;

        private readonly Tuple<CardModel, bool> _cardModel;

        private readonly NDeckHistoryEntry _cardEntry;

        private int Count;

        private SpinBox _spinBox;

        public NTrainingCardHolder(Tuple<CardModel, bool> card)
        {
            _cardModel = card;

            var name = $"{card.Item1.Title}";
            if (card.Item2) name += "+";

            RootNode = new PanelContainer
            {
                Name = $"{name}-PanelContainer",
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsVertical = SizeFlags.Fill,
            };

            RootNode.SetMeta("controller", this);

            var vbox = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            RootNode.AddChild(vbox);

            var cardBox = new HBoxContainer
            {
                ClipChildren = CanvasItem.ClipChildrenMode.Only,
                MouseFilter = MouseFilterEnum.Ignore
            };
            vbox.AddChild(cardBox);

            _spinBox = new SpinBox
            {
                CustomMinimumSize = new Vector2(32, 32),
                MinValue = 0,
                MaxValue = 99,
                Step = 1,
                Value = 0,
                Rounded = true
            };
            _spinBox.ValueChanged += OnValueChanged;
            cardBox.AddChild(_spinBox);

            if (card.Item1.ToMutable() is CardModel newCard)
            {
                if (card.Item2) newCard.UpgradeInternal();

                _cardEntry = NDeckHistoryEntry.Create(newCard, 1);
                _cardEntry.Connect(NDeckHistoryEntry.SignalName.Clicked, Callable.From<NDeckHistoryEntry>(ShowEntry));
                _cardEntry.Connect(NClickableControl.SignalName.Focused, Callable.From<NClickableControl>(delegate
                {
                    EmitSignal(NDeckHistory.SignalName.Hovered, _cardEntry);
                }));
                _cardEntry.Connect(NClickableControl.SignalName.Unfocused, Callable.From<NClickableControl>(delegate
                {
                    EmitSignal(NDeckHistory.SignalName.Unhovered, _cardEntry);
                }));
                cardBox.AddChild(_cardEntry);

                Count = TrainingConfig.GetCardValue(newCard.Title);
                _spinBox.Value = Count;
            }
        }

        public void SetValue(int newValue)
        {
            _spinBox.Value = newValue;
        }

        public void Filter(string filter)
        {
            if (_cardModel.Item1.Title.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
            {
                RootNode.Show();
            }
            else
            {
                RootNode.Hide();
            }         
        }

        private void ShowEntry(NDeckHistoryEntry entry)
        {
            NGame.Instance?.GetInspectCardScreen().Open([_cardModel.Item1], 0);
        }

        private void OnValueChanged(double value)
        {
            Count = Convert.ToInt32(value);
            TrainingConfig.SelectCard(_cardEntry.Card.Title, Count);
            NTrainingRunScreen.Config.Save();
        }

        public IEnumerable<Tuple<CardModel, bool>> GetCards()
        {
            return Enumerable.Repeat(_cardModel, Count);
        }
    }
}