using BepInEx;
using BepInEx.Logging;
using LobbyCompatibility.Attributes;
using LobbyCompatibility.Enums;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;

namespace DeathQuota
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.HardDependency)]
    [LobbyCompatibility(CompatibilityLevel.Everyone, VersionStrictness.None)]
    public class DeathQuota : BaseUnityPlugin
    {
        public static DeathQuota Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

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
        }

        private static void HUDManager_ApplyPenalty(On.HUDManager.orig_ApplyPenalty orig, HUDManager self, int playersDead, int bodiesInsured)
        {
            float num = 0.2f;
            Terminal terminal = FindAnyObjectByType<Terminal>();
            int creditCount = terminal.groupCredits;
            int currentQuota = TimeOfDay.Instance.profitQuota;
            bodiesInsured = Mathf.Max(bodiesInsured, 0);
            for (int i = 0; i < playersDead - bodiesInsured; i++)
            {
                currentQuota += (int)((float)currentQuota * num);
            }
            for (int j = 0; j < bodiesInsured; j++)
            {
                currentQuota += (int)((float)currentQuota * (num / 2.5f));
            }
            if (terminal.groupCredits < 0)
            {
                terminal.groupCredits = 0;
            }
            self.statsUIElements.penaltyAddition.text = string.Format("{0} casualties: -{1}%\n({2} bodies recovered)", playersDead, num * 100f * (float)(playersDead - bodiesInsured), bodiesInsured);
            self.statsUIElements.penaltyTotal.text = string.Format("QUOTA INCREASED TO: ${0}", currentQuota);
            Debug.Log(string.Format("New group credits after penalty: {0}", terminal.groupCredits));
            TimeOfDay.Instance.profitQuota = currentQuota;
        }
    }
}
