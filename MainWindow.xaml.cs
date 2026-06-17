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

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 이전 버전 업데이트 백업 파일 자동 정리
                string currentExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                string backupExe = currentExe + ".bak";
                if (System.IO.File.Exists(backupExe))
                {
                    try { System.IO.File.Delete(backupExe); } catch { }
                }

                // GitHub Releases 자동 업데이트 설정 (NetSparkleUpdater)
                string appcastUrl = "https://raw.githubusercontent.com/iy7847/Color-Code/main/appcast.xml";
                _sparkle = new SparkleUpdater(appcastUrl, new Ed25519Checker(SecurityMode.Unsafe))
                {
                    UIFactory = new NetSparkleUpdater.UI.WPF.UIFactory()
                };

                // 조용히 업데이트 확인
                var updateInfo = await _sparkle.CheckForUpdatesQuietly();
                if (updateInfo.Status == NetSparkleUpdater.Enums.UpdateStatus.UpdateAvailable && updateInfo.Updates.Count > 0)
                {
                    var latestVersion = updateInfo.Updates[0];
                    
                    // 새 업데이트 알림을 위한 커스텀 창 생성 (업데이트 내역 포함)
                    var promptWindow = new System.Windows.Window
                    {
                        Title = "새로운 업데이트 알림",
                        Width = 450,
                        Height = 320,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                        ResizeMode = System.Windows.ResizeMode.NoResize,
                        WindowStyle = System.Windows.WindowStyle.ToolWindow,
                        Topmost = true,
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 32, 32)),
                        Foreground = System.Windows.Media.Brushes.White
                    };

                    var stackPanel = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(20) };
                    
                    var titleText = new System.Windows.Controls.TextBlock 
                    { 
                        Text = $"🎉 새 업데이트(v{latestVersion.Version})가 출시되었습니다!", 
                        FontSize = 16, 
                        FontWeight = System.Windows.FontWeights.Bold,
                        Margin = new System.Windows.Thickness(0, 0, 0, 15)
                    };
                    
                    var notesLabel = new System.Windows.Controls.TextBlock 
                    { 
                        Text = "업데이트 내용:", 
                        FontSize = 13, 
                        Margin = new System.Windows.Thickness(0, 0, 0, 5),
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200))
                    };
                    
                    var notesBox = new System.Windows.Controls.TextBox 
                    { 
                        Text = string.IsNullOrWhiteSpace(latestVersion.Description) ? "업데이트 내역이 제공되지 않았습니다." : latestVersion.Description.Trim(),
                        IsReadOnly = true,
                        TextWrapping = System.Windows.TextWrapping.Wrap,
                        VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                        Height = 120,
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                        Foreground = System.Windows.Media.Brushes.White,
                        BorderThickness = new System.Windows.Thickness(1),
                        BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80)),
                        Padding = new System.Windows.Thickness(10)
                    };

                    var btnPanel = new System.Windows.Controls.StackPanel 
                    { 
                        Orientation = System.Windows.Controls.Orientation.Horizontal, 
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        Margin = new System.Windows.Thickness(0, 20, 0, 0)
                    };
                    
                    bool isUpdateAccepted = false;
                    
                    var btnUpdate = new System.Windows.Controls.Button 
                    { 
                        Content = "지금 업데이트", 
                        Width = 120, 
                        Height = 32, 
                        Margin = new System.Windows.Thickness(0, 0, 10, 0),
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215)),
                        Foreground = System.Windows.Media.Brushes.White,
                        BorderThickness = new System.Windows.Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    btnUpdate.Click += (s, e) => { isUpdateAccepted = true; promptWindow.Close(); };
                    
                    var btnCancel = new System.Windows.Controls.Button 
                    { 
                        Content = "나중에", 
                        Width = 80, 
                        Height = 32,
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 70, 70)),
                        Foreground = System.Windows.Media.Brushes.White,
                        BorderThickness = new System.Windows.Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    btnCancel.Click += (s, e) => { promptWindow.Close(); };

                    btnPanel.Children.Add(btnUpdate);
                    btnPanel.Children.Add(btnCancel);
                    
                    stackPanel.Children.Add(titleText);
                    stackPanel.Children.Add(notesLabel);
                    stackPanel.Children.Add(notesBox);
                    stackPanel.Children.Add(btnPanel);
                    
                    promptWindow.Content = stackPanel;
                    promptWindow.ShowDialog();

                    if (isUpdateAccepted)
                    {
                        // 윈도우 UI 다크테마 적용 대신 자체 다운로더 및 업데이트 로직 실행
                        var downloadUrl = latestVersion.DownloadLink;
                        var tempZip = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ColorCodePicker_Update.zip");

                        // 진행 상태를 보여줄 커스텀 프로그레스바 윈도우 생성
                        var updateWindow = new System.Windows.Window
                        {
                            Title = "Color Code 업데이트",
                            Width = 450,
                            Height = 180,
                            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                            ResizeMode = System.Windows.ResizeMode.NoResize,
                            WindowStyle = System.Windows.WindowStyle.ToolWindow,
                            Topmost = true,
                            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 32, 32)),
                            Foreground = System.Windows.Media.Brushes.White
                        };

                        var grid = new System.Windows.Controls.Grid { Margin = new System.Windows.Thickness(20) };
                        var textBlock = new System.Windows.Controls.TextBlock 
                        { 
                            Text = "업데이트 파일을 다운로드하는 중입니다...\n잠시만 기다려주세요.", 
                            VerticalAlignment = System.Windows.VerticalAlignment.Top,
                            FontSize = 14,
                            Foreground = System.Windows.Media.Brushes.White
                        };
                        var progressBar = new System.Windows.Controls.ProgressBar 
                        { 
                            Height = 24, 
                            VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                            Minimum = 0,
                            Maximum = 100,
                            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215))
                        };
                        grid.Children.Add(textBlock);
                        grid.Children.Add(progressBar);
                        updateWindow.Content = grid;
                        updateWindow.Show();
                        
                        string currentExePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using (var client = new System.Net.Http.HttpClient())
                                {
                                    using (var response = await client.GetAsync(downloadUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead))
                                    {
                                        response.EnsureSuccessStatusCode();
                                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                                        using (var fs = new System.IO.FileStream(tempZip, System.IO.FileMode.Create))
                                        using (var stream = await response.Content.ReadAsStreamAsync())
                                        {
                                            var buffer = new byte[81920];
                                            long totalRead = 0;
                                            int bytesRead;
                                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                            {
                                                await fs.WriteAsync(buffer, 0, bytesRead);
                                                totalRead += bytesRead;
                                                if (totalBytes != -1)
                                                {
                                                    var percent = (int)((double)totalRead / totalBytes * 100);
                                                    Application.Current.Dispatcher.Invoke(() => {
                                                        progressBar.Value = percent;
                                                        textBlock.Text = $"다운로드 중... ({percent}%)  [ {(totalRead/1024/1024)}MB / {(totalBytes/1024/1024)}MB ]\n잠시만 기다려주세요.";
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }

                                Application.Current.Dispatcher.Invoke(() => {
                                    progressBar.IsIndeterminate = true;
                                    textBlock.Text = "설치 파일 압축 해제 중...\n거의 다 완료되었습니다!";
                                });

                                string bakPath = currentExePath + ".bak";

                                // 이전 백업 파일이 있으면 삭제
                                if (System.IO.File.Exists(bakPath))
                                    System.IO.File.Delete(bakPath);

                                // 현재 실행 중인 파일 이름을 변경
                                System.IO.File.Move(currentExePath, bakPath);

                                Application.Current.Dispatcher.Invoke(() => {
                                    textBlock.Text = "새 버전 적용 중...";
                                });

                                // ZIP 압축 해제 후 현재 폴더로 복사 (.NET Preview 버그 우회용 PowerShell 사용)
                                string extractDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ColorCodePicker_Extract");
                                if (System.IO.Directory.Exists(extractDir))
                                    System.IO.Directory.Delete(extractDir, true);
                                
                                var psInfo = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "powershell.exe",
                                    Arguments = $"-NoProfile -Command \"Expand-Archive -Force -Path '{tempZip}' -DestinationPath '{extractDir}'\"",
                                    UseShellExecute = true,
                                    CreateNoWindow = true,
                                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                                };
                                var ps = System.Diagnostics.Process.Start(psInfo);
                                ps?.WaitForExit();
                                
                                string newExe = System.IO.Directory.GetFiles(extractDir, "*.exe").FirstOrDefault() ?? "";
                                if (!string.IsNullOrEmpty(newExe))
                                {
                                    System.IO.File.Copy(newExe, currentExePath, true);
                                    
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        updateWindow.Close();
                                        // 새 버전 실행
                                        System.Diagnostics.Process.Start(currentExePath);
                                        // 현재 앱 종료
                                        Application.Current.Shutdown();
                                    });
                                }
                                else
                                {
                                    throw new Exception("압축 파일 안에 실행 파일(.exe)을 찾을 수 없습니다.");
                                }
                            }
                            catch (Exception ex)
                            {
                                string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ColorCodePicker_UpdateError.txt");
                                System.IO.File.WriteAllText(logPath, ex.ToString());
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    updateWindow.Close();
                                    System.Windows.MessageBox.Show("업데이트 중 오류가 발생했습니다:\n" + ex.Message + "\n\n자세한 로그가 다음 경로에 저장되었습니다:\n" + logPath, "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                                });
                            }
                        });
                    }
                }
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