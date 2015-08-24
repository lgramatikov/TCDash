using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class TeamCityBuild
    {
        public int id { get; set; }
        public string state { get; set; }
        public string status { get; set; }
        public string number { get; set; }
        public string buildTypeId { get; set; }
    }
}
