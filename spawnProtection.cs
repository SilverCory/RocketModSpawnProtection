﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Rocket.API;
using Rocket.Core;
using Rocket.Unturned;
using Rocket.Core.Plugins;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Unturned.Player;
using Rocket.Unturned.Events;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using Steamworks;
using Rocket.Core.Commands;
using UnityEngine;

namespace RocketModSpawnProtection
{
    public class SpawnProtection : RocketPlugin<SpawnProtectionConfig>
    {
        public static SpawnProtection Instance;

        protected override void Load()
        {
            Instance = this;

            UnturnedPlayerEvents.OnPlayerRevive += OnRevive;
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;

            Logger.Log("SpawnProtection loaded!");
        }

        void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            if (player == null) return;
            if (IsExcluded(player.CSteamID.m_SteamID))
            {
                UnturnedChat.Say(player, Translate("protection_excluded"), Color.yellow);
                return;
            }

            if (Configuration.Instance.GiveProtectionOnJoin)
            {
                player.GetComponent<ProtectionComponent>().StartProtection();
            }

            if (Configuration.Instance.CancelOnCombat)
            {
                player.Player.life.onHurt += OnHurt;
            }

        }


        protected override void Unload()
        {
            Logger.Log("SpawnProtection Unloaded!");

            UnturnedPlayerEvents.OnPlayerRevive -= OnRevive;
            U.Events.OnPlayerConnected -= Events_OnPlayerConnected;

            DisableAllPlayersSpawnProtection();
        }

        void OnHurt(Player player, byte damage, Vector3 force, EDeathCause cause, ELimb limb, CSteamID killer)
        {

            UnturnedPlayer damager = UnturnedPlayer.FromCSteamID(killer);
            if (damager != null) {
                ProtectionComponent component = damager.GetComponent<ProtectionComponent>();
                if( component != null && component.protectionEnabled)
                {
                    UnturnedChat.Say(damager, Translate("canceled_combat"), Color.yellow);
                    component.StopProtection();
                }
            }

        }

        void OnRevive(UnturnedPlayer player, Vector3 position, byte angle)
        {
           
           if (!Configuration.Instance.GiveProtectionOnRespawn || player == null)
                return;

            if (IsExcluded(player.CSteamID.m_SteamID))
            {
                UnturnedChat.Say(player, Translate("protection_excluded"), Color.yellow);
                return;
            }

            player.GetComponent<ProtectionComponent>().StartProtection();
        }

        public override Rocket.API.Collections.TranslationList DefaultTranslations
        {
            get
            {
                return new Rocket.API.Collections.TranslationList
                {
                    {"prot_started", "You have spawn protection for {0} seconds!"},
                    {"canceled_item", "Your spawn protection expired because you equipted a item!"},
                    {"expired", "Your spawn protection expired!"},
                    {"canceled_veh", "Your spawn protection expired because you are in a vehicle with others!"},
                    {"admin_prot_enabled", "Enabled protection on {0}!"},
                    {"admin_prot_disabled", "Disabled protection on {0}!"},
                    {"usage_start", "Correct command usage: /pstart <player>"},
                    {"usage_stop", "Correct command usage: /pstop <player>"},
                    {"noplayer", "Player '{0}' not found!"},
                    {"canceled_punch", "Your spawn protection expired because you punched!"},
                    {"canceled_dist", "Your protection has expired because of moving away from spawn!" },
                    {"canceled_bedrespawn", "You were not giving spawnprotection due to spawning at your bed"},
                    {"canceled_combat", "Your spawn protection expired because you were involved in PVP combat!"},
                    {"protection_excluded", "You have disabled spawnprotection for yourself, do /toggleprotection to enable again"},
                    {"toggled_protection_on", "You will now receive spawn protection"},
                    {"toggled_protection_off", "You will no longer receive spawn protection"}
                };
            }
        }

        [RocketCommand("startprot", "Manually enables spawnprotection on a player", "<player>", AllowedCaller.Both)]
        [RocketCommandAlias("pstart")]
        public void EnableProtCMD(IRocketPlayer caller, string[] command)
        {
            if (command.Length != 1)
            {
                UnturnedChat.Say(caller, Translate("usage_start"));
                return;
            }

            UnturnedPlayer uP = UnturnedPlayer.FromName(command[0]);
            if (uP == null)
            {
                UnturnedChat.Say(caller, Translate("noplayer", command[0]));
                return;
            }

            uP.GetComponent<ProtectionComponent>().StartProtection();
            sendMSG(caller, Translate("admin_prot_enabled", uP.DisplayName));
        }

        [RocketCommand("stopprot", "Manually disables spawnprotection on a player", "<player>", AllowedCaller.Both)]
        [RocketCommandAlias("pstop")]
        public void DisableProtCMD(IRocketPlayer caller, string[] command)
        {
            if (command.Length != 1)
            {
                UnturnedChat.Say(caller, Translate("usage_stop"));
                return;
            }

            UnturnedPlayer uP = UnturnedPlayer.FromName(command[0]);
            if (uP == null)
            {
                UnturnedChat.Say(caller, Translate("noplayer", command[0]));
                return;
            }

            uP.GetComponent<ProtectionComponent>().StopProtection();
            sendMSG(caller, Translate("admin_prot_disabled", uP.DisplayName));
        }

        void sendMSG(IRocketPlayer caller, string msg)
        {
            if (caller is ConsolePlayer)
            {
                Logger.Log(msg);
            }
            else
            {
                UnturnedChat.Say(caller, msg, GetCmdMsgColor());
            }
        }

        void DisableAllPlayersSpawnProtection()
        {
            foreach (var player in Provider.clients)
            {
                try
                {
                    var uP = UnturnedPlayer.FromSteamPlayer(player);
                    if (uP == null) continue;

                    var component = uP.GetComponent<ProtectionComponent>();
                    if (component == null) continue;

                    if (component.protectionEnabled)
                    {
                        component.StopProtection();
                    }
                }
                catch { }
            }
        }

        void SendCommandMessage(IRocketPlayer caller, string msg)
        {
            if (!(caller is ConsolePlayer)) UnturnedChat.Say(caller, msg);
        }

        UnityEngine.Color GetCmdMsgColor()
        {
            return UnturnedChat.GetColorFromName(Configuration.Instance.CommandMessageColor, UnityEngine.Color.green);
        }

        public static UnityEngine.Color GetProtMsgColor()
        {
            return UnturnedChat.GetColorFromName(SpawnProtection.Instance.Configuration.Instance.ProtectionMessageColor, UnityEngine.Color.yellow);
        }

        internal static bool CheckIfNearBed(UnturnedPlayer player)
        {
            if (Instance.Configuration.Instance.CancelOnBedRespawn)
            {
                byte bedAngle;
                Vector3 bedPos;
                if (BarricadeManager.tryGetBed(player.CSteamID, out bedPos, out bedAngle) && Vector3.Distance(bedPos, player.Position) < 15)
                {
                    UnturnedChat.Say(player, Instance.Translate("canceled_bedrespawn"), UnturnedChat.GetColorFromName(Instance.Configuration.Instance.ProtectionMessageColor, Color.red));
                    return true;
                }
            }
            return false;
        }

        internal static bool IsExcluded(ulong id)
        {
            return Instance.Configuration.Instance.NoSpawnProtection.Contains(id);
        }
    }
}
