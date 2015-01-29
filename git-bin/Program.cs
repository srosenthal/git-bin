using System;
using System.Linq;
using Objector;

namespace GitBin
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                // Build the list of available commands and execute the one requested in the user-provided args.
                var builder = new Builder();
                ApplicationRegistrations.Register(builder);
                var container = builder.Create();

                var commandFactory = container.Resolve<ICommandFactory>();

                var command = commandFactory.GetCommand(args);
                command.Execute();
            }
            catch (ಠ_ಠ lod)
            {
                GitBinConsole.WriteLine(lod.Message);
                return 1;
            }
            catch (Exception e)
            {
                GitBinConsole.WriteLine("Uncaught exception, please report this bug! " + e);
                return 2;
            }

            return 0;
        }
    }
}
