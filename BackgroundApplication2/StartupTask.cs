using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using System.Diagnostics;
using Windows.System.Threading;
using ClassLibrary1;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace BackgroundApplication2
{
    public sealed class StartupTask : IBackgroundTask
    {
        AppServiceConnection appServiceConnection;
        BackgroundTaskDeferral serviceDeferral;
        ThreadPoolTimer timer;

        private const string serverHost = "";
        private const string basicAuthString = "";

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            serviceDeferral = taskInstance.GetDeferral();

            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService != null && appService.Name == "App2AppComService")
            {
                appServiceConnection = appService.AppServiceConnection;
                appServiceConnection.RequestReceived += OnRequestReceived;
            }
        }

        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;

            if (command.Equals("DashboardReady", StringComparison.OrdinalIgnoreCase))
            {
                //get initial data
                Notify();

#if DEBUG
                this.timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromSeconds(20));
#else
                this.timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMinutes(15));
#endif
            }
        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            Notify();
        }

        private async void Notify()
        {
            var updateMessage = new ValueSet();
            
            var builds = await GetBuilds();

            updateMessage.Add("Builds", JsonConvert.SerializeObject(builds));
            var responseStatus = await appServiceConnection.SendMessageAsync(updateMessage);
            Debug.WriteLine(responseStatus.Status.ToString());
        }

        private async Task<List<Build>> GetBuilds()
        {
            var builds = new List<Build>();
#if RELEASE
            TimeSpan start = new TimeSpan(9, 0, 0);
            TimeSpan end = new TimeSpan(21, 0, 0);
            TimeSpan now = DateTime.Now.TimeOfDay;

            if ((now < start) || (now > end))
            {
                return builds;
            }
#endif
            var httpClient = new HttpClient();

            try
            {
                //we need project definitions in order to get builds
                var tcProjects = await GetProjectDefinitions(httpClient);

                var tcBuildTypesLookup = tcProjects.buildType.ToDictionary(b => b.id);

                 //and also running builds
                var runningTCBuilds = await GetRunningBuilds(httpClient);
                var tcRunningBuildsLookup = runningTCBuilds.build.ToDictionary(b => b.buildTypeId);

                //and builds themselves
                var tcBuilds = await Task.WhenAll(tcProjects.buildType.Select(p => GetBuild(p.id, httpClient)));

                //and now somehow we'll have to stich the three things together. Good luck with that.
                foreach (var tcBuild in tcBuilds)
                {
                    //no builds yet
                    if (tcBuild.build.Count == 0)
                    {
                        continue;
                    }

                    var b = new Build();
                  
                    //TC sorts completed builds by completion time. So we can just use that
                    b.Id = tcBuild.build[0].buildTypeId;
                    b.Name = tcBuildTypesLookup[b.Id].name;
                    b.ProjectName = tcBuildTypesLookup[b.Id].projectName;
                    b.TimeStamp = DateTime.Now;

                    if (tcRunningBuildsLookup.ContainsKey(b.Id))
                    {
                        b.Status = "RUNNING";
                        b.Number = tcRunningBuildsLookup[b.Id].number;
                    }
                    else
                    {
                        b.Status = tcBuild.build[0].status;
                        b.Number = tcBuild.build[0].number;
                    }

                    if (tcBuild.build.Count>1)
                    {
                        b.PreviousStatus = tcBuild.build[1].status;
                    }
                    else
                    {
                        b.PreviousStatus = b.Status;
                    }

                    builds.Add(b);
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
                Debug.Write(ex.HResult);
            }
            finally
            {
                httpClient.Dispose();
            }

            return builds;
        }

        private async Task<TeamCityProjectDefinitions> GetProjectDefinitions(HttpClient httpClient)
        {
            var requestMessage = GetHttpMessage("/app/rest/buildTypes?fields=buildType(id,name,projectName)");
            var responseMessage = await httpClient.SendAsync(requestMessage);
            var response = await responseMessage.Content.ReadAsStringAsync();

            var tcBduildTypes = JsonConvert.DeserializeObject<TeamCityProjectDefinitions>(response);

            return tcBduildTypes;
        }

        private async Task<TeamCityBuilds> GetBuild(string buildTypeId, HttpClient httpClient)
        {
            var requestMessage = GetHttpMessage(String.Format("/app/rest/builds?locator=buildType:(id:{0})&count=2&fields=build(id,buildTypeId,status,number,state)", buildTypeId));
            var responseMessage = await httpClient.SendAsync(requestMessage);
            var response = await responseMessage.Content.ReadAsStringAsync();

            var tcBuilds = JsonConvert.DeserializeObject<TeamCityBuilds>(response);

            return tcBuilds;
        }

        private async Task<TeamCityBuilds> GetRunningBuilds(HttpClient httpClient)
        {
            var requestMessage = GetHttpMessage("/app/rest/builds?locator=running:true&fields=build(id,buildTypeId,status,number,state)");
            var responseMessage = await httpClient.SendAsync(requestMessage);
            var response = await responseMessage.Content.ReadAsStringAsync();

            var tcBuilds = JsonConvert.DeserializeObject<TeamCityBuilds>(response);

            return tcBuilds;
        }

        private HttpRequestMessage GetHttpMessage (string requestPath)
        {
            var message = new HttpRequestMessage();
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuthString);
            message.Method = HttpMethod.Get;
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            message.RequestUri = new Uri(serverHost + requestPath);
            message.Headers.UserAgent.Add(new ProductInfoHeaderValue("Iniga", "0.1"));

            return message;
        }
    }
}
