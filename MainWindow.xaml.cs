using System;
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
            btnStart.IsEnabled = false;
            StatusDot.Fill = Brushes.Yellow;
            StatusText.Text = "STATUS : RUNNING...";

            var client = new DeviceClient();
            var response = await client.SendAsync("READ_INPUT_REGISTERS");

            ShowPackets(response);

            StatusDot.Fill = response.IsError ? Brushes.Red : Brushes.Lime;
            StatusText.Text = response.IsError ? "STATUS : ERROR" : "STATUS : PASS";
            btnStart.IsEnabled = true;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            PacketPanel.Children.Clear();
            StatusDot.Fill = Brushes.Lime;
            StatusText.Text = "STATUS : READY";
        }

        // ── Main render method ──────────────────────────────────────────────

        private void ShowPackets(DeviceResponse response)
        {
            if (response.Request != null)
                PacketPanel.Children.Add(BuildRequestTable(response.Request));

            if (response.Response != null)
                PacketPanel.Children.Add(BuildResponseTable(response.Response));
        }

        // ── REQUEST table ───────────────────────────────────────────────────

        private UIElement BuildRequestTable(ModbusPacket pkt)
        {
            // Header + description
            var outerStack = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

            outerStack.Children.Add(SectionLabel("REQUEST", "#00FFFF"));
            outerStack.Children.Add(SubLabel(
                "The request message specifies the starting register and quantity of registers to be read.",
                "#AAAAAA"));
            outerStack.Children.Add(SubLabel(
                $"Example: Read 0…1 (register 30001 to 30002) from slave device {pkt.SlaveAddress}:",
                "#888888"));

            // Table
            var grid = MakeTableGrid();

            AddHeaderRow(grid, 0);

            int row = 1;
            AddRow(grid, row++, "Header", "None", "None");
            AddRow(grid, row++, "Slave Address", $"{pkt.SlaveAddress:X2}", $"{pkt.SlaveAddress:X2}");
            AddRow(grid, row++, "Function", $"{pkt.FunctionCode:X2}", $"0 {pkt.FunctionCode:X}");
            AddRow(grid, row++, "Starting Address Hi", $"{(pkt.StartAddress >> 8):X2}", $"0 {(pkt.StartAddress >> 8):X}");
            AddRow(grid, row++, "Starting Address Lo", $"{(pkt.StartAddress & 0xFF):X2}", $"0 {(pkt.StartAddress & 0xFF):X}");
            AddRow(grid, row++, "Quantity of Registers Hi", $"{(pkt.Quantity >> 8):X2}", $"0 {(pkt.Quantity >> 8):X}");
            AddRow(grid, row++, "Quantity of Registers Lo", $"{(pkt.Quantity & 0xFF):X2}", $"0 {(pkt.Quantity & 0xFF):X}");

            byte crcLo = (byte)(pkt.Crc & 0xFF);
            byte crcHi = (byte)(pkt.Crc >> 8);
            AddRow(grid, row++, "Error Check Lo (CRC)", $"{crcLo:X2}", $"LRC ({crcLo:X2})");
            AddRow(grid, row++, "Error Check Hi (CRC)", $"{crcHi:X2}", "None");
            AddRow(grid, row++, "Trailer", "None", "CR LF");
            AddRow(grid, row++, "Total Bytes", pkt.RawBytes.Length.ToString(), "—");

            outerStack.Children.Add(WrapTable(grid));

            // Raw hex line
            outerStack.Children.Add(RawHexBlock(pkt.RawBytes, "#00FFFF"));

            return outerStack;
        }

        // ── RESPONSE table ──────────────────────────────────────────────────

        private UIElement BuildResponseTable(ModbusPacket pkt)
        {
            var outerStack = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

            outerStack.Children.Add(SectionLabel("RESPONSE", "#00FF00"));
            outerStack.Children.Add(SubLabel(
                "Register data is packed as two bytes per register, high-order byte first.",
                "#AAAAAA"));
            outerStack.Children.Add(SubLabel("Example of a response to the request:", "#888888"));

            var grid = MakeTableGrid();
            AddHeaderRow(grid, 0);

            int row = 1;
            AddRow(grid, row++, "Header", "None", "None");
            AddRow(grid, row++, "Slave Address", $"{pkt.SlaveAddress:X2}", $"0 {pkt.SlaveAddress:X}");
            AddRow(grid, row++, "Function", $"{pkt.FunctionCode:X2}", $"0 {pkt.FunctionCode:X}");
            AddRow(grid, row++, "Byte Count", $"{pkt.ByteCount:X2}", $"0 {pkt.ByteCount:X}");

            // data bytes (2 bytes per register)
            if (pkt.DataBytes != null)
            {
                for (int i = 0; i < pkt.DataBytes.Length; i += 2)
                {
                    int regNum = (i / 2) + 1;
                    AddRow(grid, row++, $"Data Hi (Reg {regNum})", $"{pkt.DataBytes[i]:X2}", $"0 {pkt.DataBytes[i]:X}");
                    AddRow(grid, row++, $"Data Lo (Reg {regNum})", $"{pkt.DataBytes[i + 1]:X2}", $"0 {pkt.DataBytes[i + 1]:X}");
                }
            }

            byte crcLo = (byte)(pkt.Crc & 0xFF);
            byte crcHi = (byte)(pkt.Crc >> 8);
            AddRow(grid, row++, "Error Check Lo (CRC)", $"{crcLo:X2}", $"LRC ({crcLo:X2})");
            AddRow(grid, row++, "Error Check Hi (CRC)", $"{crcHi:X2}", "None");
            AddRow(grid, row++, "Trailer", "None", "CR LF");
            AddRow(grid, row++, "Total Bytes", pkt.RawBytes.Length.ToString(), "—");

            outerStack.Children.Add(WrapTable(grid));
            outerStack.Children.Add(RawHexBlock(pkt.RawBytes, "#00FF00"));

            return outerStack;
        }

        // ── Table helpers ────────────────────────────────────────────────────

        private static readonly string[] ColHeaders = { "Field Name", "RTU (hex)", "ASCII Characters" };

        private Grid MakeTableGrid()
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
            return g;
        }

        private void AddHeaderRow(Grid g, int rowIndex)
        {
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < ColHeaders.Length; c++)
            {
                var cell = TableCell(ColHeaders[c], "#CCCCCC", isHeader: true);
                Grid.SetRow(cell, rowIndex);
                Grid.SetColumn(cell, c);
                g.Children.Add(cell);
            }
        }

        private void AddRow(Grid g, int rowIndex, string field, string rtu, string ascii)
        {
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            string[] vals = { field, rtu, ascii };
            for (int c = 0; c < vals.Length; c++)
            {
                var cell = TableCell(vals[c], "#DDDDDD", isHeader: false);
                Grid.SetRow(cell, rowIndex);
                Grid.SetColumn(cell, c);
                g.Children.Add(cell);
            }
        }

        private Border TableCell(string text, string fgHex, bool isHeader)
        {
            return new Border
            {
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#333333"),
                BorderThickness = new Thickness(0.5),
                Background = isHeader
                    ? (Brush)new BrushConverter().ConvertFromString("#1A1A2E")
                    : (Brush)new BrushConverter().ConvertFromString("#111111"),
                Padding = new Thickness(8, 5, 8, 5),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = (Brush)new BrushConverter().ConvertFromString(fgHex),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private Border WrapTable(Grid g)
        {
            return new Border
            {
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#444444"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 10, 0, 8),
                ClipToBounds = true,
                Child = g
            };
        }

        private UIElement RawHexBlock(byte[] bytes, string colorHex)
        {
            string hex = string.Join(" ", Array.ConvertAll(bytes, b => b.ToString("X2")));
            return new TextBlock
            {
                Text = "RAW:  " + hex,
                Foreground = (Brush)new BrushConverter().ConvertFromString(colorHex),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 6)
            };
        }

        // ── Label helpers ─────────────────────────────────────────────────────

        private UIElement SectionLabel(string text, string colorHex)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = (Brush)new BrushConverter().ConvertFromString(colorHex),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            };
        }

        private UIElement SubLabel(string text, string colorHex)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = (Brush)new BrushConverter().ConvertFromString(colorHex),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };
        }
    }
}
