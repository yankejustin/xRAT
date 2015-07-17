using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Hosting;

namespace xClient.AddIn.ExceptionHelpers
{
    /// <summary>
    /// Provides an object to use that will help retrieve unhandled Exceptions across other Application Domains.
    /// </summary>
    public class RemoteExceptionObject : MarshalByRefObject
    {
        private AddInToken _token;

        public void Initialize(AddInToken token)
        {
            _token = token;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // This token gave us problems, so mark it as unstable.
            _token.MarkTokenUnstable();
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}