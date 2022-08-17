using System.Configuration;
using System.Diagnostics;

namespace BG_ServerManager
{
    internal class Program
    {
        #region Misc
        /// <summary>
        /// Program variables that require storage.
        /// </summary>
        class Variables
        {
            public static int Crashes = 0;
            public static string PathToServer = "";
            public static string ServerParams = "";

            public static bool ServerRunning = false;
            public static bool serverStopping = false;
            public static Process ServerProcess = new Process();
            public static System.Timers.Timer serverTimer = new System.Timers.Timer();
        }

        #region Logger
        /// <summary>
        /// Program logging function that displays a entry within the console window.
        /// </summary>
        /// <param name="txt">The text that you would like to display in this entry. (should only be left blank if "spacer" is true)</param>
        /// <param name="stamped">Should this entry be time-stamped?</param>
        /// <param name="txtColour">The foreground colour for this entrys text.</param>
        /// <param name="spacer">Is this entry a spacer?</param>
        /// <param name="newLine">Should this entry end in a new-line?</param>
        /// <returns></returns>
        static async Task Log(string txt = "", bool stamped = true, ConsoleColor txtColour = ConsoleColor.White, bool spacer = false, bool newLine = true)
        {
            if (stamped && !spacer)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("[" + DateTime.Now + "(" + DateTime.Now.Millisecond + "ms)] Server Manager: ");
            }

