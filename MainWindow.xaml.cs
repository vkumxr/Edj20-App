using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Edj20Tester
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            DeviceClient client = new DeviceClient();

            DeviceResponse response =
                await client.SendAsync("RELAY5_ON");

            ShowResponse(response);
        }

        private void ShowResponse(DeviceResponse response)
        {
            string requestText =
@"Slave ID      : 1
Function Code : 05
Type          : Write Coil
Address       : 0004
Value         : ON

RAW:
01 05 00 04 FF 00";

            string responseText =
$@"Measured Value : {response.Raw}
Status         : PASS

RAW:
01 03 02 04 51";

            PacketPanel.Children.Add(
                CreatePacketCard(
                    "MODBUS REQUEST",
                    requestText,
                    "#00FFFF"));

            PacketPanel.Children.Add(
                CreatePacketCard(
                    "MODBUS RESPONSE",
                    responseText,
                    "#00FF00"));
        }

        private Border CreatePacketCard(
            string title,
            string content,
            string color)
        {
            TextBlock text = new TextBlock
            {
                Text = title + "\n\n" + content,

                Foreground =
                    (Brush)new BrushConverter()
                    .ConvertFromString(color),

                FontFamily =
                    new FontFamily("Consolas"),

                FontSize = 14,

                TextWrapping = TextWrapping.Wrap
            };

            Border border = new Border
            {
                BorderBrush =
                    (Brush)new BrushConverter()
                    .ConvertFromString(color),

                BorderThickness = new Thickness(1),

                CornerRadius = new CornerRadius(8),

                Padding = new Thickness(10),

                Margin = new Thickness(0, 0, 0, 10),

                Background =
                    (Brush)new BrushConverter()
                    .ConvertFromString("#111111"),

                Child = text
            };

            return border;
        }
    }
}
