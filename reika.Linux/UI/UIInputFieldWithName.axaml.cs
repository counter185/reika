using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace reika.Linux.UI
{
    public partial class UIInputFieldWithName : UserControl
    {
        public static readonly StyledProperty<string> InputFieldNameProperty =
            AvaloniaProperty.Register<UIInputFieldWithName, string>(nameof(InputFieldName), "Name");

        public string InputFieldName
        {
            get => GetValue(InputFieldNameProperty);
            set => SetValue(InputFieldNameProperty, value);
        }

        public UIInputFieldWithName()
        {
            DataContext = this;
            InitializeComponent();
        }
    }
}