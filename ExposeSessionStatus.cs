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
        private static readonly HttpClient SharedClient = new HttpClient();

        public override string Name => "ExposeSessionStatus";

        public override string Author => "Nutcake";

        public override string Version => "0.0.3";

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<string> DataHandlerUrl = new ModConfigurationKey<string>("url", "Data Handler URL", computeDefault: () => "localhost:9393", internalAccessOnly: true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<int> UpdateInterval = new ModConfigurationKey<int>("update_interval", "Update Interval (sec)", computeDefault: () => 5, internalAccessOnly: true);

        private static ModConfiguration Config;

        public override void OnEngineInit()
        {
            Config = GetConfiguration();
            Config.Save(true);

            Engine.Current.RunPostInit(() =>
            {
                Timer timer = new Timer(Config.GetValue(UpdateInterval) * 1000);
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
                string Url = String.Format("http://{0}/ingest/{1}", Config.GetValue(DataHandlerUrl), Engine.Current.Cloud.Session.CurrentUsername);
                await SharedClient.PostAsync(Url, new StringContent(content));
            }
            catch (Exception ex)
            {
                Warn("[ExposeSessionStatus] Failed to send session status: " + ex.Message + ex.StackTrace);
            }
        }
    }
}