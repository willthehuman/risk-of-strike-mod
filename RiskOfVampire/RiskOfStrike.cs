using BepInEx;
using R2API;
using R2API.Utils;
using R2API.Networking;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Networking;
using BepInEx.Configuration;
using MonoMod.Cil;
using System;
using R2API.Networking.Interfaces;
using UnityEngine.SceneManagement;
using MonoMod.RuntimeDetour;
using static ProBuilder.MeshOperations.pb_MeshImporter;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
namespace RiskOfStrike
{
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(DifficultyAPI), nameof(DirectorAPI), nameof(NetworkingAPI))]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class.
    public class RiskOfStrike : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "mochi";
        // PluginNameに空白を入れると読み込まれない
        public const string PluginName = "RiskOfVampire";
        public const string PluginVersion = "2.1.1";

        public static GameObject OptionPickup;
        public static Sprite MonsoonIcon;
        public static GameObject ChatPrefab;

        internal static SyncConfig syncConfig;

        // 割当は読み込み後じゃないとnullになる
        // RoR2が読み込まれてないからかな
        public ItemDef scrapWhite;
        public ItemDef scrapGreen;

        // こいつらはRunごとにリセットが必要
        public int ambientLevelFloor = 1;
        //public int tier1Count = 0;
        //public int tier1CountPrev = 0;
        //public int tier2Count = 0;
        //public int tier2CountPrev = 0;

        // ConfigEntryはBaseUnityPluginを継承したクラスでしかBindできない
        // そこで定義とBindはこのクラスでやる
        // config need to sync
        private static ConfigEntry<float> InvTime;
        private static ConfigEntry<float> OspPercent;
        private static ConfigEntry<float> HealPerSecond;
        private static ConfigEntry<float> PossessedItemChance;
        private static ConfigEntry<float> MoneyScaling;
        private static ConfigEntry<int> ItemPickerOptionAmount;
        private static ConfigEntry<int> RandomItemAddPoolCount;
        private static ConfigEntry<int> WhiteItemUpperLimit;
        private static ConfigEntry<int> GreenItemUpperLimit;

        // config don't need to sync
        private static ConfigEntry<float> MultiShopSpawnChance;
        private static ConfigEntry<float> ScrapperSpawnChance;
        private static ConfigEntry<float> PrinterSpawnChance;
        private static ConfigEntry<float> ObjectSumMultiply;
        private static ConfigEntry<float> ChanceShrineSpawnChance;
        private static ConfigEntry<float> VoidItemPodSpawnChance;

        private static ConfigEntry<float> HealMultiply;



        // ゲームの起動時に呼ばれる
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);
            //NetworkingAPI.RegisterMessageType<SyncConfig>();
            //BindConfig();
            //LoadAssets();
            //DefineDifficulties();

            //// Hooks
            //Hook_RunStart();
            //HookMoneyScaling();
            //Hook_PickupPicker_OnInteractionBegin();
            //Hook_ChestBehavior();
            //HookOSP();
            //HooksForLog();
            //HookMonsterSpawn();
            //HookHeal();
            //Hook_HPDefine();
            //Hook_Spawns();
            //Hook_forItemCountChat();
            //Hook_forItemSelect();
            //Hook_ShrineDrop();
            //Hook_BossDrop();
            //Hook_DifficultyScaling();

            //Set core hooks
            On.RoR2.Run.Start += Run_Start; ;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            On.RoR2.UI.MainMenu.BaseMainMenuScreen.Awake += BaseMainMenuScreen_Awake;
            On.RoR2.Networking.NetworkManagerSystem.OnServerAddPlayerInternal += NetworkManagerSystem_OnServerAddPlayerInternal;
            SceneManager.sceneLoaded += Init; //Let me know if there's a better way of doing this
        }

        private void Init(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == "title")
            {
                if (BodyCatalog.availability.available &&
                    ItemCatalog.availability.available &&
                    EquipmentCatalog.availability.available)
                {
                    //Loads Config Settings
                    //Settings.LoadConfig(Config);

                    //Setup
                    //Hooks.Init();

                    Debug.Log("GameModeAPI setup completed @GameModeAPI");
                }
                else Debug.LogError("Failed to load GameModeAPI, please let the developer know on \"https://github.com/tung362/RoR2GameModeAPI/issues\" @GameModeAPI");
                SceneManager.sceneLoaded -= Init;
            }
        }

        private void BaseMainMenuScreen_Awake(On.RoR2.UI.MainMenu.BaseMainMenuScreen.orig_Awake orig, RoR2.UI.MainMenu.BaseMainMenuScreen self)
        {
            foreach(Transform child in self.myMainMenuController.moreMenuScreen.transform)
            {
                Debug.Log(child.name);
            }
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            throw new NotImplementedException();
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            throw new NotImplementedException();
        }

        private void NetworkManagerSystem_OnServerAddPlayerInternal(On.RoR2.Networking.NetworkManagerSystem.orig_OnServerAddPlayerInternal orig, RoR2.Networking.NetworkManagerSystem self, NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
        {
            throw new NotImplementedException();
        }
    }
}

#pragma warning restore Publicizer001 // Accessing a member that was not originally public
