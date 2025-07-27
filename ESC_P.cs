using System;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using VRage.Plugins;
using VRage.FileSystem;
using Sandbox.ModAPI;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using VRage;
using VRage.GameServices;
using VRage.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game;
using Sandbox;

namespace EasyServerConnecting
{
    public class Main : IPlugin
    {
        private List<ServerConfig> servers = new List<ServerConfig>
        {
            new ServerConfig { Name = "Server 1", IpAddress = "000.0.0.0:00228" },
            new ServerConfig { Name = "Server 2", IpAddress = "" },
            new ServerConfig { Name = "Server 3", IpAddress = "" },
            new ServerConfig { Name = "Server 4", IpAddress = "" },
            new ServerConfig { Name = "Server 5", IpAddress = "" },
            new ServerConfig { Name = "Server 6", IpAddress = "" },
            new ServerConfig { Name = "Server 7", IpAddress = "" },
            new ServerConfig { Name = "Server 8", IpAddress = "" },
            new ServerConfig { Name = "Server 9", IpAddress = "" },
            new ServerConfig { Name = "Server 10", IpAddress = "" }
        };
        private const string ConfigFileName = "ServerIP.txt";
        private string configFilePath;
        private MainForm mainForm;

        public void Init(object gameInstance)
        {
            configFilePath = Path.Combine(MyFileSystem.UserDataPath, ConfigFileName);
            LoadServerConfigs();
            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                mainForm = new MainForm(this);
                Application.Run(mainForm);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            if (mainForm != null)
            {
                mainForm.Invoke(new Action(() => mainForm.Close()));
            }
        }

        private void LoadServerConfigs()
        {
            if (File.Exists(configFilePath))
            {
                string[] lines = File.ReadAllLines(configFilePath);
                for (int i = 0; i < lines.Length && i < servers.Count; i++)
                {
                    string[] parts = lines[i].Split('|');
                    if (parts.Length == 2)
                    {
                        servers[i].Name = parts[0].Trim();
                        servers[i].IpAddress = parts[1].Trim();
                    }
                }
            }
        }

        public void SaveServerConfigs()
        {
            List<string> lines = new List<string>();
            foreach (var server in servers)
            {
                lines.Add($"{server.Name}|{server.IpAddress}");
            }
            File.WriteAllLines(configFilePath, lines);
        }

        public List<ServerConfig> GetServers()
        {
            return servers;
        }

        public void ConnectToServer(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || !ipAddress.Contains(":"))
                return;

            string[] parts = ipAddress.Split(':');
            if (parts.Length != 2 || !ushort.TryParse(parts[1], out ushort port))
                return;

            var progressScreen = new MyGuiScreenProgress(MyTexts.Get(MyCommonTexts.DialogTextJoiningWorld), new MyStringId?(MyCommonTexts.Cancel), false, true);
            MyGuiSandbox.AddScreen(progressScreen);

            MyGameService.OnPingServerResponded += (sender, serverItem) =>
            {
                progressScreen.CloseScreen(false);
                MyLocalCache.SaveLastSessionInfo(null, true, false, serverItem.Name, serverItem.ConnectionString);
                MyJoinGameHelper.JoinGame(serverItem, true, null);
            };

            MyGameService.OnPingServerFailedToRespond += (sender, e) =>
            {
                progressScreen.CloseScreen(false);
            };

            MyGameService.PingServer(ipAddress);
        }
    }

    public class ServerConfig
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
    }

    public class MainForm : Form
    {
        private readonly Main plugin;
        private readonly Button[] connectButtons = new Button[10];
        private Button settingsButton;

        public MainForm(Main plugin)
        {
            this.plugin = plugin;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Easy Server Connect";
            this.Size = new System.Drawing.Size(850, 1400);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;

            Panel scrollPanel = new Panel
            {
                Size = new System.Drawing.Size(760, 860),
                Location = new System.Drawing.Point(20, 20),
                AutoScroll = true
            };

            for (int i = 0; i < 10; i++)
            {
                int index = i;
                connectButtons[i] = new Button
                {
                    Text = plugin.GetServers()[i].Name,
                    Location = new System.Drawing.Point(20, 80 + i * 100),
                    Size = new System.Drawing.Size(300, 80)
                };
                connectButtons[i].Click += (s, e) => plugin.ConnectToServer(plugin.GetServers()[index].IpAddress);
                scrollPanel.Controls.Add(connectButtons[i]);
            }

            settingsButton = new Button
            {
                Text = "Settings",
                Location = new System.Drawing.Point(360, 80),
                Size = new System.Drawing.Size(300, 80)
            };
            settingsButton.Click += (s, e) => OpenSettingsDialog();
            scrollPanel.Controls.Add(settingsButton);

            this.Controls.Add(scrollPanel);
        }

        private void OpenSettingsDialog()
        {
            Form settingsForm = new Form
            {
                Text = "Server Settings",
                Size = new System.Drawing.Size(850, 1400),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                TopMost = true
            };

            Panel scrollPanel = new Panel
            {
                Size = new System.Drawing.Size(760, 860),
                Location = new System.Drawing.Point(20, 20),
                AutoScroll = true
            };

            TextBox[] nameTextBoxes = new TextBox[10];
            TextBox[] ipTextBoxes = new TextBox[10];
            var servers = plugin.GetServers();

            for (int i = 0; i < 10; i++)
            {
                Label nameLabel = new Label
                {
                    Text = $"Server {i + 1} Name:",
                    Location = new System.Drawing.Point(20, 40 + i * 160),
                    Size = new System.Drawing.Size(200, 40)
                };
                nameTextBoxes[i] = new TextBox
                {
                    Text = servers[i].Name,
                    Location = new System.Drawing.Point(220, 40 + i * 160),
                    Size = new System.Drawing.Size(400, 60)
                };

                Label ipLabel = new Label
                {
                    Text = $"IP:Port:",
                    Location = new System.Drawing.Point(20, 100 + i * 160),
                    Size = new System.Drawing.Size(200, 40)
                };
                ipTextBoxes[i] = new TextBox
                {
                    Text = servers[i].IpAddress,
                    Location = new System.Drawing.Point(220, 100 + i * 160),
                    Size = new System.Drawing.Size(400, 60)
                };

                scrollPanel.Controls.Add(nameLabel);
                scrollPanel.Controls.Add(nameTextBoxes[i]);
                scrollPanel.Controls.Add(ipLabel);
                scrollPanel.Controls.Add(ipTextBoxes[i]);
            }

            Button saveButton = new Button
            {
                Text = "Save",
                Location = new System.Drawing.Point(20, 40 + 10 * 160),
                Size = new System.Drawing.Size(200, 80)
            };
            saveButton.Click += (s, e) =>
            {
                for (int i = 0; i < 10; i++)
                {
                    servers[i].Name = nameTextBoxes[i].Text;
                    servers[i].IpAddress = ipTextBoxes[i].Text;
                    connectButtons[i].Text = servers[i].Name;
                }
                plugin.SaveServerConfigs();
                settingsForm.Close();
            };

            Button cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(380, 40 + 10 * 160),
                Size = new System.Drawing.Size(200, 80)
            };
            cancelButton.Click += (s, e) => settingsForm.Close();

            scrollPanel.Controls.Add(saveButton);
            scrollPanel.Controls.Add(cancelButton);

            settingsForm.Controls.Add(scrollPanel);
            settingsForm.ShowDialog(this);
        }
    }
}