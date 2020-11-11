using DiscordRPC;
using DiscordRPC.Logging;
using System;
using System.Windows;


namespace ME3_Discord_RPC
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			initialize();

		}

		public void initialize()
		{
			DiscordRpcClient client = new DiscordRpcClient("");

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
			while (true)
			{
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
		}
	}
}
