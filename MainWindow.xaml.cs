using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wacton.Unicolour;
using Wpf.Ui.Controls;

using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;

namespace ColorCodePicker
{
    /// <summary>
    /// 메인 윈도우 로직 처리 클래스
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private bool _isUpdating = false;
        private SparkleUpdater _sparkle;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // GitHub Releases 자동 업데이트 설정 (NetSparkleUpdater)
                // 참고: 실제 배포 시에는 appcast.xml 경로와 Public Key를 정확히 세팅해야 합니다.
                string appcastUrl = "https://raw.githubusercontent.com/iy7847/Color-Code/main/appcast.xml";
                _sparkle = new SparkleUpdater(appcastUrl, new Ed25519Checker(SecurityMode.Unsafe))
                {
                    UIFactory = new NetSparkleUpdater.UI.WPF.UIFactory()
                };
                
                // 앱 실행 시 백그라운드에서 업데이트 확인
                _sparkle.StartLoop(true, true);

            }
            catch (Exception)
            {
                // 초기 appcast.xml 부재 시 발생하는 에러 무시
            }
        }

        /// <summary>
        /// 스포이드 버튼 클릭 시 발생하는 이벤트 핸들러
        /// 화면에서 색상을 추출하는 기능을 시작합니다.
        /// </summary>
        private void EyedropperButton_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new EyedropperOverlay(this);
            overlay.Show();
        }

        /// <summary>
        /// 스포이드 오버레이에서 추출한 색상을 입력창에 반영합니다.
        /// </summary>
        public void SetRgbFromEyedropper(byte r, byte g, byte b)
        {
            RgbInputTextBox.Text = $"{r}, {g}, {b}";
        }

        /// <summary>
        /// 사용자가 수동으로 RGB 또는 HEX 코드를 입력할 때 발생하는 이벤트 핸들러
        /// </summary>
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text)) return;

            string input = textBox.Text.Trim();
            Unicolour? color = null;

            try
            {
                if (textBox.Name == "HexInputTextBox")
                {
                    if (!input.StartsWith("#")) input = "#" + input;
                    color = new Unicolour(input);
                }
                else if (textBox.Name == "RgbInputTextBox")
                {
                    // 예: "255, 100, 50" 또는 "255 100 50"
                    var parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3 &&
                        int.TryParse(parts[0], out int r) &&
                        int.TryParse(parts[1], out int g) &&
                        int.TryParse(parts[2], out int b))
                    {
                        color = new Unicolour(ColourSpace.Rgb255, r, g, b);
                    }
                }
                else if (textBox.Name == "MunsellInputTextBox")
                {
                    var match = System.Text.RegularExpressions.Regex.Match(input, @"^([\d\.]+)\s*([A-Za-z]+)\s+([\d\.]+)\s*/\s*([\d\.]+)$");
                    if (match.Success)
                    {
                        double hNum = double.Parse(match.Groups[1].Value);
                        string hLetter = match.Groups[2].Value.ToUpper();
                        double v = double.Parse(match.Groups[3].Value);
                        double c = double.Parse(match.Groups[4].Value);

                        try
                        {
                            var m = new Wacton.Unicolour.Munsell(hNum, hLetter, v, c);
                            color = new Unicolour(ColourSpace.Munsell, m.H, m.V, m.C);
                        }
                        catch
                        {
                            // 무효한 Munsell 값일 경우 무시
                        }
                    }
                }
                else if (textBox.Name == "XyzInputTextBox")
                {
                    var parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3 &&
                        double.TryParse(parts[0], out double x) &&
                        double.TryParse(parts[1], out double y) &&
                        double.TryParse(parts[2], out double z))
                    {
                        // 입력이 주로 0-100 범위로 들어오므로 100으로 나누어 Unicolour에 전달 (Unicolour의 기본 XYZ 스케일은 0-1)
                        color = new Unicolour(ColourSpace.Xyz, x / 100.0, y / 100.0, z / 100.0);
                    }
                }
                else if (textBox.Name == "LabInputTextBox")
                {
                    var parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3 &&
                        double.TryParse(parts[0], out double l) &&
                        double.TryParse(parts[1], out double a) &&
                        double.TryParse(parts[2], out double b))
                    {
                        color = new Unicolour(ColourSpace.Lab, l, a, b);
                    }
                }
                else if (textBox.Name == "CmykInputTextBox")
                {
                    var parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 4 &&
                        double.TryParse(parts[0], out double c) &&
                        double.TryParse(parts[1], out double m) &&
                        double.TryParse(parts[2], out double y) &&
                        double.TryParse(parts[3], out double k))
                    {
                        color = ConvertCmykToRgb(c, m, y, k);
                    }
                }
                else if (textBox.Name == "RalInputTextBox")
                {
                    var formattedInput = input.Replace(" ", "").ToUpper();
                    if (!formattedInput.StartsWith("RAL")) formattedInput = "RAL" + formattedInput;
                    
                    var match = RalColorHelper.Colors.Find(r => r.Code.Replace(" ", "").ToUpper() == formattedInput);
                    if (match != null)
                    {
                        color = new Unicolour(match.Hex);
                    }
                }
            }
            catch
            {
                // 파싱 실패 시 무시
                return;
            }

            if (color != null)
            {
                UpdateColorUI(color, textBox.Name);
            }
        }

        /// <summary>
        /// 성공적으로 파싱된 색상을 UI에 업데이트합니다.
        /// </summary>
        private void UpdateColorUI(Unicolour color, string sourceTextBoxName)
        {
            _isUpdating = true;

            try
            {
                // 1. 색상 미리보기 브러시 및 글자색 생성
                var rgbColor = color.Rgb;
                var mediaColor = System.Windows.Media.Color.FromRgb((byte)Math.Round(rgbColor.R * 255), (byte)Math.Round(rgbColor.G * 255), (byte)Math.Round(rgbColor.B * 255));
                var inputBrush = new SolidColorBrush(mediaColor);
                var inputForeground = (rgbColor.R * 0.299 + rgbColor.G * 0.587 + rgbColor.B * 0.114) > 0.5 
                    ? Brushes.Black : Brushes.White;

                RgbColorPreview.Background = inputBrush;
                ResultRgbText.Foreground = inputForeground;

                HexColorPreview.Background = inputBrush;
                ResultHexText.Foreground = inputForeground;

                MunsellColorPreview.Background = inputBrush;
                ResultMunsellText.Foreground = inputForeground;

                // 2. 결과 텍스트 업데이트
                ResultHexText.Text = color.Hex;
                ResultRgbText.Text = $"{(int)Math.Round(rgbColor.R * 255)}, {(int)Math.Round(rgbColor.G * 255)}, {(int)Math.Round(rgbColor.B * 255)}";
                
                // Munsell (HVC) 업데이트
                var munsell = color.Munsell; 
                ResultMunsellText.Text = munsell.ToString();

                // XYZ 업데이트
                var xyz = color.Xyz;
                string xyzStr = $"{xyz.X * 100:F1}, {xyz.Y * 100:F1}, {xyz.Z * 100:F1}";
                ResultXyzText.Text = xyzStr;

                // LAB 업데이트
                var lab = color.Lab;
                string labStr = $"{lab.L:F1}, {lab.A:F1}, {lab.B:F1}";
                ResultLabText.Text = labStr;

                // CMYK 업데이트
                var cmyk = ConvertRgbToCmyk(color);
                string cmykStr = $"{cmyk.C:F0}, {cmyk.M:F0}, {cmyk.Y:F0}, {cmyk.K:F0}";
                ResultCmykText.Text = cmykStr;

                // RAL 업데이트
                var closestRalInfo = RalColorHelper.GetClosestRalColor(color.Hex);
                if (closestRalInfo != null)
                {
                    var closestRal = closestRalInfo.Value.Color;
                    var diff = closestRalInfo.Value.Diff;
                    ResultRalText.Text = $"{closestRal.Code} ({closestRal.Name})";
                    RalDifferenceText.Text = $"가장 비슷한 색 (오차 ΔE: {diff:F1})";
                    
                    var ralRgb = new Unicolour(closestRal.Hex).Rgb;
                    var ralMediaColor = System.Windows.Media.Color.FromRgb((byte)Math.Round(ralRgb.R * 255), (byte)Math.Round(ralRgb.G * 255), (byte)Math.Round(ralRgb.B * 255));
                    RalColorPreview.Background = new SolidColorBrush(ralMediaColor);
                    ResultRalText.Foreground = (ralRgb.R * 0.299 + ralRgb.G * 0.587 + ralRgb.B * 0.114) > 0.5 
                        ? Brushes.Black : Brushes.White;
                }
                else
                {
                    ResultRalText.Text = "-";
                    RalDifferenceText.Text = "가장 비슷한 색";
                    RalColorPreview.Background = Brushes.Transparent;
                    ResultRalText.Foreground = Brushes.White;
                }

                // (먼셀 색상 미리보기는 위에서 입력 색상과 동일하게 설정됨)

                // 3. 입력창 동기화
                if (sourceTextBoxName != "HexInputTextBox")
                {
                    HexInputTextBox.Text = color.Hex;
                }
                if (sourceTextBoxName != "RgbInputTextBox")
                {
                    RgbInputTextBox.Text = ResultRgbText.Text;
                }
                if (sourceTextBoxName != "MunsellInputTextBox")
                {
                    MunsellInputTextBox.Text = munsell.ToString();
                }
                if (sourceTextBoxName != "XyzInputTextBox")
                {
                    XyzInputTextBox.Text = xyzStr;
                }
                if (sourceTextBoxName != "LabInputTextBox")
                {
                    LabInputTextBox.Text = labStr;
                }
                if (sourceTextBoxName != "CmykInputTextBox")
                {
                    CmykInputTextBox.Text = cmykStr;
                }
                if (sourceTextBoxName != "RalInputTextBox" && closestRalInfo != null)
                {
                    RalInputTextBox.Text = closestRalInfo.Value.Color.Code;
                }
            }
            catch (Exception)
            {
                // 에러 발생 시 처리
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// RGB 값을 CMYK 값(0-100)으로 변환합니다. (기본 표준 공식 적용)
        /// </summary>
        private (double C, double M, double Y, double K) ConvertRgbToCmyk(Unicolour color)
        {
            var r = color.Rgb.R;
            var g = color.Rgb.G;
            var b = color.Rgb.B;

            var k = 1.0 - Math.Max(Math.Max(r, g), b);
            if (k == 1.0) return (0, 0, 0, 100);

            var c = (1.0 - r - k) / (1.0 - k);
            var m = (1.0 - g - k) / (1.0 - k);
            var y = (1.0 - b - k) / (1.0 - k);

            return (c * 100, m * 100, y * 100, k * 100);
        }

        /// <summary>
        /// CMYK 값(0-100)을 RGB 색상공간을 사용하는 Unicolour 객체로 변환합니다.
        /// </summary>
        private Unicolour ConvertCmykToRgb(double c, double m, double y, double k)
        {
            c = Math.Clamp(c / 100.0, 0, 1);
            m = Math.Clamp(m / 100.0, 0, 1);
            y = Math.Clamp(y / 100.0, 0, 1);
            k = Math.Clamp(k / 100.0, 0, 1);

            var r = (1.0 - c) * (1.0 - k);
            var g = (1.0 - m) * (1.0 - k);
            var b = (1.0 - y) * (1.0 - k);

            return new Unicolour(ColourSpace.Rgb, r, g, b);
        }
    }
}