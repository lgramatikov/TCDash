using ClassLibrary1;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using org.laz.TCDashboardInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.AppService;
using Windows.Devices.AllJoyn;
using Windows.Devices.Gpio;
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

// Color paletter from http://www.colourlovers.com/palette/3866893/hot_and_cold

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page//, IDisposable
    {
        static Random randomizer = new Random();

        Build[,] buildsGridData;

        AppServiceConnection appServiceConnection;

        AppServiceConnection whAppServiceConnection;

        private const int PIR_SENSOR_PIN = 4;
        private GpioPin pirStatusPin;

        private bool motionDetected = false;

        //private DispatcherTimer gpioPollTimer;

        private bool isBusAttachmentConnected = false;

        private AllJoynBusAttachment busAttachment = null;
        private TCDashboardInterfaceProducer ajTCDashBoardProducer = null;

        public static MainPage Current;

        bool funkyModeIsOn = false;

        TelemetryClient telemetry;

        public MainPage()
        {
            this.InitializeComponent();
            telemetry = new TelemetryClient();

            this.buildsGridData = new Build[4, 5];  //four rows, five columns

            InitAppSvc();

            this.InitializeGPIO();
            this.Loaded += MainPage_Loaded;
            Current = this;
        }

        private void SetupBuildsGrid(List<Build> builds)
        {
            Array.Clear(this.buildsGridData, 0, this.buildsGridData.Length);
            foreach (var b in builds)
            {
                var emptyCell = GetRandomEmptyCell(this.buildsGridData);
                this.buildsGridData[emptyCell.Item1, emptyCell.Item2] = b;
            }
        }

        private Tuple<int, int> GetRandomEmptyCell(Build[,] buildsGridData)
        {
            var row = randomizer.Next(0, 4);
            var column = randomizer.Next(0, 5);

            Debug.WriteLine($"Got row: {row}, column: {column}");

            if ((buildsGridData[row, column] != null) || ((row == 0) && (column == 4)))
            {
                Debug.WriteLine($"Location at row: {row}, column: {column} is already occupied, trying again.");
                return GetRandomEmptyCell(buildsGridData);
            }

            return new Tuple<int, int>(row, column);
        }

        private void DrawBuildsGrid()
        {
            if (LoadingBar.Visibility == Visibility.Visible)
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
                    if (this.buildsGridData[row, column] != null)
                    {
                        var r = DrawBuildItem(this.buildsGridData[row, column]);

                        BuildsGrid.Children.Add(r);
                        Grid.SetRow(r, row);
                        Grid.SetColumn(r, column);
                    }

                    if ((this.motionDetected == true) && (row == 0) && (column == 4))
                    {
                        var r = DrawHelloItem();
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
            byte alphaChannel = 200;

            if (this.motionDetected)
            {
                alphaChannel = 255;
            }

            if (build.Status.Equals("RUNNING", StringComparison.OrdinalIgnoreCase))
            {
                brush.GradientStops = new GradientStopCollection
                    {
                        new GradientStop
                        {
                            Color = funkyModeIsOn? Color.FromArgb(200, (byte)randomizer.Next(254), (byte)randomizer.Next(254), (byte)randomizer.Next(254)) : Color.FromArgb(alphaChannel, 21, 86, 104),
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
                            Color = funkyModeIsOn? Color.FromArgb(200, (byte)randomizer.Next(254), (byte)randomizer.Next(254), (byte)randomizer.Next(254)) : Color.FromArgb(alphaChannel, 136, 196, 37),
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
                            Color = funkyModeIsOn ? Color.FromArgb(200, (byte)randomizer.Next(254), (byte)randomizer.Next(254), (byte)randomizer.Next(254)) : Color.FromArgb(alphaChannel, 204, 45, 0),
                            Offset = 0
                        }
                    };
            };

            if (build.PreviousStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                brush.GradientStops[0].Offset = 0.9;
                brush.GradientStops.Add(new GradientStop
                {
                    Color = funkyModeIsOn ? Color.FromArgb(200, (byte)randomizer.Next(254), (byte)randomizer.Next(254), (byte)randomizer.Next(254)) : Color.FromArgb(alphaChannel, 136, 196, 37),//Color.FromArgb(alphaChannel, 136, 196, 37),
                    Offset = 0.9
                });
            }
            else
            {
                brush.GradientStops[0].Offset = 0.9;
                brush.GradientStops.Add(new GradientStop
                {
                    Color = funkyModeIsOn ? Color.FromArgb(200, (byte)randomizer.Next(254), (byte)randomizer.Next(254), (byte)randomizer.Next(254)) : Color.FromArgb(alphaChannel, 204, 45, 0),
                    Offset = 0.9
                });
            };

            var b = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Background = brush, //funkyModeIsOn ? new SolidColorBrush(Color.FromArgb(200, (byte)randomizer.Next(254), (byte)randomizer.Next(254), (byte)randomizer.Next(254))) : brush,//new SolidColorBrush(Color.FromArgb(200, (byte)randomizer.Next(254), (byte)randomizer.Next(254), (byte)randomizer.Next(254))), //brush,//
                Margin = new Thickness(2),
                Padding = new Thickness(5)
            };

            //if (funkyModeIsOn)
            //{
            //    b.Background = new SolidColorBrush(Color.FromArgb(200, (byte)randomizer.Next(254), (byte)randomizer.Next(254), (byte)randomizer.Next(254)));
            //}
            //else
            //{
            //    b.Background = brush;
            //}

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
                Text = "#" + build.Number,
                FontSize = 32
            });

            text.Inlines.Add(new LineBreak());

            text.Inlines.Add(new Run
            {
                Text = "Updated: " + build.TimeStamp.ToString("dd MMM yyyy, HH:mm"),
                FontSize = build.PreviousStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) ? 22 : 18//18
            });

            b.Child = text;

            return b;
        }

        private Border DrawHelloItem()
        {
            var brush = new SolidColorBrush(Color.FromArgb(200, 255, 229, 69));

            var b = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Background = brush,
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
                Text = "Oh, hello there!",
                FontSize = 32
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

            whAppServiceConnection = new AppServiceConnection();
            whAppServiceConnection.PackageFamilyName = "WHTaskConnectApp_vayj3az00mxpp";
            whAppServiceConnection.AppServiceName = "org.laz.tcwebhook";

            // Send a initialize request 
            var res = await appServiceConnection.OpenAsync();
            if (res == AppServiceConnectionStatus.Success)
            {
                var message = new ValueSet();
                message.Add("Command", "DashboardReady");
                var response = await appServiceConnection.SendMessageAsync(message);
                if (response.Status != AppServiceResponseStatus.Success)
                {
                    telemetry.TrackEvent("Laz.Org.AppService.Msg.Fail.App2AppComService");
                    throw new Exception("Failed to send message");
                }

                Debug.WriteLine("AppServiceConnection initialized");
                telemetry.TrackEvent("Laz.Org.AppService.Conn.Success.App2AppComService");
                appServiceConnection.RequestReceived += OnMessageReceived;
            }
            else
            {
                Debug.WriteLine("Failed to initialize AppServiceConnection");
                telemetry.TrackEvent("Laz.Org.AppService.Conn.Fail.App2AppComService");
            }


            //---
            var whRes = await whAppServiceConnection.OpenAsync();
            if (whRes == AppServiceConnectionStatus.Success)
            {
                var whMessage = new ValueSet();
                whMessage.Add("Command", "DashboardReady");

                var whResponse = await whAppServiceConnection.SendMessageAsync(whMessage);
                if (whResponse.Status != AppServiceResponseStatus.Success)
                {
                    telemetry.TrackEvent("Laz.Org.AppService.Msg.Fail.tcwebhook");
                    throw new Exception("Failed to send message to WH service");
                }

                whAppServiceConnection.RequestReceived += WhAppServiceConnection_RequestReceived;
                telemetry.TrackEvent("Laz.Org.AppService.Conn.Success.tcwebhook");
            }
            else
            {
                Debug.WriteLine("Failed to connect to WH app service connection");
                telemetry.TrackEvent("Laz.Org.AppServiceInitialize.Conn.Fail.tcwebhook");
            }

        }

        private async void WhAppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            Debug.WriteLine(message);
            var build = message["Build"] as string;
            var b = JsonConvert.DeserializeObject<Build>(build);

            for (byte i = 0; i < 4; i++)
            {
                for (byte j = 0; j < 5; j++)
                {
                    if ((this.buildsGridData[i, j] != null) && (this.buildsGridData[i, j].Id.Equals(b.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        this.buildsGridData[i, j] = b;
                        break;
                    }
                }
            }

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DrawBuildsGrid();
            });

            //this.buildsGridData = new Build[4, 5];  //four rows, five columns
        }

        private async void OnMessageReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            var buildsState = message["Builds"] as string;
            var b = JsonConvert.DeserializeObject<List<Build>>(buildsState);
            Debug.WriteLine(buildsState);

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SetupBuildsGrid(b);
                DrawBuildsGrid();
            });
        }

        // Handling GPIO is not strictly dashboard concern. I think we should be able to handle this with background task. On other hand, this is for the show. App to app communication
        // may or may not always work. But as this is a hardware demo, it should always work (or at least, we should make sure it is as robust as possible). Hence, code is here.
        private void InitializeGPIO()
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                // GpioStatus.Text = "There is no GPIO controller on this device. 2";
                System.Diagnostics.Debug.WriteLine("There is no GPIO controller on this device.");
                telemetry.TrackEvent("Laz.Org.GPIO.NotFound");
                return;
            }
            else
            {
                //GpioStatus.Text = "Found GPIO and will use it. 4";
                System.Diagnostics.Debug.WriteLine("Found GPIO and will use it.");
                telemetry.TrackEvent("Laz.Org.GPIO.Found");
            }

            pirStatusPin = gpio.OpenPin(PIR_SENSOR_PIN);
            pirStatusPin.SetDriveMode(GpioPinDriveMode.Input);
            pirStatusPin.ValueChanged += PirStatusPin_ValueChanged;

            //gpioPollTimer = new DispatcherTimer();
            //gpioPollTimer.Interval = TimeSpan.FromMilliseconds(500);
            //gpioPollTimer.Tick += GpioPollTimer_Tick;
            //gpioPollTimer.Start();

            //pirStatusPin.Dispose();
        }

        //Allright, what will happen if we ask the grid to render while it is rendering because of new build data? Crash? Nothing?
        private async void PirStatusPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (args.Edge.CompareTo(GpioPinEdge.RisingEdge) == 0)
                {
                    //PirStatus.Text = "PIR sensed movement";
                    System.Diagnostics.Debug.WriteLine("PIR sensed movement");
                    motionDetected = true;
                    DrawBuildsGrid();
                    telemetry.TrackEvent("Laz.Org.PIR.Activated");
                }
                else
                {
                    //PirStatus.Text = "No movement";
                    System.Diagnostics.Debug.WriteLine("No movement");
                    motionDetected = false;
                    DrawBuildsGrid();
                    telemetry.TrackEvent("Laz.Org.PIR.Deactivated");
                }
            });
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //var busAttachment = new AllJoynBusAttachment();
            //busAttachment.AuthenticationMechanisms.Add(AllJoynAuthenticationMechanism.SrpAnonymous);

            //var producer = new TCDashboardInterfaceProducer(busAttachment);
            //producer.Service = new FunkyDashboardService();

            //producer.Start();
            StartAJProducer();
        }

        private void StartAJProducer()
        {
            // Prevent launching the producer again if it is already launched.
            if (isBusAttachmentConnected)
            {
                System.Diagnostics.Debug.Write("Bus already connected. Nothing to do.");
                telemetry.TrackEvent("Laz.Org.AllJoyn.BusFound");
            }
            else
            {
                System.Diagnostics.Debug.Write("Will start new bus attachment.");
                busAttachment = new AllJoynBusAttachment();
                busAttachment.StateChanged += BusAttachment_StateChanged;

                // Optional - Populate About data for the producer.
                busAttachment.AboutData.DefaultManufacturer = "Laz";
                busAttachment.AboutData.SoftwareVersion = "0.0.1";
                busAttachment.AboutData.DefaultAppName = "TeamCity Dashboard";
                busAttachment.AboutData.SupportUrl = new Uri("http://www.sharenkoto.org");

                // Initialize the producer object generated by the AllJoynCodeGenerator tool.
                ajTCDashBoardProducer = new TCDashboardInterfaceProducer(busAttachment);

                // Instantiate SecureInterfaceService which will handle the concatenation method calls.
                ajTCDashBoardProducer.Service = new FunkyDashboardService();

                // Start advertising the service.
                ajTCDashBoardProducer.Start();
                telemetry.TrackEvent("Laz.Org.AllJoyn.BusStarted");
            }

        }

        private void BusAttachment_StateChanged(AllJoynBusAttachment sender, AllJoynBusAttachmentStateChangedEventArgs args)
        {
            switch (args.State)
            {
                case AllJoynBusAttachmentState.Disconnected:
                    System.Diagnostics.Debug.Write(String.Format("Disconnected from the AllJoyn bus attachment with AllJoyn status: 0x{0:X}.", args.Status));
                    isBusAttachmentConnected = false;
                    break;
                case AllJoynBusAttachmentState.Connected:
                    System.Diagnostics.Debug.Write("Launched.");
                    isBusAttachmentConnected = true;
                    break;
                default:
                    break;
            }

        }

        public void GoFunky()
        {
            funkyModeIsOn = true;
            DrawBuildsGrid();
        }

        public void GoBoring()
        {
            funkyModeIsOn = false;
            DrawBuildsGrid();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (ajTCDashBoardProducer != null)
                    {
                        ajTCDashBoardProducer.Stop();
                        ajTCDashBoardProducer.Dispose();
                    }

                    if (busAttachment != null)
                    {
                        busAttachment.StateChanged -= BusAttachment_StateChanged;
                        busAttachment.Disconnect();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        //private void btnMovementOn_Click(object sender, RoutedEventArgs e)
        //{
        //    motionDetected = true;
        //    DrawBuildsGrid();
        //    //BuildsGrid.Children[]

        //}

        //private void btnMovementOff_Click(object sender, RoutedEventArgs e)
        //{
        //    motionDetected = false;
        //    DrawBuildsGrid();
        //}

        //private void GpioPollTimer_Tick(object sender, object e)
        //{
        //    GpioPinValue val = pirStatusPin.Read();
        //    if (val==GpioPinValue.High)
        //    {
        //        PirStatus.Text = "PIR sensed movement";
        //    }
        //    else
        //    {
        //        PirStatus.Text = "No movement";
        //    }
        //}

        //#region IDisposable Support
        //private bool disposedValue = false; // To detect redundant calls

        //void Dispose(bool disposing)
        //{
        //    if (!disposedValue)
        //    {
        //        if (disposing)
        //        {
        //            // TODO: dispose managed state (managed objects).
        //            pirStatusPin.Dispose();
        //        }

        //        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        //        // TODO: set large fields to null.

        //        disposedValue = true;
        //    }
        //}

        //// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        //// ~MainPage() {
        ////   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        ////   Dispose(false);
        //// }

        //// This code added to correctly implement the disposable pattern.
        //public void Dispose()
        //{
        //    // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //    Dispose(true);
        //    // TODO: uncomment the following line if the finalizer is overridden above.
        //    // GC.SuppressFinalize(this);
        //}
        //#endregion
    }
}
