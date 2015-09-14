using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class TeamCityWebHookBuildStatus
    {
        public string buildStatus { get; set; }
        public string buildResult { get; set; }
        public string buildResultPrevious { get; set; }
        public string notifyType { get; set; }
        public string buildFullName { get; set; }
        public string buildName { get; set; }
        public string buildId { get; set; }
        public string buildTypeId { get; set; }
        public string projectName { get; set; }
        public string projectId { get; set; }
        public string buildNumber { get; set; }
        public string message { get; set; }
    }
}
