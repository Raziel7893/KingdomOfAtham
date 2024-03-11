using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using System.Text;

namespace WindowsGSM.Plugins
{
    public class KingdomOfAtham : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.KingdomOfAtham", // WindowsGSM.XXXX
            author = "raziel7893",
            description = "WindowsGSM plugin for supporting KingdomOfAtham Dedicated Server",
            version = "1.0.0",
            url = "https://github.com/Raziel7893/WindowsGSM.KingdomOfAtham", // Github repository link (Best practice) TODO
            color = "#00ff2a" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "1736750"; // Game server appId Steam

        // - Standard Constructor and properties
        public KingdomOfAtham(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        //public override string StartPath => "KingdomOfAthamServer.exe"; // Game server start path
        public override string StartPath => "KoA\\Binaries\\Win64\\KoAServer-Win64-Shipping.exe";
        public string FullName = "KingdomOfAtham Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation

        // - Game server default values
        public string Port = "7777"; // Default port

        public string Additional = "?ServerPassword=?ServerAdminPassword=? -batchmode -nographics -log -nosteam"; // Additional server start parameter

        // TODO: Following options are not supported yet, as ther is no documentation of available options
        public string Maxplayers = "16"; // Default maxplayers        
        public string QueryPort = "27015"; // Default query port. This is the port specified in the Server Manager in the client UI to establish a server connection.
        // TODO: Unsupported option
        public string Defaultmap = "Dedicated"; // Default map name
        // TODO: Undisclosed method
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Create a default cfg or changes the config for the game server after installation
        public async void CreateServerCFG()
        {
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"?SessionName={serverData.ServerName}?Port={serverData.ServerPort}?QueryPort={serverData.ServerQueryPort}?MaxPlayers={serverData.ServerMaxPlayer}");
            
            if(serverData.ServerParam.StartsWith("-")) 
                sb.Append($" {serverData.ServerParam}");
            else
                sb.Append($"{serverData.ServerParam}");

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,  //wird komplett ignoriert?!?
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = sb.ToString(),
                    WindowStyle = ProcessWindowStyle.Hidden,  //wird komplett ignoriert?!?
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
            }

            // Start Process
            try
            {
                p.Start();
                if (AllowsEmbedConsole)
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                }
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }

        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("^c");
                p.WaitForExit(10000);
                if (!p.HasExited)
                    p.Kill();
            });
        }
    }
}
