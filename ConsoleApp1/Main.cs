using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace FTP
{
    class Program
    {

        // User prompt
        public const string PROMPT = "FTP> ";

        // Information to parse commands
        public static readonly string[] COMMANDS = { "ascii",
                          "binary",
                          "cd",
                          "cdup",
                          "debug",
                          "dir",
                          "get",
                          "help",
                          "passive",
                          "put",
                          "pwd",
                          "quit",
                          "user" };

        public const int ASCII = 0;
        public const int BINARY = 1;
        public const int CD = 2;
        public const int CDUP = 3;
        public const int DEBUG = 4;
        public const int DIR = 5;
        public const int GET = 6;
        public const int HELP = 7;
        public const int PASSIVE = 8;
        public const int PUT = 9;
        public const int PWD = 10;
        public const int QUIT = 11;
        public const int USER = 12;

        // Help message
        public static readonly String[] HELP_MESSAGE = {
            "ascii      --> Set ASCII transfer type",
            "binary     --> Set binary transfer type",
            "cd <path>  --> Change the remote working directory",
            "cdup       --> Change the remote working directory to the",
            "               parent directory (i.e., cd ..)",
            "debug      --> Toggle debug mode",
            "dir        --> List the contents of the remote directory",
            "get path   --> Get a remote file",
            "help       --> Displays this text",
            "passive    --> Toggle passive/active mode",
            "put path   --> Transfer the specified file to the server",
            "pwd        --> Print the working directory on the server",
            "quit       --> Close the connection to the server and terminate",
            "user login --> Specify the user name (will prompt for password" };


        static void Main(string[] args)
        {
            bool eof = false;
            String input = null;

            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: FTP serverHostname");
                Environment.Exit(1);
            }
            else if (args.Length == 2)
            {

            }
            Client client = new Client(args[0]);

            client.Login();

            do
            {
                try
                {
                    Console.Write(PROMPT);
                    input = Console.ReadLine();
                }
                catch (Exception e)
                {
                    eof = true;
                }

                // Keep going if we have not hit end of file
                if (!eof && input.Length > 0)
                {
                    int cmd = -1;
                    string[] argv = Regex.Split(input, "\\s+");

                    // What command was entered?
                    for (int i = 0; i < COMMANDS.Length && cmd == -1; i++)
                    {
                        if (COMMANDS[i].Equals(argv[0], StringComparison.CurrentCultureIgnoreCase))
                        {
                            cmd = i;
                        }
                    }

                    // Execute the command
                    switch (cmd)
                    {
                        case ASCII:
                            client.Type('A');
                            break;

                        case BINARY:
                            client.Type('I');
                            break;

                        case CD:
                            client.Cd(argv[1]);
                            break;

                        case CDUP:
                            client.Cdup();
                            break;

                        case DEBUG:
                            client.Debug();
                            break;

                        case DIR:
                            client.Dir();
                            break;

                        case GET:
                            break;

                        case HELP:
                            for (int i = 0; i < HELP_MESSAGE.Length; i++)
                            {
                                Console.WriteLine(HELP_MESSAGE[i]);
                            }
                            break;

                        case PASSIVE:
                            Console.WriteLine(client.Passive());
                            break;

                        case PUT:
                            break;

                        case PWD:
                            client.Pwd();
                            break;

                        case QUIT:
                            eof = true;
                            break;

                        case USER:
                            break;

                        default:
                            Console.WriteLine("Invalid command");
                            break;
                    }
                }
            } while (!eof);
        }
    }

    
}