            if (spacer)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("-----------------------------");
            }
            else
            {
                Console.ForegroundColor = txtColour;
                if (newLine) Console.WriteLine(txt);
                else Console.Write(txt);
            }
        }
        #endregion

        #region Config
        /// <summary>
        /// Stores a string variable to the config file.
        /// </summary>
        /// <param name="name">The name/title for this variable</param>
        /// <param name="data">The contents of the string you would like to recover</param>
        public static void storeVariable(string name, string data)
        {
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = Environment.ExpandEnvironmentVariables("%AppData%") + @"\BG_ServerManager\\user.config"; ;
            Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = configuration.AppSettings.Settings;
            if (settings[name] == null) settings.Add(name, data);
            else settings[name].Value = data;
            configuration.Save(ConfigurationSaveMode.Modified);        }

        /// <summary>
        /// Returns a string variable stored in the config file.
        /// </summary>
        /// <param name="variable">The name/title associated with this variable.</param>
        /// <returns></returns>
        public static string getVariable(string variable)
        {
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = Environment.ExpandEnvironmentVariables("%AppData%") + @"\BG_ServerManager\\user.config"; ;
            Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = configuration.AppSettings.Settings;

            if (settings[variable] == null) return "";
            else return settings[variable].Value;
        }
        #endregion

        #region Checks
        /// <summary>
        /// Checks for a server executable.
        /// </summary>
        /// <param name="overwrite">Should this function skip checking the config file and save a new executable.</param>
        /// <returns>The location to the server executable.</returns>
        static async Task<string> CheckServerEXE(bool overwrite = false)
        {
            //If server EXE is saved in config.
            if (getVariable("serverEXE") != "" && !overwrite)
            {
                //Found exe in config
                return getVariable("serverEXE");
            }
            else
            {
                //No exe in config
                await Log("Please enter the location to the dedicated server executable (.exe):");
                string pathToExe = Console.ReadLine();
                if (File.Exists(pathToExe))
                {
                    storeVariable("serverEXE", pathToExe);
                    return pathToExe;
                }
                else
                {
                    await Log("Uh-oh, it looks like that file does not exist, please try again...");
                    CheckServerEXE();
                    return "";
                }
            }
        }

        /// <summary>
        /// Checks for stored server paramaters.
        /// </summary>
        /// <param name="overwrite">Should this function skip checking the config file and save new server paramaters.</param>
        /// <returns>The server paramaters.</returns>
        static async Task<string> CheckServerParams(bool overwrite = false)
        {
            //If server parrams are saved in config.
            if (getVariable("serverParams") != "" && !overwrite)
            {
                //Found exe in config
                return getVariable("serverParams");
            }
            else
            {
                //No exe in config
                await Log("Please enter the paramaters you would like to run the server with:");
                string serverParams = Console.ReadLine();
                storeVariable("serverParams", serverParams);
                return serverParams;
            }
        }
        #endregion
        #endregion

        #region Main
        /// <summary>
        /// Entry method for the program.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.Title = "BG Server Manager";
            Start();
        }

        /// <summary>
        /// Async entry method for program.
        /// </summary>
        static async void Start()
        {
            await Log("Welcome to the BG Server Manager!", false, ConsoleColor.Yellow);
            await Log(spacer: true);

            await Log("Starting Server Manager...");
            await Log("Checking for server exe...");
            Variables.PathToServer = await CheckServerEXE();
            await Log("Local path to server: " + Variables.PathToServer, txtColour: ConsoleColor.Green);
            await Log("Checking for server paramaters...");
            Variables.ServerParams = await CheckServerParams();
            await Log("Server Params are: " + Variables.ServerParams, txtColour: ConsoleColor.Green);

            await Log("All checks complete, starting manager...");
            await Log(spacer: true);
            await CMD();
        }
        #endregion

        #region Server Management
        /// <summary>
        /// Starts the dedicated server.
        /// </summary>
        public static async Task StartServer()
        {
            await Log("Starting server...");

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Variables.PathToServer;
            startInfo.Arguments = "-console -game ship -port 27016 -steam " + Variables.ServerParams;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Maximized;
            startInfo.CreateNoWindow = false;

            Variables.ServerProcess.StartInfo = startInfo;
            Variables.ServerProcess.Start();
            string hoursUntilRestart = getVariable("hoursUntilRestart");
            if (hoursUntilRestart != "")
            {
                Variables.serverTimer.Interval = int.Parse(hoursUntilRestart) * 60 * 60 * 1000;
                Variables.serverTimer.Enabled = true;
                Variables.serverTimer.Elapsed += new System.Timers.ElapsedEventHandler(ServerTimerElapsed);
            }
            Variables.ServerRunning = true;
            await Log("Server started.");
            Console.WriteLine();

            await Task.Delay(2000).WaitAsync(TimeSpan.FromSeconds(5));

            await Variables.ServerProcess.WaitForExitAsync();

            if (!Variables.serverStopping && Variables.ServerProcess.HasExited)
            {
                Console.WriteLine();
                await Log("Server crashed! Restarting...");
                Variables.Crashes += 1;
                await StopServer();
                StartServer();
            }
        }
        /// <summary>
        /// Auto-restart timer elapsed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static async void ServerTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Variables.serverStopping = true;
            Console.WriteLine();
            await Log("Auto-restarting...");
            await StopServer();
            StartServer();
        }

        /// <summary>
        /// Stop the dedicated server
        /// </summary>
        public static async Task StopServer()
        {
            Variables.serverStopping = true;
            await Log("Stopping server...");
            if (getVariable("hoursUntilRestart") != "") Variables.serverTimer.Stop();
            Variables.ServerProcess.Kill();
            Variables.ServerProcess = new Process();
            Variables.ServerRunning = false;
            await Log("Server stopped.");
            Variables.serverStopping = false;
        }

        #endregion

        #region CMD
        /// <summary>
        /// The command line interface for this program.
        /// </summary>
        public static async Task CMD()
        {
            await Log("You can enter commands in here to interact with the manager or use the help command", false, ConsoleColor.Yellow);
            await Log(spacer: true);

            await Log("Command: ", newLine: false);
            string[] command = Console.ReadLine().ToLower().Split(" ");

            #region Help Command | Displays a list of all avaliable commands

            if (command[0] == "help")
            {
                if (command.Length == 1)
                {
                    await Log("You can enter commands in here seperated by a space.");
                    await Log("Key:");
                    await Log("\"<>\" - Represents an argument given to a command.");
                    await Log("\"()\" - Represents an optional argument.");
                    await Log(spacer: true);
                    await Log("\"help (<command>)\" - Provides more info on the command you can use within this Server Manager.");
                    await Log("\"start\" - Starts the dedicated server.");
                    await Log("\"stop\" - Stops the dedicated server.");
                    await Log("\"restart <time-hours>\" - Restarts the dedicated server.");
                    await Log("\"clear\" - Clears the console window.");
                    await Log("\"edit <serverpath/serverparams>\" - Allows you to edit the saved server inforamtion.");
                    await Log("\"status\" - Displays information about the current server");
                }
                else
                {
                    switch (command[1])
                    {
                        case "start":
                            await Log("Starts the dedicated server.");
                            await Log(spacer: true);
                            break;
                        case "stop":
                            await Log("Stops the dedicated server.");
                            await Log(spacer: true);
                            break;
                        case "restart":
                            await Log("Restarts the dedicated server.");
                            await Log("<timer-hours> - If set will automatically restart the server in the hours specified (Set 0 to disable).");
                            await Log(spacer: true);
                            break;
                        case "clear":
                            await Log("Clears the server manager's console window.");
                            await Log(spacer: true);
                            break;
                        case "edit":
                            await Log("Edit the configuration of the server.");
                            await Log("<serverpath> - Edit the saved server executable location.");
                            await Log("<serverparms> - Edit the saved server paramaters.");
                            await Log(spacer: true);
                            break;
                        case "status":
                            await Log("Displays the status of the current server.");
                            await Log(spacer: true);
                            break;
                        default:
                            break;
                    }
                }
            }
            #endregion

            #region Clear Command | Clears the console window
            else if (command[0] == "clear")
            {
                Console.Clear();
            }
            #endregion

            #region Start Command | Starts the dedicated server
            else if (command[0] == "start")
            {
                StartServer();
            }
            #endregion

            #region Stop Command | Stops the dedicated server
            else if (command[0] == "stop")
            {
                await StopServer();
            }
            #endregion

            #region Restart Command | Restarts the dedicated server
            else if (command[0] == "restart")
            {
                int hoursUntilRestart = 0;
                if (command.Length == 1)
                {
                    if (Variables.ServerRunning)
                    {
                        await Log("Restarting server...");
                        await StopServer();
                        StartServer();
                        await Log("Server restarted!");
                    }
                    else await Log("Please start a server first.", txtColour: ConsoleColor.Red);
                }
                else if (int.TryParse(command[1], out hoursUntilRestart))
                {
                    if (hoursUntilRestart == 0)
                    {
                        await Log("Automatic restarts are now disabled.");
                        storeVariable("hoursUntilRestart", "");
                    }
                    else
                    {
                        await Log("Automatic restarts are set to: " + hoursUntilRestart + "hours", txtColour: ConsoleColor.Green);


                        storeVariable("hoursUntilRestart", hoursUntilRestart.ToString());
                    }

                    if (Variables.ServerRunning)
                    {
                        await Log("The server must be restarted to apply these changes! Press any key to restart the server.");
                        Console.ReadKey();

                        await StopServer();
                        StartServer();
                    }
                }
            }
            #endregion

            #region Edit Command | Edits the config stored in the config file
            else if (command[0] == "edit")
            {
                if (command.Length == 1) await Log("Please specify which setting you would like to edit!", txtColour: ConsoleColor.Red);
                else if (command[1] == "serverpath")
                {
                    await CheckServerEXE(true);
                    await Log("Server path updated!", txtColour: ConsoleColor.Green);

                }
                else if (command[1] == "serverparams")
                {
                    await CheckServerParams(true);
                    await Log("Server params updated!", txtColour: ConsoleColor.Green);
                }
                else await Log("Sorry, \"" + command[1] + "\" is not recognized, please try again.", txtColour: ConsoleColor.Red);
            }
            #endregion

            #region Status Command | Displays the status of the dedicated server
            else if (command[0] == "status")
            {
                if (!Variables.ServerRunning)
                {
                    await Log("SERVER OFFLINE", txtColour: ConsoleColor.Red);
                }
                else
                {
                    await Log("SERVER ONLINE", txtColour: ConsoleColor.Green);
                    TimeSpan upTime = Variables.ServerProcess.StartTime - DateTime.Now;
                    await Log("Up-time: " + upTime + " | Crashes:" + Variables.Crashes);

                    string hoursUntilRestart = getVariable("hoursUntilRestart");
                    if (hoursUntilRestart != "")
                    {
                        TimeSpan timeUntilRestart = DateTime.Now - Variables.ServerProcess.StartTime - TimeSpan.FromMilliseconds(Variables.serverTimer.Interval);

                        await Log("Auto-restart set to: " + hoursUntilRestart + "hours");
                        await Log("Resting in: " + timeUntilRestart);
                    }
                }
            }
            #endregion

            #region Non-recognized Command | Handles all unrecognied inputs
            else
            {
                if (command[0] == "") await Log("Please enter a command, use \"help\" to see a list of commands.", txtColour: ConsoleColor.Red);
                else await Log("Sorry, \"" + command[0] + "\" is not recognized, please try again (or try using \"help\")", txtColour: ConsoleColor.Red);
            }
            #endregion

            CMD();
        }
        #endregion
    }
}