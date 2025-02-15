using BepInEx;
using ExitGames.Client.Photon;
using GorillaNetworking;
using Newtonsoft.Json;
using OVR.OpenVR;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using UnityEngine;

namespace PlayerTrakkar
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        void Start()
        {
            try {
                webhook = File.ReadAllText("PlayerTrakkar_Webhook.txt");
            } catch
            {
                File.WriteAllText("PlayerTrakkar_Webhook.txt", "");
            }

            try
            {
                webhookMessage = File.ReadAllText("PlayerTrakkar_WebhookMessage.txt");
            } catch
            {
                webhookMessage = @"```
+========================================================+
Player
    Index Name      {0}
	Nickname        {2}
	UserID          {3}
	Color           {4}
	Cosmetics       {5}
Lobby
	Name            {1}
	Region          {6}
	Gamemode        {7}
	Players         {8}/10
+========================================================+
```";
                File.WriteAllText("PlayerTrakkar_WebhookMessage.txt", webhookMessage);
            }

            try
            {
                boringWebhookMessage = File.ReadAllText("PlayerTrakkar_BoringWebhookMessage.txt");
            } catch
            {
                boringWebhookMessage = "{0} ({2}, uid {3}) found in {1} region {6} gamemode {7} players {8}/10";
                File.WriteAllText("PlayerTrakkar_BoringWebhookMessage.txt", boringWebhookMessage);
            }

            try
            {
                playerIds = File.ReadAllText("PlayerTrakkar_IDs.txt");
            }
            catch
            {
                File.WriteAllText("PlayerTrakkar_IDs.txt", playerIds);
            }

            PhotonNetwork.NetworkingClient.EventReceived += EventReceived;
        }

        public void EventReceived(EventData data)
        {
            if (data.Code == 200)
            {
                string reasoning = PhotonNetwork.PhotonServerSettings.RpcList[int.Parse(((Hashtable)data.CustomData)[(byte)5].ToString())];
                if (reasoning == "RPC_UpdateCosmetics" || reasoning == "RPC_UpdateCosmeticsWithTryon" || reasoning == "RPC_UpdateCosmeticsWithTryonPacked")
                {
                    checkCosmetics(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender, false));
                    checkSpecificPlayer(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender, false));
                }
            }
        }

        void Update()
        {
            if (archiveMessage.Length > 0)
            {
                BetaWebhookMessage(archiveMessage);
                archiveMessage = "";
                shouldPlayTaps = false;
            }
            bool keyFlag = UnityInput.Current.GetKey(KeyCode.Z);
            if (keyFlag && !lastInsert)
            {
                uiIsVisible = !uiIsVisible;
            }
            lastInsert = keyFlag;

            try
            {
                if (PhotonNetwork.InRoom && !lastInRoom)
                {
                    lastCode = null;
                    trackerDelay = Time.time + rejSpeed;
                    if (PhotonNetwork.PlayerList.Length == 1)
                    {
                        trackerDelay = Time.time + 2f;
                        SendWebhookMessage("-# **" + PhotonNetwork.CurrentRoom.Name + "** is empty");
                    }
                    checkIds();
                }

                lastInRoom = PhotonNetwork.InRoom;
            }
            catch { }

            if (isTracking)
            {
                if (!PhotonNetwork.InRoom)
                {
                    if (lobbyHop)
                    {
                        if (Time.time > trackerDelay)
                        {
                            trackerDelay = Time.time + 6f;
                            if (queueIndex == 3)
                            {
                                string[] randomLobbyNames = new string[]
                                {
                                    "1",
                                    "2",
                                    "CLOUDSCOMP",
                                    "GULLIBLE",
                                    "MODS",
                                    "MOD",
                                    "ELLIOT",
                                    "ELLIOT1",
                                    "VMT",
                                    "RUN",
                                    "HELP",
                                    "JUANGTAG",
                                    "PBBV",
                                    "JMANCURLY",
                                    "JMAN",
                                    "VMT1",
                                    "VEN1",
                                    "JUAN",
                                    "TUXEDO",
                                    "K9",
                                    "ALECVR",
                                    "RED"
                                };
                                if (lastCode != null)
                                {
                                    SendWebhookMessage("-# **" + lastCode + "** is full");
                                }

                                string tojoin = randomLobbyNames[theurges];
                                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(tojoin, JoinType.Solo);
                                lastCode = tojoin;
                                theurges++;
                                if (theurges > randomLobbyNames.Length - 1)
                                {
                                    theurges = 0;
                                }
                            } else {
                                GorillaComputer.instance.currentQueue = queueIndex == 2 ? (UnityEngine.Random.Range(0f, 1f) > 0.5f ? "COMPETITIVE" : "DEFAULT") : queues[queueIndex].Capitalize();
                                GameObject[] triggerZones = new GameObject[]
                                {
                                    GameObject.Find("Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Forest, Tree Exit"),
                                    GameObject.Find("Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - City Front"),
                                };

                                //PhotonNetwork.ConnectToRegion(new string[] { "us", "usw", "eu" }[(int)Mathf.Round(UnityEngine.Random.Range(0f, 2f))]);
                                triggerZones[mapIndex == maps.Length - 1 ? UnityEngine.Random.Range(0, triggerZones.Length - 1) : mapIndex].GetComponent<GorillaNetworkJoinTrigger>().OnBoxTriggered();
                            }
                        }
                    }
                }
                else
                {
                    if (lobbyHop)
                    {
                        if (Time.time > trackerDelay)
                        {
                            PhotonNetwork.Disconnect();
                            trackerDelay = Time.time + 3f;
                        }
                    } else
                    {
                        if (!lastInRoom)
                        {
                            checkIds();
                        }
                    }
                }
            }

            lastInRoom = PhotonNetwork.InRoom;
        }

        GUIStyle a = null;
        GUIStyle b = null;
        GUIStyle c = null;

        void OnGUI()
        {
            if (uiIsVisible) {
                if (a == null)
                {
                    a = createSolid(GUI.skin.box, new Color32(50, 25, 0, 255), new UnityEngine.Color(1, 1, 1, 1));
                }
                changeColor(a, Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), 1f, 0.1f));
                GUI.skin.box = a;
                GUI.skin.box.fontSize = 20;

                if (b == null)
                {
                    b = createButton(new Color32(0, 0, 0, 128), new UnityEngine.Color(1, 1, 1, 1));
                }
                GUI.skin.button = b;
                GUI.skin.button.fontSize = 20;

                if (c == null)
                {
                    c = createSolid(GUI.skin.textArea, new Color32(0, 0, 0, 128), new UnityEngine.Color(1, 1, 1, 1));
                }
                GUI.skin.textArea = c;
                GUI.skin.textArea.fontSize = 13;

                GUI.Box(new Rect(10, 10, 290, 430), "PlayerTrakkar");

                if (GUI.Button(new Rect(20, 90, 130, 30), "Save IDs"))
                {
                    File.WriteAllText("PlayerTrakkar_IDs.txt", playerIds);
                }

                if (GUI.Button(new Rect(160, 90, 130, 30), "Load IDs"))
                {
                    playerIds = File.ReadAllText("PlayerTrakkar_IDs.txt");
                }

                playerIds = GUI.TextArea(new Rect(20, 130, 270, 300), playerIds);

                isTracking = GUI.Toggle(new Rect(20, 40, 200, 20), isTracking, "Tracker Enabled");

                lobbyHop = GUI.Toggle(new Rect(20, 60, 200, 20), lobbyHop, "Auto Lobby Hop");

                // GUI.skin.button.fontSize = 15;

                if (GUI.Button(new Rect(160, 40, 130, 20), "Map (" + maps[mapIndex] +  ")"))
                {
                    mapIndex++;
                    if (mapIndex > maps.Length - 1)
                    {
                        mapIndex = 0;
                    }
                }

                if (GUI.Button(new Rect(300, 40, 130, 20), "Queue (" + queues[queueIndex] + ")"))
                {
                    queueIndex++;
                    if (queueIndex > 3)
                    {
                        queueIndex = 0;
                    }
                }

                noHopOnTrack = GUI.Toggle(new Rect(300, 65, 200, 20), noHopOnTrack, "No Hop on Track");

                if (GUI.Button(new Rect(160, 65, 130, 20), "Rejoin Delay (" + rejSpeed + ")"))
                {
                    rejIndex++;
                    if (rejIndex > rejSpeeds.Length - 1)
                    {
                        rejIndex = 0;
                    }

                    rejSpeed = rejSpeeds[rejIndex];
                }

                if (webhook == "") {
                    GUI.Label(new Rect(10, 10, 1200, 200), "No webhook has been specified, please input a webhook URL");
                }
            }
        }

        void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void SendWebhookMessage(string webhookMessagee)
        {
            archiveMessage += webhookMessagee + "\n";
        }

        static bool shouldPlayTaps = false;
        static string archiveMessage = "";

        private static readonly HttpClient httpClient = new HttpClient();

        async void BetaWebhookMessage(string webhookMessage)
        {
            try
            {
                var data = new StringContent($"content={webhookMessage}", Encoding.UTF8, "application/x-www-form-urlencoded");
                await httpClient.PostAsync(webhook, data);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            if (shouldPlayTaps)
            {
                try
                {
                    for (int i = 50; i < 55; i++)
                    {
                        GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(i, true, 99999f);
                    }
                }
                catch
                {
                    Debug.LogError("Error playing taps.");
                }
            }
        }

        async void BetaWebhookEmbed(string content, string title, string description, string footer, string image)
        {
            try
            {
                var embed = new
                {
                    title = title,
                    description = description,
                    color = 16744448,
                    footer = new { text = footer },
                    thumbnail = new { url = image }
                };

                var webhookMessage = new
                {
                    content = content,
                    embeds = new[] { embed }
                };

                var json = JsonConvert.SerializeObject(webhookMessage);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                await httpClient.PostAsync(webhook, data);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            lobbyHop = false;
            try
            {
                for (int i = 50; i < 55; i++)
                {
                    GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(i, true, 99999f);
                }
            }
            catch
            {
                Debug.LogError("Error playing taps.");
            }
        }

        void checkIds()
        {
            /*foreach (Photon.Realtime.Player plr in PhotonNetwork.PlayerList)
            {
                checkSpecificPlayer(plr);
            }*/
        }

        public void checkSpecificPlayer(NetPlayer plr)
        {
            //bool wasFound = false;
            try
            {
                foreach (string v in playerIds.Split("\n"))
                {
                    string playerId = v.Split(';')[0];
                    string playerName = v.Split(';')[1];

                    if (plr.UserId == playerId)
                    {
                        string indexname = playerName;
                        indexname = new string(Array.FindAll<char>(indexname.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
                        
                        string fixedName = plr.NickName;
                        fixedName = new string(Array.FindAll<char>(fixedName.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
                        if (fixedName.Length > 12)
                        {
                            fixedName = fixedName.Substring(0, 11);
                        }
                        fixedName = fixedName.ToUpper();

                        string color = "PC 0 0 0 // Quest 0 0 0";
                        VRRig pray = GetVRRigFromPlayer(plr);
                        if (pray != null)
                        {
                            color = "PC " + Math.Round(pray.playerColor.r * 255f).ToString() + " " + Math.Round(pray.playerColor.g * 255f).ToString() + " " + Math.Round(pray.playerColor.b * 255f).ToString() + " // Quest " + Math.Round(pray.playerColor.r * 9f).ToString() + " " + Math.Round(pray.playerColor.g * 9f).ToString() + " " + Math.Round(pray.playerColor.b * 9f).ToString() + " ";
                        }

                        string cosmetics = "";
                        if (pray != null)
                        {
                            cosmetics = pray.concatStringOfCosmeticsAllowed;
                        }

                        string regionnn = PhotonNetwork.NetworkingClient.CloudRegion;
                        regionnn = new string(Array.FindAll<char>(regionnn.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
                        if (regionnn.Length > 12)
                        {
                            regionnn = regionnn.Substring(0, 11);
                        }
                        regionnn = regionnn.ToUpper();

                        string formattedMessage = string.Format(webhookMessage, indexname, PhotonNetwork.CurrentRoom.Name, fixedName, plr.UserId, color, cosmetics, regionnn, PhotonNetwork.CurrentRoom.CustomProperties["gameMode"].ToString(), PhotonNetwork.PlayerList.Length);

                        shouldPlayTaps = true;
                        UnityEngine.Debug.Log("Found someone very special\n" + formattedMessage);
                        //SendWebhookMessage(formattedMessage);
                        BetaWebhookEmbed("<@&1189695503399649280>", "Player " + indexname + " found", formattedMessage, "Created by @goldentrophy", GetPhotoOfPlayer(indexname));
                    }
                }
            } catch (Exception e) { UnityEngine.Debug.Log(e.Message); }
            //checkCosmetics(plr);
            /*
            if (!wasFound)
            {
                string fixedName = plr.NickName;
                fixedName = new string(Array.FindAll<char>(fixedName.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
                if (fixedName.Length > 12)
                {
                    fixedName = fixedName.Substring(0, 11);
                }
                fixedName = fixedName.ToUpper();

                string color = "PC 0 0 0 // Quest 0 0 0";
                VRRig pray = GetVRRigFromPlayer(plr);
                if (pray != null)
                {
                    color = "PC " + Math.Round(pray.playerColor.r * 255f).ToString() + " " + Math.Round(pray.playerColor.g * 255f).ToString() + " " + Math.Round(pray.playerColor.b * 255f).ToString() + " // Quest " + Math.Round(pray.playerColor.r * 9f).ToString() + " " + Math.Round(pray.playerColor.g * 9f).ToString() + " " + Math.Round(pray.playerColor.b * 9f).ToString() + " ";
                }

                string cosmetics = "";
                if (pray != null)
                {
                    cosmetics = pray.concatStringOfCosmeticsAllowed;
                }

                string formattedMessage = string.Format(boringWebhookMessage, fixedName, PhotonNetwork.CurrentRoom.Name, fixedName, plr.UserId, color, cosmetics, PhotonNetwork.NetworkingClient.CloudRegion, PhotonNetwork.CurrentRoom.CustomProperties["gameMode"].ToString(), PhotonNetwork.PlayerList.Length);

                SendWebhookMessage(formattedMessage);
            }*/
        }

        public void checkCosmetics(Photon.Realtime.Player plr)
        {
            try
            {
                if (plr != PhotonNetwork.LocalPlayer)
                {
                    VRRig pray = GetVRRigFromPlayer(plr);
                    if (pray != null)
                    {
                        Dictionary<string, string> cosmetics = new Dictionary<string, string> { { "LBAAD.", "ADMINISTRATOR BADGE" }, { "LBAAK.", "MOD STICK" }, { "LBADE.", "FINGER PAINTER BADGE" }, { "LBAGS.", "ILLUSTRATOR BADGE" } };
                        foreach (KeyValuePair<string, string> v in cosmetics)
                        {
                            if (pray.concatStringOfCosmeticsAllowed.Contains(v.Key))
                            {
                                //wasFound = true;
                                string fixedName = plr.NickName;
                                fixedName = new string(Array.FindAll<char>(fixedName.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
                                if (fixedName.Length > 12)
                                {
                                    fixedName = fixedName.Substring(0, 11);
                                }
                                fixedName = fixedName.ToUpper();

                                string color = "PC 0 0 0 // Quest 0 0 0";
                                color = "PC " + Math.Round(pray.playerColor.r * 255f).ToString() + " " + Math.Round(pray.playerColor.g * 255f).ToString() + " " + Math.Round(pray.playerColor.b * 255f).ToString() + " // Quest " + Math.Round(pray.playerColor.r * 9f).ToString() + " " + Math.Round(pray.playerColor.g * 9f).ToString() + " " + Math.Round(pray.playerColor.b * 9f).ToString() + " ";

                                string cosmeticss = pray.concatStringOfCosmeticsAllowed;

                                string regionnn = PhotonNetwork.NetworkingClient.CloudRegion;
                                regionnn = new string(Array.FindAll<char>(regionnn.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
                                if (regionnn.Length > 12)
                                {
                                    regionnn = regionnn.Substring(0, 11);
                                }
                                regionnn = regionnn.ToUpper();

                                string formattedMessage = string.Format(webhookMessage, v.Value, PhotonNetwork.CurrentRoom.Name, fixedName, plr.UserId, color, cosmeticss, regionnn, PhotonNetwork.CurrentRoom.CustomProperties["gameMode"].ToString(), PhotonNetwork.PlayerList.Length);

                                shouldPlayTaps = true;
                                UnityEngine.Debug.Log("Found someone very special\n" + formattedMessage);
                                //SendWebhookMessage(formattedMessage);
                                BetaWebhookEmbed("<@&1189695503399649280>", v.Value + " found", formattedMessage, "Created by @goldentrophy", GetPhotoOfCosmetic(v.Value));
                            }
                        }
                        Dictionary<string, string> unimportantcosmetics = new Dictionary<string, string> { { "LFAAZ.", "2022 GLASSES" }, { "LMAJA.", "GT MONKE PLUSH" }, { "LBAAE.", "EARLY ACCESS" }, { "LBAAZ.", "GT1 BADGE" }, { "LMAAV.", "HIGH TECH SLINGSHOT" }, { "LMAAQ.", "STICKABLE TARGET" }, { "LBAIT.", "JUNIPER WHITE HOODIE BF" }, { "LMALW.", "GT MONKE JUNIPER" }, { "LMAIA.", "GT HOODIE JUNIPER" }, { "LMALX.", "GT BACKPACK JUNIPER" }, { "LHAAB.", "CAT EARS" } };
                        foreach (KeyValuePair<string, string> v in unimportantcosmetics)
                        {
                            if (pray.concatStringOfCosmeticsAllowed.Contains(v.Key))
                            {
                                //wasFound = true;
                                string fixedName = plr.NickName;
                                fixedName = new string(Array.FindAll<char>(fixedName.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
                                if (fixedName.Length > 12)
                                {
                                    fixedName = fixedName.Substring(0, 11);
                                }
                                fixedName = fixedName.ToUpper();

                                string color = "PC 0 0 0 // Quest 0 0 0";
                                color = "PC " + Math.Round(pray.playerColor.r * 255f).ToString() + " " + Math.Round(pray.playerColor.g * 255f).ToString() + " " + Math.Round(pray.playerColor.b * 255f).ToString() + " // Quest " + Math.Round(pray.playerColor.r * 9f).ToString() + " " + Math.Round(pray.playerColor.g * 9f).ToString() + " " + Math.Round(pray.playerColor.b * 9f).ToString() + " ";

                                string cosmeticss = pray.concatStringOfCosmeticsAllowed;

                                string regionnn = PhotonNetwork.NetworkingClient.CloudRegion;
                                regionnn = new string(Array.FindAll<char>(regionnn.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
                                if (regionnn.Length > 12)
                                {
                                    regionnn = regionnn.Substring(0, 11);
                                }
                                regionnn = regionnn.ToUpper();

                                string formattedMessage = string.Format(boringWebhookMessage, v.Value, PhotonNetwork.CurrentRoom.Name, fixedName, plr.UserId, color, cosmetics, regionnn, PhotonNetwork.CurrentRoom.CustomProperties["gameMode"].ToString(), PhotonNetwork.PlayerList.Length);

                                SendWebhookMessage(formattedMessage);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        string fingerpainter = "https://cdn.discordapp.com/attachments/1222354892132454400/1261807401254912151/latest.png?ex=6696f037&is=66959eb7&hm=19cae49469d98fb84240cf985861b4e8b5d99c48145f6fa297b5d3cd54ba91f5&";
        string mod = "https://cdn.discordapp.com/attachments/1222354892132454400/1261807401514963034/latest.png?ex=6696f037&is=66959eb7&hm=d1813baa7cebcaaad0a0aaecf3da1e715d437ff2a87819a3bfdc0dd5a56204f9&";
        string sweater = "https://cdn.discordapp.com/attachments/1222354892132454400/1261807401858891786/360.png?ex=6696f037&is=66959eb7&hm=d545645e2f6e6e13e0710ae6ba9d420a65a0ca16b550636530b679b9386bf0c7&";
        string admin = "https://cdn.discordapp.com/attachments/1222354892132454400/1261807402148298813/360.png?ex=6696f037&is=66959eb7&hm=296e667fe7ba033691142dffb846ca2df84e1a44553ff9ee48c174487fe6b65e&";
        string illustrator = "https://cdn.discordapp.com/attachments/1222354892132454400/1262562717797974117/illustrator.png?ex=66970ca9&is=6695bb29&hm=6a481ada7fed3f84023e07d5a4ffb7dd90fa2b690a136083a879a74891f2573a&";
        string unk = "https://cdn.discordapp.com/attachments/1222354892132454400/1262559804346466374/Tophat.webp?ex=669709f2&is=6695b872&hm=ccc71569fda13ddfbddf2830078709ed03bbf876b3c90017cfb41e82452099fc&";

        string GetPhotoOfCosmetic(string name)
        {
            switch (name) {
                case "FINGER PAINTER BADGE":
                    return fingerpainter;
                case "MOD STICK":
                    return mod;
                case "COLD MONKE SWEATER":
                    return sweater;
                case "ADMINISTRATOR BADGE":
                    return admin;
                case "ILLUSTRATOR BADGE":
                    return illustrator;
            }
            return unk;
        }

        string GetPhotoOfPlayer(string name)
        {
            name = name.ToLower();
            if (name.Contains("lemming"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1263682956593991754/bE18Unyk_400x400.jpg?ex=669b1ff6&is=6699ce76&hm=09eb0cabdb31788a3236b2d99accf31b3e90e8ee544d45faf5959473ce2a740f&";
            }
            if (name.Contains("biffbish"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1263682956145197217/JAsefbwF_400x400.jpg?ex=669b1ff6&is=6699ce76&hm=c79ab540dcb4714d65dce6db0bda45e4143ffe8b7b0487255115e54e76ac7700&";
            }
            if (name.Contains("tttpig"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1263682956376014978/channels4_profile.jpg?ex=669b1ff6&is=6699ce76&hm=7f37a2977ba814a96907895a8c520134b3fca04238940c52471eef598e3af70f&";
            }
            if (name.Contains("alec"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1263682955834822706/images.jpg?ex=669b1ff6&is=6699ce76&hm=fc75c08821281479b4638ea10bebc5083d1cd879859f92b7aa7b659c9134d2d8&";
            }
            if (name.Contains("person"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1263682955583295488/channels4_profile.png?ex=669b1ff6&is=6699ce76&hm=4878b5174dbdfbfa1d8eebd6d8a3b624dd871bf66f3282d689bad9ab06e34f9a&";
            }
            if (name.Contains("tuxedo"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1263682955289563167/channels4_profile_1.jpg?ex=669b1ff6&is=6699ce76&hm=b1f9b69862657bdd34a0d5a1ca324760295f3ba4e2c4e59533874f1423a5bae8&";
            }
            if (name.Contains("9998") || name.Contains("theythem"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1263682955000287353/DevIcon.webp?ex=669b1ff6&is=6699ce76&hm=5be0c90898fe73852debdcb94dbcffe5c14e5d98dbfc5a782a7607a93c4458df&";
            }
            if (name.Contains("cjvr"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1265873857038778430/Wja1rXJspfsYJJguYe6P75TwchT0X8HWwskY4OJWOqIiXm00JEO03fXgI2LXcvCfyJMwRzD5s900-c-k-c0x00ffffff-no-rj.png?ex=66a31866&is=66a1c6e6&hm=1a6c377653cae4722e3fa52b3c946e36a6b62a1bcb3bc0da5c6bce71644ad2a1&";
            }
            if (name.Contains("huskygt"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1274203382609088534/103238785.png?ex=66c165df&is=66c0145f&hm=7cb3f2cc0e22d6bdcde62f79301d82d44f96e6f7cdd22d32b4bd0b4f1e8d30a2&";
            }
            if (name.Contains("goatgt"))
            {
                return "https://cdn.discordapp.com/attachments/1222354892132454400/1274203382927593532/IQV6LBeVFKW-6eA01TW6Ke4pJpUgTBj_wZCjflxtnGuJAqyYvSCXFt19pX868g2TOs-jL2Hjs900-c-k-c0x00ffffff-no-rj.png?ex=66c165e0&is=66c01460&hm=fd07dd582fd369db12fedd15f26e5264a2508b4124b68b1d4b01b9d2bcf51999&";
            }
            if (name.Contains("stick"))
            {
                return mod;
            }
            if (name.Contains("admin"))
            {
                return admin;
            }
            if (name.Contains("illustrator"))
            {
                return illustrator;
            }
            return fingerpainter;
        }

        public static VRRig GetVRRigFromPlayer(NetPlayer p)
        {
            return GorillaGameManager.instance.FindPlayerVRRig(p);
        }

        GUIStyle createSolid(GUIStyle type, UnityEngine.Color bgc, UnityEngine.Color txc)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, bgc);
            texture.Apply();

            GUIStyle solid = new GUIStyle(type);
            solid.normal.background = texture;
            solid.hover.background = texture;
            solid.active.background = texture;
            solid.focused.background = texture;

            solid.normal.textColor = txc;
            solid.hover.textColor = txc;
            solid.active.textColor = txc;
            solid.focused.textColor = txc;

            return solid;
        }

        GUIStyle createButton(UnityEngine.Color bgc, UnityEngine.Color txc)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, bgc);
            texture.Apply();

            Texture2D texturec = new Texture2D(1, 1);
            texturec.SetPixel(0, 0, new UnityEngine.Color(bgc.r / 2f, bgc.g / 2f, bgc.b / 2f));
            texturec.Apply();

            GUIStyle solid = new GUIStyle(GUI.skin.button);
            solid.normal.background = texture;
            solid.hover.background = texture;
            solid.active.background = texturec;

            solid.normal.textColor = txc;
            solid.hover.textColor = txc;
            solid.active.textColor = txc;

            return solid;
        }

        void changeColor(GUIStyle solid, Color bgc)
        {
            solid.normal.background.SetPixel(0, 0, bgc);
            solid.hover.background.SetPixel(0, 0, bgc);
            solid.active.background.SetPixel(0, 0, bgc);
            solid.focused.background.SetPixel(0, 0, bgc);

            solid.normal.background.Apply();
            solid.hover.background.Apply();
            solid.active.background.Apply();
            solid.focused.background.Apply();
        }

        static bool lastInsert = false;

        static bool uiIsVisible = false;

        public static bool isTracking = true;

        public static bool noHopOnTrack = true;

        static bool lobbyHop = false;

        static bool lastInRoom = false;

        static int mapIndex = 0;

        static int queueIndex = 2;

        static float trackerDelay = 0f;

        static float rejSpeed = 3f;

        static string lastCode = null;

        static float[] rejSpeeds = new float[]
        {
            2f,
            3f,
            4f,
            5f,
            10f,
            15f,
            30f,
            60f,
        };

        static int rejIndex = 1;

        string[] maps = new string[]
        {
            "Forest",
            "City",
            "Random"
        };

        string[] queues = new string[]
        {
            "DEFAULT",
            "COMPETITIVE",
            "RANDOM",
            "HYPERSPECIFIC"
        };

        static string playerIds = "E19CE8918FD9E927;goldentrophy";

        static string webhook = "";

        static string webhookMessage = "";
        static string boringWebhookMessage = "";

        static int theurges = 0;
    }
}
