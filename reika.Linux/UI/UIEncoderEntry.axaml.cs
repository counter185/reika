using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace reika.Linux.UI
{
    public partial class UIEncoderEntry : UserControl
    {
        public static readonly StyledProperty<string> PrimaryTextProperty =
            AvaloniaProperty.Register<UIInputFieldWithName, string>(nameof(PrimaryText), "PrimaryText");

        public string PrimaryText
        {
            get => GetValue(PrimaryTextProperty);
            set => SetValue(PrimaryTextProperty, value);
        }

        public UIEncoderEntry(string primary, string secondary)
        {
            DataContext = this;
            PrimaryText = primary;
            InitializeComponent();

            Text_Primary.Content = primary;
            Text_Secondary.Text = secondary;
        }
    }
}