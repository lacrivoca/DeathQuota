using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;

namespace DeathQuota
{
    public class ConfigMain : SyncedConfig2<ConfigMain>
    {
        [DataMember] public SyncedEntry<bool> ReplaceVanilla {  get; private set; }
        [DataMember] public SyncedEntry<int> BodyCost { get; private set; }
        public ConfigMain(ConfigFile cfg) : base("lacrivoca.DeathQuota"){
            ConfigManager.Register(this);
            ReplaceVanilla = cfg.BindSyncedEntry(
                new ConfigDefinition("General", "Replace Vanilla"),
                false,
                new ConfigDescription("Whether to completely replace the vanilla quota system with a 20% increase per unrecovered body or to simply add a flat amount per body.")
                );

            BodyCost = cfg.BindSyncedEntry(
                new ConfigDefinition("General", "Body Cost"),
                50,
                new ConfigDescription("Amount to increase the quota by per unrecovered body. Only used if Replace Vanilla is set to false.")
                );
        }
    }
}
