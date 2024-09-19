using BepInEx;
using BuildSafe;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using UnityEngine;
using Utilla;
using Newtonsoft.Json;

namespace AutoCodeSender
{
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public string webhookUrl = "";
        public bool Enabled = true;

        public void Start()
        {
            Instance = this;
            Utilla.Events.GameInitialized += OnGameInitialized;
        }

        public void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
            Enabled = true;
        }

        public void OnDisable()
        {
            HarmonyPatches.RemoveHarmonyPatches();
            Enabled = false;
        }

        public void OnGameInitialized(object sender, EventArgs e)
        {
            string filePath = Path.Combine(Paths.ConfigPath, "discord_webhook.txt");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "https://discord.com/api/webhooks/your_webhook_url_here");
                Logger.LogInfo("Webhook URL file created. Please configure the webhook URL.");
            }

            webhookUrl = File.ReadAllText(filePath).Trim();
            Logger.LogInfo("Webhook URL loaded: " + webhookUrl);

            if (string.IsNullOrEmpty(webhookUrl))
            {
                Logger.LogWarning("Webhook URL is not set. Please configure the webhook URL in discord_webhook.txt.");
                return;
            }

            this.gameObject.AddComponent<NetworkCallBacks>();
        }

        public void OnLobbyJoin(string lobbyCode, int playerCount)
        {
            if(!Enabled)
            { return; }
            SendMessage(webhookUrl, $"{PhotonNetwork.NickName} has joined the lobby with code: {lobbyCode} that has {playerCount} {Players(playerCount)}");
        }

        public void OnLobbyLeft(string lobbyCode, int playerCount)
        {
            if (!Enabled)
            { return; }
            SendMessage(webhookUrl, $"{PhotonNetwork.NickName} has left the lobby {lobbyCode} that had {playerCount} {Players(playerCount)}");
        }

        public string Players(int playerCount)
        {
            return playerCount == 1 ? "Player" : "Players";
        }

        public static void SendMessage(string URL, string Message, Texture2D Photo = null, Texture2D Photo2 = null, Texture2D Photo3 = null, string EmbedImageUrl = null, string EmbedImageUrl2 = null, string EmbedImageUrl3 = null)
        {
            if (string.IsNullOrEmpty(URL))
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        // Prepare the message content for the embed
                        string messagetosend = Message;
                        string NiceEmbedMessage = "> " + messagetosend;

                        using (var form = new MultipartFormDataContent())
                        {
                            // Create the embed JSON structure
                            var embeds = new List<object>();

                            // Main embed with message
                            var mainEmbed = new
                            {
                                description = NiceEmbedMessage,
                                color = 0x3498db  // Blue color (optional)
                            };

                            embeds.Add(mainEmbed);

                            // Add image embeds if URLs are provided
                            if (!string.IsNullOrEmpty(EmbedImageUrl))
                            {
                                embeds.Add(new
                                {
                                    image = new { url = EmbedImageUrl }
                                });
                            }

                            if (!string.IsNullOrEmpty(EmbedImageUrl2))
                            {
                                embeds.Add(new
                                {
                                    image = new { url = EmbedImageUrl2 }
                                });
                            }

                            if (!string.IsNullOrEmpty(EmbedImageUrl3))
                            {
                                embeds.Add(new
                                {
                                    image = new { url = EmbedImageUrl3 }
                                });
                            }

                            // Create payload with content and embeds
                            var payload = new
                            {
                                content = "",  // No plain message content, only embed
                                embeds = embeds
                            };

                            // Serialize payload to JSON
                            string payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                            // Add the payload JSON to the form
                            form.Add(new StringContent(payloadJson, Encoding.UTF8, "application/json"), "payload_json");

                            // Attach Texture2D photos as files if they exist
                            if (Photo != null)
                            {
                                var imageData = Texture2DToByteArray(Photo);
                                var imageContent = new ByteArrayContent(imageData);
                                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                                form.Add(imageContent, "file1", "photo1.jpg");
                            }

                            if (Photo2 != null)
                            {
                                var imageData2 = Texture2DToByteArray(Photo2);
                                var imageContent2 = new ByteArrayContent(imageData2);
                                imageContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                                form.Add(imageContent2, "file2", "photo2.jpg");
                            }

                            if (Photo3 != null)
                            {
                                var imageData3 = Texture2DToByteArray(Photo3);
                                var imageContent3 = new ByteArrayContent(imageData3);
                                imageContent3.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                                form.Add(imageContent3, "file3", "photo3.jpg");
                            }

                            // Send the POST request
                            HttpResponseMessage response = client.PostAsync(URL, form).GetAwaiter().GetResult();

                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Failed to send message. Status code: {response.StatusCode}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            });
        }


        private static byte[] Texture2DToByteArray(Texture2D texture)
        {
            using (var stream = new MemoryStream())
            {
                var texture2D = texture as Texture2D;
                var bytes = texture2D.EncodeToJPG();
                stream.Write(bytes, 0, bytes.Length);
                return stream.ToArray();
            }
        }
    }
}
