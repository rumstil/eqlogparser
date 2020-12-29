using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EQLogParser;


namespace LogSync
{
    /// <summary>
    /// This class handles sending data to the discord.com webhook API.
    /// https://discord.com/developers/docs/resources/webhook
    /// </summary>
    public class Discord : IDisposable  
    {
        private HttpClient httpClient;
        private string uploadUrl = "https://discord.com/api";
        private Action<string> logger;

        public string WebhookUrl { get { return uploadUrl; } set { uploadUrl = value; } }


        public Discord(Action<string> logger)
        {
            httpClient = new HttpClient();
            //httpClient.DefaultRequestHeaders.Add("User-Agent", "DiscordBot (https://www.raidloot.com, v1)");
            this.logger = logger;
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public Task<bool> UploadFight(FightInfo f)
        {
            logger("Uploading " + f.Name);

            // https://discord.com/developers/docs/reference#message-formatting

            // build a summary of the fight
            //var notes = new StringBuilder();
            //notes.AppendFormat("{0} -- {1}", f.Name, f.HP);
            //foreach (var p in f.Participants.Take(10))
            //    notes.AppendFormat("\n{0} {1}", p.Name, p.OutboundHitSum);

            var notes = new StringBuilder();
            var writer = new StringWriter(notes);
            f.WriteNotes(writer);


            return SendMessage(notes.ToString());
        }

        public async Task<bool> SendMessage(string text)
        {
            var json = JsonSerializer.Serialize(new { content = text });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var result = await httpClient.PostAsync(uploadUrl, content);
                if (result.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    logger("Discord error: " + result.StatusCode + " " + await result.Content.ReadAsStringAsync());
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger("Discord error: " + ex.Message);
                return false;
            }
        }

        public static bool IsValidWebhookUrl(string url)
        {
            return Regex.IsMatch(url, @"^https://discord.com/api/webhooks/(\d+)/([a-z0-9_-]+)$");
        }
    }
}
