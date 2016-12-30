﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rocket.API;
using System.Xml.Serialization;

namespace RocketModSpawnProtection
{
    public class SpawnProtectionConfig : IRocketPluginConfiguration
    {
        public bool GiveVanishWhileProtected;
        public bool CancelProtectionOnEquip;
        public bool CancelProtectionIfInVehicleWithOthers;
        public bool GiveProtectionOnJoin;
        public bool GiveProtectionOnRespawn;
        public bool AutoRepairProtectedPlayersVehicles;
        public bool SendProtectionMessages;
        public int ProtectionTime;
        public int MaxProtectionVanishTime;
        public double ProtectionVanishDelayMilliseconds;
        public string ProtectionMessageColor;
        public string CommandMessageColor;

        [XmlArrayItem(ElementName = "ID")]
        public List<ushort> WhitelistedItems;
       //public int MaxVanishDistFromSpawn;

        public void LoadDefaults()
        {
            GiveVanishWhileProtected = true;
            CancelProtectionOnEquip = true;
            CancelProtectionIfInVehicleWithOthers = true;
            GiveProtectionOnJoin = false;
            GiveProtectionOnRespawn = true;
            AutoRepairProtectedPlayersVehicles = true;
            SendProtectionMessages = true;
            ProtectionTime = 30;
            MaxProtectionVanishTime = 6;
            ProtectionVanishDelayMilliseconds = 1000;
            ProtectionMessageColor = "Yellow";
            CommandMessageColor = "Green";

            WhitelistedItems = new List<ushort> { ushort.MaxValue-1 };
            //MaxVanishDistFromSpawn = 30;
        }
    }
}
