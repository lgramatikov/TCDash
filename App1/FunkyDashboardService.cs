
using org.laz.TCDashboardInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.AllJoyn;
using Windows.Foundation;

// see https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/AllJoyn/ProducerExperiences/cs/SecureInterfaceService.cs

namespace App1
{
    class FunkyDashboardService : ITCDashboardInterfaceService
    {
        public IAsyncOperation<TCDashboardInterfaceGoBoringResult> GoBoringAsync(AllJoynMessageInfo info)
        {
            System.Diagnostics.Debug.Write("Go boring called");

            IAsyncAction asyncAction = MainPage.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                MainPage.Current.GoBoring();
            });

            Task<TCDashboardInterfaceGoBoringResult> task = new Task<TCDashboardInterfaceGoBoringResult>(() =>
            {
                return TCDashboardInterfaceGoBoringResult.CreateSuccessResult(true);
            });

            task.Start();
            return task.AsAsyncOperation();
        }

        public IAsyncOperation<TCDashboardInterfaceGoFunkyResult> GoFunkyAsync(AllJoynMessageInfo info)
        {
            System.Diagnostics.Debug.Write("Go funky called");

            IAsyncAction asyncAction = MainPage.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                MainPage.Current.GoFunky();
            });

            Task<TCDashboardInterfaceGoFunkyResult> task = new Task<TCDashboardInterfaceGoFunkyResult>(() =>
            {
                return TCDashboardInterfaceGoFunkyResult.CreateSuccessResult(true);
            });

            task.Start();
            return task.AsAsyncOperation();
        }
    }
}
