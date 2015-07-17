using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Hosting;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace xClient.AddIn.ExceptionHelpers
{
    /// <summary>
    /// Provides a set of actions to manage unhandled Exceptions in AddIns and set of actions that
    /// manage unstable AddIns.
    /// </summary>
    public static class UnhandledExceptionHelper
    {
        private const string unstableTokenFile = "unstableTokens.tokens";

        /// <summary>
        /// Wires the calling AddIn to a Remote Exception Object so unhandled Exceptions will be handled.
        /// </summary>
        /// <param name="addIn">The AddIn to handle unhandled Exceptions from.</param>
        public static void HandleUnhandledExceptions(this AddInToken addIn)
        {
            try
            {
                AddInController controller = AddInController.GetAddInController(addIn);
                AddInToken token = controller.Token;
                AppDomain domain = controller.AppDomain;
                RemoteExceptionObject helper =
                    (RemoteExceptionObject)domain.CreateInstanceFromAndUnwrap(typeof(RemoteExceptionObject).Assembly.Location, typeof(RemoteExceptionObject).FullName);

                helper.Initialize(token);

            }
            catch
            { }
        }

        /// <summary>
        /// Retrieves a List of unstable tokens.
        /// </summary>
        /// <returns></returns>
        public static List<AddInToken> GetUnstableTokens()
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();

                if (File.Exists(unstableTokenFile))
                {
                    using (FileStream fs = File.OpenRead(unstableTokenFile))
                    {
                        return (List<AddInToken>)formatter.Deserialize(fs);
                    }
                }
                else
                {
                    // No unstable tokens...
                    return new List<AddInToken>();
                }
            }
            catch
            {
                // Unable to get the List of unstable tokens. Return an empty List.
                return new List<AddInToken>();
            }
        }

        /// <summary>
        /// Marks the calling token as unstable.
        /// </summary>
        /// <param name="token">The token to mark as unstable.</param>
        public static void MarkTokenUnstable(this AddInToken token)
        {
            List<AddInToken> unstableTokens = GetUnstableTokens();
            unstableTokens.Add(token);

            // TO-DO: Send a message to the server that states that the token is unstable.

            unstableTokens.WriteUnstableTokens();
        }
        
        /// <summary>
        /// Writes all of the tokens in the calling List of tokens to a file that holds each unstable token.
        /// </summary>
        /// <param name="tokens">The List of unstable tokens to write to the file that holds each unstable token.</param>
        public static void WriteUnstableTokens(this List<AddInToken> tokens)
        {
            try
            {
                BinaryFormatter f = new BinaryFormatter();

                using (FileStream fs = File.OpenWrite(unstableTokenFile))
                {
                    f.Serialize(File.OpenWrite(unstableTokenFile), tokens);
                }
            }
            catch
            { }
        }
    }
}