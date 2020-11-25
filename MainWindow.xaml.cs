using DiscordRPC;
using DiscordRPC.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using static WPF_Client.ME3MemmoryReader;


namespace ME3_Discord_RPC
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public DiscordRpcClient client;
		public static int packetCount = 0;

		int map = 999;
		int enemy = 999;
		int diff = 999;

		int lastWave = 0;
		int lastSec = 0;
		bool packetWasSent = false;

		System.Windows.Threading.DispatcherTimer gameReader;

		public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            while ((readGameData()) != null)
            {
                initialize();
                update(readGameData()["map"]);
            }  
		}

        public Dictionary<string, string> readGameData()
        {
            Process process = Process.GetProcessesByName("masseffect3").FirstOrDefault();
            if (process == null)
            {
                return null;
            }

            IntPtr processHandle = OpenProcess(PROCESS_VM_READ, false, process.Id);
            int lobbysetup = ReadMemInt32(processHandle, LOC_LOBBYSETUP);
            if (lobbysetup == 0)
            {
             
                CloseHandle(processHandle);
                return null;
            }

            bool isPlayingMatch = false;

            int sector1 = GetFinalLocationFromOffset(processHandle, LOC_MPINFO, OFF_SECTOR1_GAMEREPLICATIONINFO);
            if (sector1 == -1)
            {
                CloseHandle(processHandle);
                return null;
            }

            int sector2 = GetFinalLocationFromOffset(processHandle, LOC_MPINFO, OFF_SECTOR2_WAVECOORDINATOR);
            if (sector2 == -1)
            {
                
            }

            int info = sector1 + 0x2F8;

            map = ReadMemInt32(processHandle, info);
            enemy = ReadMemInt32(processHandle, info + 4);
            diff = ReadMemInt32(processHandle, info + 8);

            if (map == -1)
            {
                map = ReadMemInt32(processHandle, info + 16);
                enemy = ReadMemInt32(processHandle, info + 20);
                diff = ReadMemInt32(processHandle, info + 24);
                isPlayingMatch = (sector2 != -1);
            }

            int seconds = ReadMemInt32(processHandle, sector1 + 0x240);

            if (!isPlayingMatch)
            {
                CloseHandle(processHandle);
                return null;
            }

            int wavenum = -1;
            wavenum = ReadMemInt32(processHandle, sector2 + 0x218);
            if (lastWave != wavenum + 1)
            {
                lastWave = wavenum + 1;
            }

            // Check if match has ended
            int endofmatchobj = GetEndOfMatchObjectAddress(processHandle);
            if (endofmatchobj == -1)
            {
                packetWasSent = false;
                CloseHandle(processHandle);
                return null;
            }

            // match ended
            PlayerInfo player = GetLocalPlayerInfo(processHandle);
            MatchResults results = GetMatchResultsData(processHandle, endofmatchobj);
            Dictionary<string, string> packetData = new Dictionary<string, string>();

            packetData.Add("enemy", results.EnemyID.ToString());
            packetData.Add("map", results.MapId.ToString());
            packetData.Add("diff", results.DifficultyID.ToString());
            packetData.Add("wave", results.Waves.ToString());
            packetData.Add("success", results.Success.ToString());
            packetData.Add("class", player.ClassValue.ToString());
            packetData.Add("chara", player.CharValue.ToString());
            packetData.Add("weapon1", player.Weapon1Value.ToString());
            packetData.Add("weapon2", player.Weapon2Value.ToString());
            packetData.Add("playerscore", player.Score.ToString());
            packetData.Add("teamscore", results.TeamScore.ToString());
            packetData.Add("teamsize", results.TeamSize.ToString());
            packetData.Add("cobramissilesused", player.ConsumablesUsedCounts[0].ToString());
            packetData.Add("medigelsused", player.ConsumablesUsedCounts[1].ToString());
            packetData.Add("opssurvivalpacksused", player.ConsumablesUsedCounts[2].ToString());
            packetData.Add("thermalclippacksused", player.ConsumablesUsedCounts[3].ToString());
            packetData.Add("playermedals*", player.Medals);



            packetData.Add("teammedals*", results.TeamMedals);





            lastSec = seconds;
            CloseHandle(processHandle);
            return packetData;
        }

		public void initialize()
		{
			client = new DiscordRpcClient("");

			client.Logger = new ConsoleLogger() { Level = LogLevel.Trace };

			client.OnReady += (sender, e) =>
			{
				Console.WriteLine("Received Ready from user {0}", e.User.Username);
			};

			client.OnPresenceUpdate += (sender, e) =>
			{
				Console.WriteLine("Received Update! {0}", e.Presence);
			};

			client.Initialize();

				client.SetPresence(new RichPresence()
				{
					Details = "Example Project",
					State = "csharp example",
					Assets = new Assets()
					{
						LargeImageKey = "image_large",
						LargeImageText = "Lachee's Discord IPC Library",
						SmallImageKey = "image_small"
					}
				});
		}
	
        public void update(String Map)
        {
            client.SetPresence(new RichPresence()
            {
                Details = "Example Project",
                State = "csharp example",
                Assets = new Assets()
                {
                    LargeImageKey = "image_large",
                    LargeImageText = "Playing on map" + Map,
                    SmallImageKey = "image_small"
                }
            });
        }
    }
}
