using FrooxEngine;
using Newtonsoft.Json;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Timers;

namespace ExposeSessionStatus
{
    public class SessionExposer : ResoniteMod
    {
        private const string DataHandlerUrl = "http://localhost:9393/ingest/";

        private static readonly HttpClient SharedClient = new HttpClient();

        public override string Name => "ExposeSessionStatus";

        public override string Author => "Nutcake";

        public override string Version => "0.0.3";

        public override void OnEngineInit()
        {
            Engine.Current.RunPostInit(() =>
            {
                Timer timer = new Timer(5000.0);
                timer.Elapsed += WorldsChangedListener;
                timer.AutoReset = true;
                timer.Enabled = true;
            });
        }

        private static async void WorldsChangedListener(object o, ElapsedEventArgs args)
        {
            try
            {
                var source = new List<World>();
                Engine.Current.WorldManager.GetWorlds(source);
                var dictionary = source
                    .Where(world => !string.IsNullOrEmpty(world.RawName) && world.RawName != "Local" && world.RawName != "Userspace").ToDictionary(world => world.RawName, world =>
                        new Dictionary<string, object>
                        {
                            {
                                "activeUserCount",
                                world.ActiveUserCount
                            },
                            {
                                "userCount",
                                world.UserCount
                            },
                            {
                                "accessLevel",
                                world.AccessLevel.ToString()
                            },
                            {
                                "hidden",
                                world.HideFromListing
                            }
                        });
                if (dictionary.Count == 0)
                    return;
                var content = JsonConvert.SerializeObject(dictionary);
                await SharedClient.PostAsync(DataHandlerUrl + Engine.Current.Cloud.Session.CurrentUsername, new StringContent(content));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ExposeSessionStatus] Failed to send session status: " + ex.Message + ex.StackTrace);
            }
        }
    }
}