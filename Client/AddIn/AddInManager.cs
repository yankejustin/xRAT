using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Hosting;

namespace xClient.AddIn
{
    public class AddInManager
    {
        // TO-DO: Allow easy usage of QualificationData for each AddIn:
        //        Perhaps send a List<string> to the server containing
        //        the qualification data for each of the AddIns.

        /*
        
        On Activation of the plugin, use qualification data to control how an add-in should be activated. 

        if (selectedToken.QualificationData[AddInSegmentType.AddIn]["Isolation"].Equals("NewProcess"))
        {
            // Create an external process.
            AddInProcess external = new AddInProcess();

            // Activate an add-in in the new process 
            // with the full trust security level.
            Calculator CalcAddIn5 =
                selectedToken.Activate<Calculator>(external,
                AddInSecurityLevel.FullTrust);
            Console.WriteLine("Add-in activated per qualification data.");
        }
        else
            Console.WriteLine("This add-in is not designated to be activated in a new process.");
        
         */

        public List<AddInToken> AddIns { get; private set; }

        public AddInManager()
        {
            AddIns = new List<AddInToken>();
        }

        public AddInManager(params AddInToken[] tokens)
        {
            AddIns = new List<AddInToken>(tokens);
        }
    }
}