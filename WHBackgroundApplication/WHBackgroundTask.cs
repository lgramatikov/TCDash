using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.AppService;
using Windows.System.Threading;
using Windows.Networking.Sockets;
using System.IO;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Newtonsoft.Json;
using ClassLibrary1;
//using Microsoft.ApplicationInsights;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace WHBackgroundApplication
{
    public sealed class WHBackgroundTask : IBackgroundTask
    {
        BackgroundTaskDeferral serviceDeferral;
        AppServiceConnection appServiceConnection;

        //private TelemetryClient tc = new TelemetryClient();
        //private const string AI_KEY = "4105be01-83a6-4371-a412-7c05dd329729";

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Associate a cancellation handler with the background task. 
            taskInstance.Canceled += OnCanceled;

            // Get the deferral object from the task instance
            serviceDeferral = taskInstance.GetDeferral();

            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService != null && appService.Name == "org.laz.tcwebhook")
            {
                appServiceConnection = appService.AppServiceConnection;
                appServiceConnection.RequestReceived += OnRequestReceived;
            }

            //tc.InstrumentationKey = AI_KEY;

            //// Set session data:
            //tc.Context.Session.Id = Guid.NewGuid().ToString();
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;

            switch (command)
            {
                case "DashboardReady":
                    {
                        var messageDeferral = args.GetDeferral();
                        //Set a result to return to the caller
                        var returnMessage = new ValueSet();
                        HttpServer server = new HttpServer(8765, appServiceConnection);
                        IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                            (workItem) =>
                            {
                                server.StartServer();
                            });
                        returnMessage.Add("Status", "Success");
                        var responseStatus = await args.Request.SendResponseAsync(returnMessage);
                        messageDeferral.Complete();
                        break;
                    }

                case "Quit":
                    {
                        //Service was asked to quit. Give us service deferral
                        //so platform can terminate the background task
                        serviceDeferral.Complete();
                        break;
                    }
            }
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //Clean up and get ready to exit
        }
    }

    public sealed class HttpServer : IDisposable
    {
        private const uint BufferSize = 10240;
        private int port = 8765;
        private readonly StreamSocketListener listener;
        private AppServiceConnection appServiceConnection;

        //private TelemetryClient tc = new TelemetryClient();
        //private const string AI_KEY = "4105be01-83a6-4371-a412-7c05dd329729";

        public HttpServer(int serverPort, AppServiceConnection connection)
        {
            listener = new StreamSocketListener();
            port = serverPort;
            appServiceConnection = connection;
            listener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);

            //tc.InstrumentationKey = AI_KEY;

            //// Set session data:
            //tc.Context.Session.Id = Guid.NewGuid().ToString();
        }

        public void StartServer()
        {
#pragma warning disable CS4014
            listener.BindServiceNameAsync(port.ToString());
#pragma warning restore CS4014
        }

        public void Dispose()
        {
            listener.Dispose();
        }

        private async void ProcessRequestAsync(StreamSocket socket)
        {
            // this works for text only
            StringBuilder request = new StringBuilder();
            using (IInputStream input = socket.InputStream)
            {
                byte[] data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer();
                uint dataRead = BufferSize;
                while (dataRead == BufferSize)
                {
                    await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                    dataRead = buffer.Length;
                }

            }

            //curl -X POST -d @wh.txt http://192.168.1.119:8765 --header "Content-Type:application/json" -v -H "Expect:"

            var completeRequestParts = request.ToString().Split('\n');
            string requestMethod = completeRequestParts[0];
            string requestBody = completeRequestParts[completeRequestParts.Length - 1];

            TeamCityWebHookMessage tcMessage = null;

            string[] requestParts = requestMethod.Split(' ');

            if (requestParts[0] == "POST")
            {
                try
                {
                    tcMessage = JsonConvert.DeserializeObject<TeamCityWebHookMessage>(requestBody);

                    if ((tcMessage != null) && (tcMessage.build != null))
                    {
                        var b = new Build
                        {
                            Id = tcMessage.build.buildTypeId,
                            Number = tcMessage.build.buildNumber,
                            Name = tcMessage.build.buildName,
                            PreviousStatus = tcMessage.build.buildResultPrevious.ToUpperInvariant(),
                            ProjectName = tcMessage.build.projectName,
                            Status = tcMessage.build.buildStatus,
                            TimeStamp = DateTime.Now
                        };

                        var updateMessage = new ValueSet();
                        updateMessage.Add("Build", JsonConvert.SerializeObject(b));
                        var responseStatus = await appServiceConnection.SendMessageAsync(updateMessage);
                    }

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(ex.ToString());
                    //tc.TrackException(ex);
                    //tc.Flush();
                }
            }

            using (IOutputStream output = socket.OutputStream)
            {
                if (requestParts[0] == "POST")
                {
                    await WriteResponseAsync(requestParts[1], output);
                }
                else
                {
                    await WriteUnsupportedResponseAsync(output);
                }
            }
        }

        private async Task WriteResponseAsync(string request, IOutputStream os)
        {
            using (Stream resp = os.AsStreamForWrite())
            {
                MemoryStream stream = new MemoryStream();
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                  "Content-Length: 0\r\n" +
                                  "Connection: close\r\n\r\n",
                                  stream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
            }
        }

        private async Task WriteUnsupportedResponseAsync(IOutputStream os)
        {
            using (Stream resp = os.AsStreamForWrite())
            {
                MemoryStream stream = new MemoryStream();
                string header = String.Format("HTTP/1.1 405 Method Not Allowed\r\n" +
                                  "Allow: POST",
                                  "Content-Length: 0\r\n" +
                                  "Connection: close\r\n\r\n",
                                  stream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
            }
        }
    }
}
