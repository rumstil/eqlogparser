using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EQLogParser;


namespace LogSync
{
    /// <summary>
    /// This class handles sending data to the raidloot.com website.
    /// </summary>
    public class Uploader : IDisposable  
    {
        /// <summary>
        /// The uploader becomes ready after the Hello() method completes a version check.
        /// </summary>
        public bool IsReady { get; private set; }
        
        private HttpClient httpClient;
        private string uploadUrl = "https://www.raidloot.com";
        //private string uploadUrl = "http://localhost:61565";
        private HashSet<string> uploadLog;
        private string privateKey;
        private Action<string> logger;
        

        public Uploader(Action<string> logger)
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "LogSync v1");
            uploadLog = new HashSet<string>();
            this.logger = logger;
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public void LaunchBrowser()
        {
            // launch the website using the private key
            // this page will redirect away so the user doesn't share their private key by accident
            Process.Start(new ProcessStartInfo(uploadUrl + "/logs/browse/" + privateKey) { UseShellExecute = true });
        }

        /// <summary>
        /// Initialize the privateKey and perform a version compatibility check using the User-Agent header.
        /// </summary>
        public async Task<bool> Hello(IConfigAdapter config)
        {
            privateKey = config.Read("privateKey");
            if (String.IsNullOrEmpty(privateKey))
            {
                privateKey = Guid.NewGuid().ToString();
                config.Write("privateKey", privateKey);
            }

            logger("Checking if your version is still compatible with " + uploadUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", privateKey);
            try
            {
                var result = await httpClient.GetAsync(uploadUrl + "/logs/hello");
                logger(await result.Content.ReadAsStringAsync());
                IsReady = result.IsSuccessStatusCode;
                return IsReady;
            }
            catch (Exception ex)
            {
                logger("Login failed: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> UploadFight(FightInfo f)
        {
            // don't upload the same thing multiple times
            if (uploadLog.Contains(f.ID))
            {
                logger("Already uploaded " + f.Name);
                return true;
            }

            logger("Uploading " + f.Name);
            var json = JsonSerializer.Serialize(f);
            //File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\sample.json", json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                //var result = await httpClient.PostAsync(uploadUrl + "/logs/fight/" + privateKey, content);
                var result = await httpClient.PostAsync(uploadUrl + "/logs/fight", content);
                if (result.IsSuccessStatusCode)
                {
                    uploadLog.Add(f.ID);
                    //logger("Upload completed.");
                    return true;
                }
                else
                {
                    logger("Upload error: " + result.StatusCode + " " + await result.Content.ReadAsStringAsync());
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger("Upload error: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> UploadLoot(List<LootInfo> items)
        {
            logger("Uploading " + items.Count + " item drops");
            var json = JsonSerializer.Serialize(items);
            //logger(json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var result = await httpClient.PostAsync(uploadUrl + "/logs/loot", content);
                if (result.IsSuccessStatusCode)
                {
                    //logger("Upload completed.");
                    return true;
                }
                else
                {
                    logger("Upload failed: " + result.StatusCode + " " + await result.Content.ReadAsStringAsync());
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger("Upload error: " + ex.Message);
                return false;
            }
        }

    }
}
