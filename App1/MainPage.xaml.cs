using ClassLibrary1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static Random randomizer = new Random();

        Build[,] buildsGridData;

        AppServiceConnection appServiceConnection;

        public MainPage()
        {
            this.InitializeComponent();

            this.buildsGridData = new Build[4, 5];  //four rows, five columns

            InitAppSvc();
        }

        private void SetupBuildsGrid(List<Build> builds, Build[,] buildsGridData)
        {
            Array.Clear(buildsGridData, 0, buildsGridData.Length);
            foreach (var b in builds)
            {
                var emptyCell = GetRandomEmptyCell(buildsGridData);
                buildsGridData[emptyCell.Item1, emptyCell.Item2] = b;
            }
        }

        private Tuple<int, int> GetRandomEmptyCell(Build[,] buildsGridData)
        {
            var row = randomizer.Next(0, 4);
            var column = randomizer.Next(0, 5);

            Debug.WriteLine($"Got row: {row}, column: {column}");

            if ((buildsGridData[row, column] != null) || ((row==0) && (column==4)))
            {
                Debug.WriteLine($"Location at row: {row}, column: {column} is already occupied, trying again.");
                return GetRandomEmptyCell(buildsGridData);
            }

            return new Tuple<int, int>(row, column);
        }

        private void DrawBuildsGrid(Build[,] buildsGridData)
        {
            if (LoadingBar.Visibility==Visibility.Visible)
            {
                LoadingBar.Visibility = Visibility.Collapsed;
            }
            if (LoadingBar.IsEnabled)
            {
                LoadingBar.IsEnabled = false;
            }

            //this, should, maybe, eventually, most probably remove all old items from the grid.
            BuildsGrid.Children.Clear();

            for (byte row = 0; row < 4; row++)
            {
                for (byte column = 0; column < 5; column++)
                {
                    if (buildsGridData[row, column] != null)
                    {
                        var r = DrawBuildItem(buildsGridData[row, column]);

                        BuildsGrid.Children.Add(r);
                        Grid.SetRow(r, row);
                        Grid.SetColumn(r, column);
                    }
                }
            }
        }

        private Border DrawBuildItem(Build build)
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);

            if (build.Status.Equals("RUNNING", StringComparison.OrdinalIgnoreCase))
            {
                brush.GradientStops = new GradientStopCollection
                    {
                        new GradientStop
                        {
                            Color = Color.FromArgb(200, 66, 134, 168),
                            Offset = 0
                        }
                    };
            }
            else if (build.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                brush.GradientStops = new GradientStopCollection
                    {
                        new GradientStop
                        {
                            Color = Color.FromArgb(200, 0, 255, 0),
                            Offset = 0
                        }
                    };
            }
            else 
            {
                brush.GradientStops = new GradientStopCollection
                    {
                        new GradientStop
                        {
                            Color = Color.FromArgb(200, 255, 0, 0),
                            Offset = 0
                        }
                    };
            };

            if (build.PreviousStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                brush.GradientStops[0].Offset = 0.9;
                brush.GradientStops.Add(new GradientStop
                {
                    Color = Color.FromArgb(200, 0, 255, 0),
                    Offset = 0.9
                });
            }
            else
            {
                brush.GradientStops[0].Offset = 0.9;
                brush.GradientStops.Add(new GradientStop
                {
                    Color = Color.FromArgb(200, 255, 0, 0),
                    Offset = 0.9
                });
            };

            var b = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Background = brush,//new SolidColorBrush(Color.FromArgb(200, (byte)randomizer.Next(254), (byte)randomizer.Next(254), (byte)randomizer.Next(254))), //brush,//
                Margin = new Thickness(2),
                Padding = new Thickness(5)
            };

            var text = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                FontSize = 32
            };

            text.Inlines.Add(new Run
            {
                Text = build.ProjectName,
                FontSize = 32
            });

            text.Inlines.Add(new LineBreak());

            text.Inlines.Add(new Run
            {
                Text = build.Name,
                FontSize = 42
            });

            text.Inlines.Add(new LineBreak());

            text.Inlines.Add(new Run
            {
                Text = "#"+build.Number,
                FontSize = 32
            });

            text.Inlines.Add(new LineBreak());

            text.Inlines.Add(new Run
            {
                Text = "Updated: "+build.TimeStamp.ToString("dd MMM yyyy, HH:mm"),
                FontSize = build.PreviousStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) ? 22 : 18//18
            });

            b.Child = text;

            return b;
        }

        private async void InitAppSvc()
        {
            // Initialize the AppServiceConnection
            appServiceConnection = new AppServiceConnection();
            appServiceConnection.PackageFamilyName = "fe84f789-e7b7-4c2e-b960-955e3aab0ea0_vayj3az00mxpp";
            appServiceConnection.AppServiceName = "App2AppComService";

            // Send a initialize request 
            var res = await appServiceConnection.OpenAsync();
            if (res == AppServiceConnectionStatus.Success)
            {
                var message = new ValueSet();
                message.Add("Command", "DashboardReady");
                var response = await appServiceConnection.SendMessageAsync(message);
                if (response.Status != AppServiceResponseStatus.Success)
                {
                    throw new Exception("Failed to send message");
                }

                Debug.WriteLine("AppServiceConnection initialized");
                appServiceConnection.RequestReceived += OnMessageReceived;
            }
            else
            {
                Debug.WriteLine("Failed to inSitialize AppServiceConnection");
            }
        }

        private async void OnMessageReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            var buildsState = message["Builds"] as string;
            var b = JsonConvert.DeserializeObject<List<Build>>(buildsState);
            Debug.WriteLine(buildsState);

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SetupBuildsGrid(b, this.buildsGridData);
                DrawBuildsGrid(this.buildsGridData);
            });
        }
    }
}
