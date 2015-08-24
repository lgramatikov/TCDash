using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class BuildsData
    {
        public ObservableCollection<Build> Builds;

        public BuildsData() {
            Builds= new ObservableCollection<Build>();
        }
    }
}
