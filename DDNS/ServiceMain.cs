using System.ServiceProcess;
using System.Threading;

namespace DDNS
{
    public partial class ServiceMain : ServiceBase
    {
        DDNSHub hub;
        public ServiceMain()
        {
            InitializeComponent();
#if DEBUG
            Thread.Sleep(10000);
#endif
            hub = new DDNSHub();
        }
        
        protected override void OnStart(string[] args)
        {
#if DEBUG
            Thread.Sleep(10000);
#endif
            hub.Run();
        }

        protected override void OnStop()
        {
            hub.Exit();
        }
    }
}
