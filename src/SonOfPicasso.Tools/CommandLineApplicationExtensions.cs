using System;
using FluentColorConsole;
using McMaster.Extensions.CommandLineUtils;

namespace SonOfPicasso.Tools
{
    public static class CommandLineApplicationExtensions
    {
        public static void HandleSpecifySubCommandError(this CommandLineApplication commandLineApplication)
        {
            commandLineApplication.OnExecute(() => 
            {
                Console.WriteLine("Specify a subcommand");
                commandLineApplication.ShowHelp();
                return 1;
            });
        }

        public static void HandleValidationError(this CommandLineApplication commandLineApplication)
        {
            commandLineApplication.OnValidationError(result =>
            {
                ColorConsole.WithRedText.WriteLine(result.ToString());
                Console.WriteLine();
                commandLineApplication.ShowHelp();
            });
        }
    }
}