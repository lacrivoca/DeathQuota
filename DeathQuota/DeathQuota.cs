using BepInEx;
using BepInEx.Logging;
using LobbyCompatibility.Attributes;
using LobbyCompatibility.Enums;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using LobbyCompatibility.Configuration;

namespace DeathQuota
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.sigurd.csync", "5.0.1")]
    [LobbyCompatibility(CompatibilityLevel.Everyone, VersionStrictness.None)]
    public class DeathQuota : BaseUnityPlugin
    {
        public static DeathQuota Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        internal static new ConfigMain Config;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;
            Config = new ConfigMain(base.Config);

            Hook();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        internal static void Hook()
        {
            On.HUDManager.ApplyPenalty += HUDManager_ApplyPenalty;
            On.TimeOfDay.SetNewProfitQuota += TimeOfDay_SetNewProfitQuota;
        }

        private static void TimeOfDay_SetNewProfitQuota(On.TimeOfDay.orig_SetNewProfitQuota orig, TimeOfDay self)
        {
            if (!Config.ReplaceVanilla)
            {
                orig(self);
            }
            if (self.IsHost) 
            {
                self.timesFulfilledQuota++;
                int num = self.quotaFulfilled - self.profitQuota;
                self.timeUntilDeadline = self.totalTime * 4f;
                int overtime = num / 5 + 15 * self.daysUntilDeadline;
                self.SyncNewProfitQuotaClientRpc(self.profitQuota, overtime, self.timesFulfilledQuota);
            }
            else
            {
                return;
            }
        }

        private static void HUDManager_ApplyPenalty(On.HUDManager.orig_ApplyPenalty orig, HUDManager self, int playersDead, int bodiesInsured)
        {
            if (Config.ReplaceVanilla)
            {
                VanillaReplacement(orig, self, playersDead, bodiesInsured);
            }
            else
            {
                VanillaAddition(orig, self, playersDead, bodiesInsured);
            }
        }

        private static void VanillaReplacement(On.HUDManager.orig_ApplyPenalty orig, HUDManager self, int playersDead, int bodiesInsured)
        {
            float num = 0.2f;
            int currentQuota = TimeOfDay.Instance.profitQuota;
            bodiesInsured = Mathf.Max(bodiesInsured, 0);
            if (bodiesInsured == playersDead)
            {
                Logger.LogDebug("DeathQuota: All bodies collected.");
                self.statsUIElements.penaltyAddition.text = string.Format("{0} casualties: +0%\nAll bodies recovered", playersDead);
                self.statsUIElements.penaltyTotal.text = string.Format("Quota Unchanged");
                TimeOfDay.Instance.profitQuota = currentQuota;
            }
            else
            {

                for (int i = 0; i < playersDead - bodiesInsured; i++)
                {
                    currentQuota += (int)((float)currentQuota * num);
                }
                for (int j = 0; j < bodiesInsured; j++)
                {
                    currentQuota += (int)((float)currentQuota * (num / 2.5f));
                }
                self.statsUIElements.penaltyAddition.text = string.Format("{0} casualties: +{1}%\n({2} bodies recovered)", playersDead, num * 100f * (float)(playersDead - bodiesInsured), bodiesInsured);
                self.statsUIElements.penaltyTotal.text = string.Format("Quota Increased To: ${0}", currentQuota);
                TimeOfDay.Instance.profitQuota = currentQuota;
            }
        }

        private static void VanillaAddition(On.HUDManager.orig_ApplyPenalty orig, HUDManager self, int playersDead, int bodiesInsured)
        {
            float num = 0.2f;
            int currentQuota = TimeOfDay.Instance.profitQuota;
            bodiesInsured = Mathf.Max(bodiesInsured, 0);
            if (bodiesInsured == playersDead)
            {
                Logger.LogDebug("DeathQuota: All bodies collected.");
                self.statsUIElements.penaltyAddition.text = string.Format("{0} casualties: -{1}%\nAll bodies recovered", playersDead, num * 100f * (float)(playersDead - bodiesInsured), bodiesInsured);
                self.statsUIElements.penaltyTotal.text = string.Format("Quota Unchanged");
                TimeOfDay.Instance.profitQuota = currentQuota;
            }
            else
            {

                for (int i = 0; i < playersDead - bodiesInsured; i++)
                {
                    currentQuota += Config.BodyCost;
                }
                for (int j = 0; j < bodiesInsured; j++)
                {
                    currentQuota += (int)(Config.BodyCost * num);
                }
                self.statsUIElements.penaltyAddition.text = string.Format("{0} casualties: -{1}%\n({2} bodies recovered)", playersDead, num * 100f * (float)(playersDead - bodiesInsured), bodiesInsured);
                self.statsUIElements.penaltyTotal.text = string.Format("Quota Increased To: ${0}", currentQuota);
                TimeOfDay.Instance.profitQuota = currentQuota;
            }
        }
    }
}
