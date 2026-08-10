using Godot;
using MegaCrit.Sts2.Core.Models;
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

        public CardModel CardModel => _cardModel;

        private readonly CardModel _cardModel;

        private readonly NDeckHistoryEntry _cardEntry;

        private int Count;

        private SpinBox _spinBox;

        public NTrainingCardHolder(CardModel card)
        {
            _cardModel = card;

            var name = $"{card.Title}";

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

            _cardEntry = NDeckHistoryEntry.Create(card, 1);
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

            Count = TrainingConfig.GetCardValue(CardModel);
            _spinBox.Value = Count;
        }

        public void SetValue(int newValue)
        {
            _spinBox.Value = newValue;
        }

        public void Filter(Dictionary<Type, List<Func<CardModel, bool>>> filters)
        {
            if (filters.All(group => group.Value.Count == 0 || group.Value.Any(filter => filter(_cardModel))))
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
            NGame.Instance?.GetInspectCardScreen().Open([_cardModel], 0);
        }

        private void OnValueChanged(double value)
        {
            Count = Convert.ToInt32(value);
            TrainingConfig.SelectCard(_cardEntry.Card, Count);
            NTrainingRunScreen.Config.Save();
        }

        public IEnumerable<CardModel> GetCards()
        {
            return Enumerable.Repeat(_cardModel, Count);
        }
    }
}