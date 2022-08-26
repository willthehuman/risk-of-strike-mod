﻿using BepInEx;
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
            NetworkingAPI.RegisterMessageType<SyncConfig>();
            BindConfig();
            LoadAssets();
            DefineDifficulties();

            // Hooks
            Hook_RunStart();
            HookMoneyScaling();
            Hook_PickupPicker_OnInteractionBegin();
            Hook_ChestBehavior();
            HookOSP();
            HooksForLog();
            HookMonsterSpawn();
            HookHeal();
            Hook_HPDefine();
            Hook_Spawns();
            Hook_forItemCountChat();
            Hook_forItemSelect();
            Hook_ShrineDrop();
            Hook_BossDrop();
            Hook_DifficultyScaling();

        }

        private void Hook_DifficultyScaling()
        {
            On.RoR2.Run.RecalculateDifficultyCoefficentInternal += (orig, self) =>
            {
                if (IsDefaultDifficulty())
                {
                    orig(self);
                }
                else
                {
                    // 書き換え時、thisにもselfにもambientLevelFloorがあるから注意

                    // Default 1.15f
                    // ステージ5でのステージ係数.これが時間Lvとかけ合わさって最終Lvになる
                    // 1.15^5 = 2.0
                    // 1.10^5 = 1.6
                    // 1.05^5 = 1.3
                    // ステージ10のとき
                    // 1.15^10 = 4.0
                    // 1.10^10 = 2.6
                    // 1.05^10 = 1.6
                    float stageDifficultyCoefficient = 1.05f;
                    // 時間係数. これが最終Lvに倍数として掛かる
                    // Default 1.0f
                    //ステージ2のとき
                    // 時間係数が高いと、ステージ2で急にレベルが上がる
                    // 1.15^1 * 1.0 = 1.15
                    // 1.10^1 * 1.2 = 1.32
                    // 1.10^1 * 1.3 = 1.43
                    // 1.05^1 * 1.5 = 1.57
                    // 1.05^1 * 1.6 = 1.68
                    // ステージ6のとき
                    // 1.15^5 * 1.0 = 2.00
                    // 1.10^5 * 1.2 = 1.93
                    // 1.10^5 * 1.3 = 2.09
                    // 1.05^5 * 1.5 = 1.91
                    // 1.05^5 * 1.6 = 2.04
                    // ステージ11のとき
                    // 2週目で狂気的なスケーリングをしないために、2週目の係数は低くて良い
                    // 1.15^10 * 1.0 = 4.00
                    // 1.10^10 * 1.2 = 3.11
                    // 1.10^10 * 1.3 = 3.37
                    // 1.05^10 * 1.5 = 2.44
                    // 1.05^10 * 1.6 = 2.60
                    float timeDifficultyCoefficient = 1.5f;

                    float num = self.GetRunStopwatch() * timeDifficultyCoefficient;
                    DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty);
                    float num2 = Mathf.Floor(num * 0.016666668f);
                    float num3 = (float)self.participatingPlayerCount * 0.3f;
                    float num4 = 0.7f + num3;
                    float num5 = 0.7f + num3;
                    float num6 = Mathf.Pow((float)self.participatingPlayerCount, 0.2f);
                    float num7 = 0.0506f * difficultyDef.scalingValue * num6;
                    float num8 = 0.0506f * difficultyDef.scalingValue * num6;
                    // Modified
                    float num9 = Mathf.Pow(stageDifficultyCoefficient, (float)self.stageClearCount);
                    self.compensatedDifficultyCoefficient = (num5 + num8 * num2) * num9;
                    self.difficultyCoefficient = (num4 + num7 * num2) * num9;
                    float num10 = (num4 + num7 * (num * 0.016666668f)) * Mathf.Pow(stageDifficultyCoefficient, (float)self.stageClearCount);
                    self.ambientLevel = Mathf.Min((num10 - num4) / 0.33f + 1f, (float)Run.ambientLevelCap);
                    int ambientLevelFloor = this.ambientLevelFloor;
                    self.ambientLevelFloor = Mathf.FloorToInt(self.ambientLevel);
                    if (ambientLevelFloor != self.ambientLevelFloor && ambientLevelFloor != 0 && self.ambientLevelFloor > ambientLevelFloor)
                    {
                        self.OnAmbientLevelUp();
                    }
                }
            };
        }

        private bool IsDefaultDifficulty()
        {
            // デフォルト難易度は0~, Invalidが-1, 追加難易度は-2から負に向かう
            return (int)Run.instance.selectedDifficulty >= -1;
        }

        private static void Hook_BossDrop()
        {
            On.RoR2.BossGroup.DropRewards += (orig, self) =>
            {
                int participatingPlayerCount = Run.instance.participatingPlayerCount;
                if (participatingPlayerCount != 0 && self.dropPosition)
                {
                    PickupIndex pickupIndex = PickupIndex.none;
                    if (self.dropTable)
                    {
                        pickupIndex = self.dropTable.GenerateDrop(self.rng);
                    }
                    else
                    {
                        List<PickupIndex> list = Run.instance.availableTier2DropList;
                        if (self.forceTier3Reward)
                        {
                            list = Run.instance.availableTier3DropList;
                        }
                        pickupIndex = self.rng.NextElementUniform<PickupIndex>(list);
                    }
                    int num = 1 + self.bonusRewardCount;
                    if (self.scaleRewardsByPlayerCount)
                    {
                        num *= participatingPlayerCount;
                    }
                    float angle = 360f / (float)num;
                    Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
                    Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                    int i = 0;
                    while (i < num)
                    {
                        PickupIndex pickupIndex2 = pickupIndex;
                        // ボスドロップだったらpickupIndex2を書き換える
                        if ((self.bossDrops.Count > 0 || self.bossDropTables.Count > 0) && self.rng.nextNormalizedFloat <= self.bossDropChance)
                        {
                            if (self.bossDropTables.Count > 0)
                            {
                                pickupIndex2 = self.rng.NextElementUniform<PickupDropTable>(self.bossDropTables).GenerateDrop(self.rng);
                            }
                            else
                            {
                                pickupIndex2 = self.rng.NextElementUniform<PickupIndex>(self.bossDrops);
                            }
                        }
                        // 緑アイテムだったら
                        // 4ステージ目でTier3ボスドロップはあるので除外する
                        if (pickupIndex2.pickupDef.itemTier != ItemTier.Boss
                            && pickupIndex2.pickupDef.itemTier != ItemTier.VoidBoss
                            && pickupIndex2.pickupDef.itemTier != ItemTier.Tier3)
                        {
                            var options = PickupPickerController.GenerateOptionsFromDropTable(5, self.dropTable, self.rng);
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2),
                                pickerOptions = options,
                                //pickerOptions = PickupPickerController.SetOptionsFromInteractor(),
                                rotation = Quaternion.identity,
                                prefabOverride = OptionPickup
                            }, self.dropPosition.position, vector);
                        }
                        // ボスアイテムとTier3だったら元のまま
                        else
                        {
                            PickupDropletController.CreatePickupDroplet(pickupIndex2, self.dropPosition.position, vector);
                        }
                        i++;
                        vector = rotation * vector;
                    }
                }
            };
        }

        private void Hook_ShrineDrop()
        {
            On.RoR2.ShrineChanceBehavior.AddShrineStack += (orig, self, activator) =>
            {
                if (!NetworkServer.active)
                {
                    Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineChanceBehavior::AddShrineStack(RoR2.Interactor)' called on client");
                    return;
                }
                PickupIndex pickupIndex = PickupIndex.none;
                if (self.dropTable)
                {
                    if (self.rng.nextNormalizedFloat > self.failureChance)
                    {
                        pickupIndex = self.dropTable.GenerateDrop(self.rng);
                    }
                }
                else
                {
                    PickupIndex none = PickupIndex.none;
                    PickupIndex value = self.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
                    PickupIndex value2 = self.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
                    PickupIndex value3 = self.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
                    PickupIndex value4 = self.rng.NextElementUniform<PickupIndex>(Run.instance.availableEquipmentDropList);
                    WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>(8);
                    weightedSelection.AddChoice(none, self.failureWeight);
                    weightedSelection.AddChoice(value, self.tier1Weight);
                    weightedSelection.AddChoice(value2, self.tier2Weight);
                    weightedSelection.AddChoice(value3, self.tier3Weight);
                    weightedSelection.AddChoice(value4, self.equipmentWeight);
                    pickupIndex = weightedSelection.Evaluate(self.rng.nextNormalizedFloat);
                }
                bool flag = pickupIndex == PickupIndex.none;
                string baseToken;
                // 失敗時
                if (flag)
                {
                    baseToken = "SHRINE_CHANCE_FAIL_MESSAGE";
                }
                else
                {
                    baseToken = "SHRINE_CHANCE_SUCCESS_MESSAGE";
                    self.successfulPurchaseCount++;

                    // アイテムだったら
                    if (pickupIndex.pickupDef.equipmentIndex == EquipmentIndex.None)
                    {
                        var myDropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
                        myDropTable.Regenerate(Run.instance);
                        BasicPickupDropTable basicDropTable = myDropTable as BasicPickupDropTable;
                        PickupPickerController.Option[] pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(5, myDropTable, self.rng);
                        if (basicDropTable != null)
                        {
                            basicDropTable.selector.Clear();
                            if (pickupIndex.pickupDef.itemTier == ItemTier.Tier1)
                            {
                                basicDropTable.Add(Run.instance.availableTier1DropList, 1f);
                            }
                            else if (pickupIndex.pickupDef.itemTier == ItemTier.Tier2)
                            {
                                basicDropTable.Add(Run.instance.availableTier2DropList, 1f);
                            }
                            else if (pickupIndex.pickupDef.itemTier == ItemTier.Tier3)
                            {
                                basicDropTable.Add(Run.instance.availableTier3DropList, 1f);
                            }
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(5, basicDropTable, self.rng);
                        }
                        ItemTier lowestItemTier = pickerOptions[0].pickupIndex.pickupDef.itemTier;

                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(lowestItemTier),
                            pickerOptions = pickerOptions,
                            //pickerOptions = PickupPickerController.SetOptionsFromInteractor(),
                            rotation = Quaternion.identity,
                            prefabOverride = OptionPickup
                        }, self.dropletOrigin.position, self.dropletOrigin.forward * 20f);
                    }
                    else
                    {
                        PickupDropletController.CreatePickupDroplet(pickupIndex, self.dropletOrigin.position, self.dropletOrigin.forward * 20f);
                    }

                }
                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    subjectAsCharacterBody = activator.GetComponent<CharacterBody>(),
                    baseToken = baseToken
                });
                // ここはShrineChanceBehaviorの中でしか実行できない
                // 連続失敗のアチーブメントで使われているだけなので良し
                //Action<bool, Interactor> action = ShrineChanceBehavior.onShrineChancePurchaseGlobal;
                //if (action != null)
                //{
                //    action(flag, activator);
                //}
                self.waitingForRefresh = true;
                self.refreshTimer = 2f;
                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                {
                    origin = base.transform.position,
                    rotation = Quaternion.identity,
                    scale = 1f,
                    color = self.shrineColor
                }, true);
                if (self.successfulPurchaseCount >= self.maxPurchaseCount)
                {
                    self.symbolTransform.gameObject.SetActive(false);
                }
            };
        }

        private void Hook_forItemSelect()
        {
            On.RoR2.PickupPickerController.CreatePickup_PickupIndex += (orig, self, pickupIndex) =>
            {
                if (!NetworkServer.active)
                {
                    Debug.LogWarning("[Server] function 'System.Void RoR2.PickupPickerController::CreatePickup(RoR2.PickupIndex)' called on client");
                    return;
                }
                // 現在開いている人のinventory
                var inventory = self.networkUIPromptController.currentParticipantMaster.inventory;
                ItemTier itemTier = pickupIndex.pickupDef.itemTier;

                // Scrap選択なら一つ増やして終わり
                if (pickupIndex.pickupDef.itemIndex == scrapWhite.itemIndex
                    || pickupIndex.pickupDef.itemIndex == scrapGreen.itemIndex)
                {
                    inventory.GiveItem(pickupIndex.pickupDef.itemIndex, 1);
                    return;
                }
                // 対応するScrapを持っていれば追加取得
                if (itemTier == ItemTier.Tier1 && inventory.GetItemCount(scrapWhite) >= 1)
                {
                    inventory.GiveItem(pickupIndex.pickupDef.itemIndex, 2);
                    inventory.RemoveItem(scrapWhite, 1);
                }
                else if (itemTier == ItemTier.Tier2 && inventory.GetItemCount(scrapGreen) >= 1)
                {
                    inventory.GiveItem(pickupIndex.pickupDef.itemIndex, 2);
                    inventory.RemoveItem(scrapGreen, 1);
                }
                else
                {
                    inventory.GiveItem(pickupIndex.pickupDef.itemIndex, 1);
                }
            };
        }

        // 最初のフレームの実行直前
        public void Start()
        {
        }

        private void Hook_forItemCountChat()
        {
            // RpcItemRemoveも検討
            //On.RoR2.Inventory.RpcItemAdded += (orig, self, itemIndex) =>
            On.RoR2.Inventory.HandleInventoryChanged += (orig, self) =>
            {
                orig(self);
                if (self.isLocalPlayer || self.isClient || self.isServer)
                    if (NetworkUser.localPlayers != null)
                        if (NetworkUser.localPlayers.Count == 1)
                            if (NetworkUser.localPlayers[0].master != null)
                                if (NetworkUser.localPlayers[0].master.inventory != null)
                                    if (self == NetworkUser.localPlayers[0].master.inventory)
                                        if (self.gameObject != null)
                                            if (self.gameObject.GetComponent<PlayerCharacterMasterController>() != null)
                                                {
                                                    // OnInventoryChangedからは、isChangedの場合のみ
                                                    ShowItemCount(self);
                                                }
            };
            On.RoR2.UI.ChatBox.UpdateFade += (orig, self, deltaTime) =>
            {
                orig(self, deltaTime);
                if (self.fadeGroup != null)
                    self.fadeGroup.alpha = 1f;
            };
        }

        private void ShowItemCount(Inventory inventory)
        {
            int[] counts = ItemRecount(inventory);
            SendItemCountMessage(counts[0], counts[1]);
        }

        private void SendItemCountMessage(int tier1Count, int tier2Count)
        {
            int tier1Limit = syncConfig.whiteItemUpperLimit;
            int tier2Limit = syncConfig.greenItemUpperLimit;
            string chatMessage = $"<color=#8899b9>WhiteItem: </color><color=#ffffff>{tier1Count}/{tier1Limit}</color>, <color=#8899b9>GreenItem: </color><color=#73ff44>{tier2Count}/{tier2Limit}.</color>";
            Chat.AddMessage(new Chat.SimpleChatMessage
            {
                baseToken = chatMessage
            });
        }

        private int[] ItemRecount(Inventory inventory)

        {
            //tier1Count = inventory.GetTotalItemCountOfTier(ItemTier.Tier1)
            //+ inventory.GetTotalItemCountOfTier(ItemTier.VoidTier1);
            //tier2Count = inventory.GetTotalItemCountOfTier(ItemTier.Tier2)
            //+ inventory.GetTotalItemCountOfTier(ItemTier.VoidTier2);
            int tier1Count = 0;
            int tier2Count = 0;
            for (int i = 0; i < inventory.itemAcquisitionOrder.Count; i++)
            {
                ItemIndex itemIndex = inventory.itemAcquisitionOrder[i];
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (!itemDef)
                    continue;
                // スクラップアイテムの除去
                if (itemDef.ContainsTag(ItemTag.Scrap))
                    continue;
                if (!itemDef.canRemove || itemDef.hidden)
                    continue;
                // 使用済みアイテムはNoTier
                if (itemDef.tier == ItemTier.NoTier || itemDef.tier == ItemTier.Lunar)
                    continue;
                // bossアイテムは追加しない
                if (itemDef.tier == ItemTier.Boss || itemDef.tier == ItemTier.VoidBoss)
                    continue;
                // Tier1とTier2のアイテムを数える
                // ここでは種類を数えれば良いので、個数はいらない
                //int num = inventory.GetItemCount(itemIndex);
                if (itemDef.tier == ItemTier.Tier1 || itemDef.tier == ItemTier.VoidTier1)
                    tier1Count += 1;
                if (itemDef.tier == ItemTier.Tier2 || itemDef.tier == ItemTier.VoidTier2)
                    tier2Count += 1;
            }

            return new int[]{ tier1Count, tier2Count };
        }

        private void Hook_RunStart()
        {
            On.RoR2.Run.Start += (orig, self) =>
            {
                Logger.LogInfo("Run start");
                // RoR2Contentの中身は、Run.Startの後でないと読み込めない
                // AwakeやStartでもnullになってしまう
                scrapWhite = RoR2Content.Items.ScrapWhite;
                scrapGreen = RoR2Content.Items.ScrapGreen;
                ReloadConfig();
                // マルチプレイのときは最初のOninventoryChangeのチャットが流れない
                // 手動で流してやる
                StartCoroutine(ShowItemCountOnRunStart(3));
                //var chatbox = ChatPrefab.GetComponent<RoR2.UI.ChatBox>();
                // 秒単位。60s*60m*24hで最長1日表示
                //chatbox.fadeTimer = 60f * 60f * 24f;
                // こいつらはRunごとにリセットが必要
                ambientLevelFloor = 1;

                orig(self);
            };
        }

        IEnumerator ShowItemCountOnRunStart(float s)
        {
            yield return new WaitForSeconds(s);
            SendItemCountMessage(0, 0);
        }

        private void Hook_Spawns()
        {
            On.RoR2.SceneDirector.PopulateScene += (orig, self) =>
            {
                self.interactableCredit = (int)(self.interactableCredit * ObjectSumMultiply.Value);
                orig(self);
            };
            On.RoR2.SceneDirector.GenerateInteractableCardSelection += SceneDirector_GenerateInteractableCardSelection;
        }

        private WeightedSelection<DirectorCard> SceneDirector_GenerateInteractableCardSelection(On.RoR2.SceneDirector.orig_GenerateInteractableCardSelection orig, SceneDirector self)
        {
            var weightedSelection = orig(self);
            for (var i = 0; i < weightedSelection.Count; i++)
            {
                var choiceInfo = weightedSelection.GetChoice(i);
                //var prefabName = choiceInfo.value.spawnCard.prefab.name;
                SpawnCard spawnCard = choiceInfo.value.spawnCard;

                // MultishopOnly を参考にしてる
                if (IsChest(spawnCard))
                {
                    weightedSelection.ModifyChoiceWeight(i, choiceInfo.weight * 2f);
                }
                else if (IsLargeChest(spawnCard))
                {
                    weightedSelection.ModifyChoiceWeight(i, choiceInfo.weight * 2f);
                }
                else if (IsMultiShop(spawnCard))
                {
                    weightedSelection.ModifyChoiceWeight(i, choiceInfo.weight * MultiShopSpawnChance.Value);
                }
                else if (IsScrapper(spawnCard))
                {
                    weightedSelection.ModifyChoiceWeight(i, choiceInfo.weight * ScrapperSpawnChance.Value);
                }
                else if (IsPrinter(spawnCard))
                {
                    weightedSelection.ModifyChoiceWeight(i, choiceInfo.weight * PrinterSpawnChance.Value);
                }
                else if (IsChanceShrine(spawnCard))
                {
                    weightedSelection.ModifyChoiceWeight(i, choiceInfo.weight * ChanceShrineSpawnChance.Value);
                }
                else if (IsVoidPod(spawnCard))
                {
                    weightedSelection.ModifyChoiceWeight(i, choiceInfo.weight * VoidItemPodSpawnChance.Value);
                }
            }
            return weightedSelection;
        }

        private bool IsVoidPod(SpawnCard spawnCard)
        {
            string a = spawnCard.name.ToLower();
            return a == "iscVoidChest".ToLower();
        }

        private bool IsMultiShop(SpawnCard spawnCard)
        {
            string a = spawnCard.name.ToLower();
            return a == DirectorAPI.Helpers.InteractableNames.MultiShopCommon.ToLower() || a == DirectorAPI.Helpers.InteractableNames.MultiShopUncommon.ToLower();
        }

        private bool IsChest(SpawnCard spawnCard)
        {
            string a = spawnCard.name.ToLower();
            return a == DirectorAPI.Helpers.InteractableNames.BasicChest.ToLower();
        }
        private bool IsLargeChest(SpawnCard spawnCard)
        {
            string a = spawnCard.name.ToLower();
            return a == DirectorAPI.Helpers.InteractableNames.LargeChest.ToLower();
        }

        private bool IsScrapper(SpawnCard spawnCard)
        {
            string a = spawnCard.name.ToLower();
            return a == "iscScrapper".ToLower();
        }

        private bool IsPrinter(SpawnCard spawnCard)
        {
            string a = spawnCard.name.ToLower();
            return a == DirectorAPI.Helpers.InteractableNames.PrinterCommon.ToLower() || a == DirectorAPI.Helpers.InteractableNames.PrinterUncommon.ToLower() || a == DirectorAPI.Helpers.InteractableNames.PrinterLegendary.ToLower();
        }

        private bool IsChanceShrine(SpawnCard spawnCard)
        {
            string a = spawnCard.name.ToLower();
            return a == DirectorAPI.Helpers.InteractableNames.ChanceShrine.ToLower();
        }

        private static void Hook_HPDefine()
        {
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {

                // デフォルトの難易度だったら、何もしない
                // デフォルト難易度は0~, Invalidが-1, 追加難易度は-2から負に向かう
                if ((int)Run.instance.selectedDifficulty >= -1)
                {
                    orig(self);
                    return;
                }

                // レベルアップでのHP増加量の調整
                if (self.isPlayerControlled)
                {
                    // デフォルトは全部* 0.3f
                    self.levelMaxHealth = Mathf.Round(self.baseMaxHealth * 0.3f * 1.5f);

                    // levelMaxShieldはそもそも0なので、0のままにする
                    //self.levelMaxShield = Mathf.Round(self.baseMaxShield * 0.3f * 1.5f);       
                }
                orig(self);
            };
        }

        private void HookHeal()
        {
            On.RoR2.HealthComponent.OnInventoryChanged += (orig, self) =>
            {
                // healPerSecondが1なら、もとの挙動のまま
                if (syncConfig.healPerSecond == 1f)
                {
                    orig(self);
                    return;
                }
                // repeatHealComponentの付け外しを除去
                // それ以外の部分を移植
                self.itemCounts = default(HealthComponent.ItemCounts);
                Inventory inventory = self.body.inventory;
                self.itemCounts = (inventory ? new HealthComponent.ItemCounts(inventory) : default(HealthComponent.ItemCounts));
                self.currentEquipmentIndex = (inventory ? inventory.currentEquipmentIndex : EquipmentIndex.None);
            };

            On.RoR2.HealthComponent.Heal += (orig, self, amount, procChainMask, nonRegen) =>
            {
                // Void Fungus = MushroomVoid, id 119
                // Void Fungusはきちんと経時ヒールの対象になってて、10%制限に引っかかってるから良し！
                if (!NetworkServer.active)
                {
                    return 0f;
                }

                // 追加難易度かつnonRegenかつRepeatHealでない場合
                // デフォルト難易度は0~, Invalidが-1, 追加難易度は-2から負に向かう

                // 回復量の調節
                // すでにDifficultyAPIでcountAsHardModeにしてあるから、Regenerationは*0.6倍になってる
                // そこでRegen以外を調節する

                // if (difficultyDef.countAshardMode)
                // characterMaster.inventory.GiveItem(RoR2Content.Items.MonsoonPlayerHelper, 1);
                // MonsoonPlayerHelper>1のとき、RecalculateStatsでRegenを*0.6
                if (nonRegen && (int)Run.instance.selectedDifficulty <= -2 && !procChainMask.HasProc(ProcType.RepeatHeal))
                {
                    amount *= HealMultiply.Value;
                }

                // healPerSecondが1のときはもとの挙動のまま
                if (syncConfig.healPerSecond == 1f)
                {
                    return orig(self, amount, procChainMask, nonRegen);
                }

                // コープスブルーム(repeatHeal)の経時回復機能を使う
                // repeatHealComponentがついてなかったらつける
                if (self.body.teamComponent.teamIndex == TeamIndex.Player && self.repeatHealComponent == null)
                {
                    self.repeatHealComponent = base.gameObject.AddComponent<HealthComponent.RepeatHealComponent>();
                    self.repeatHealComponent.healthComponent = self;
                }
                // もとのHeal()から転記。回復しちゃいけないやつの除外
                if (!self.alive || amount <= 0f || self.body.HasBuff(RoR2Content.Buffs.HealingDisabled))
                {
                    return 0f;
                }
                float num = self.health;
                bool flag = false;
                // 何か不明だが、回復してないのでそのまま残す
                if (self.currentEquipmentIndex == RoR2Content.Equipment.LunarPotion.equipmentIndex && !procChainMask.HasProc(ProcType.LunarPotionActivation))
                {
                    self.potionReserve += amount;
                    return amount;
                }

                // RepeatHealじゃない場合
                // repeatHeal用に後で使うために保存する
                // この中では、条件分岐せずに必ずreturn 0f;で終わらせたい。
                if (nonRegen && self.repeatHealComponent && !procChainMask.HasProc(ProcType.RepeatHeal))
                {
                    // repeatHealはProcType.CritHealを持ってないので、ここで加工する
                    if (nonRegen && !procChainMask.HasProc(ProcType.CritHeal) && Util.CheckRoll(self.body.critHeal, self.body.master))
                    {
                        procChainMask.AddProc(ProcType.CritHeal);
                        flag = true;
                    }
                    if (flag)
                    {
                        amount *= 2f;
                    }
                    if (flag)
                    {
                        GlobalEventManager.instance.OnCrit(self.body, null, self.body.master, amount / self.fullHealth * 10f, procChainMask);
                    }

                    // FixedUpdateからのHealはprocChainMaskがRepeatHealになってる
                    //if (nonRegen && this.repeatHealComponent && !procChainMask.HasProc(ProcType.RepeatHeal))
                    // HealperSecond
                    self.repeatHealComponent.healthFractionToRestorePerSecond = syncConfig.healPerSecond / (1f + (float)self.itemCounts.repeatHeal);
                    self.repeatHealComponent.AddReserve(amount * (float)(1 + self.itemCounts.repeatHeal), self.fullHealth * 2f);
                    return 0f;
                }

                // repeatHeal()はorigに任せる
                // onCharacterHealServerのイベントがorigからしか呼べないので、origを呼び出さないといけない
                return orig(self, amount, procChainMask, nonRegen);
            };
        }

        private void HookMonsterSpawn()
        {
            On.RoR2.CombatDirector.AttemptSpawnOnTarget += (orig, self, spawnTarget, placementMode) =>
            {
                // この関数はspawnできるか全モンスターをチェックしてself.spawn()を叩いてる
                bool isSpawned = orig(self, spawnTarget, placementMode);

                // デフォルトの難易度だったら、何もしない
                // デフォルト難易度は0~, Invalidが-1, 追加難易度は-2から負に向かう
                // ただしMonsoonのみspawn数を調整する
                if ((int)Run.instance.selectedDifficulty >= -1 && Run.instance.selectedDifficulty != DifficultyIndex.Hard)
                {
                    return isSpawned;
                }
                if (isSpawned == true)
                {
                    if (this.ambientLevelFloor <= 3)
                    {
                        // origの中でthis.monsterCredit -= (float)self.currentMonsterCard.cost;される
                        // costを戻してやる
                        // spawn * 1.25
                        self.monsterCredit += (float)self.currentMonsterCard.cost * (1f / 4f);
                    }
                    // 15レベル以降は増やしすぎるときつい
                    else if (this.ambientLevelFloor <= 15)
                    {
                        // spawn * 1.25
                        //self.monsterCredit += (float)self.currentMonsterCard.cost * (1f / 4f);
                    }
                }
                return isSpawned;
            };

            On.RoR2.Run.OnAmbientLevelUp += (orig, self) =>
            {
                // 現在の敵のレベルかな
                Logger.LogInfo(self.ambientLevelFloor);
                this.ambientLevelFloor = self.ambientLevelFloor;
                orig(self);
            };
        }

        private void HookMoneyScaling()
        {
            On.RoR2.Run.GetDifficultyScaledCost_int_float += (orig, self, baseCost, difficultyCoefficient) =>
            {
                //return (int)((float)baseCost * Mathf.Pow(difficultyCoefficient, 1.25f));
                return (int)((float)baseCost * Mathf.Pow(difficultyCoefficient, syncConfig.moneyScaling));
            };
        }

        private static void HooksForLog()
        {
            //On.RoR2.InfiniteTowerWaveController.DropRewards += (orig, self) =>
            //{
            //    GameObject prefab = self.rewardPickupPrefab;
            //    orig(self);
            //};
        }

        [Server]
        private void BindConfig()
        {
            // ここはソロプレイでも呼ばれるので気をつける
            PossessedItemChance = Config.Bind("Chance", "Possessed Item Chance", 0.75f, 
                new ConfigDescription("The probability that your owned item is added to the item picker's item candidates."));
            OspPercent = Config.Bind("OSP", "OSP Threshold", 0.8f, 
                new ConfigDescription("Max receive damage / Max HP. Vanilla is 0.9"));
            InvTime = Config.Bind("OSP", "Invulnerable Time", 0.5f, 
                new ConfigDescription("The amount of time a player remains invulnerable after one shot protection is triggered. Vanilla is 0.1."));
            MoneyScaling = Config.Bind("Scaling", "Money Scaling", 1.45f,
                new ConfigDescription("How much money needed for opening chests. Normal 1.25. Code: `baseCost * Mathf.Pow(difficultyCoefficient, moneyScaling)`"));
            HealPerSecond = Config.Bind("Stats", "Max Heal per second", 1f,
                new ConfigDescription("Max Heal per second. Store overflow to next seconds. Store limit is 200% HP. Enter 1.0 to return to the original behavior."));

            ObjectSumMultiply = Config.Bind("Spawn", "Map Object Spawn Amount", 1f,
                new ConfigDescription("Multiply the total of all map objects by this. 1 is Original amount. 2 is *2 amount."));
            MultiShopSpawnChance = Config.Bind("Spawn", "MultiShop spawn chance", 0.0f,
                new ConfigDescription("Multiply the spawn weight of MultiShop. 0 is None. 1 is Original weight"));
            ScrapperSpawnChance = Config.Bind("Spawn", "Scrapper spawn chance", 0.0f,
                new ConfigDescription("Multiply the spawn weight of Scrapper. 0 is None. 1 is Original weight"));
            PrinterSpawnChance = Config.Bind("Spawn", "3D Printer spawn chance", 0.0f,
                new ConfigDescription("Multiply the spawn weight of 3D Printer. 0 is None. 1 is Original weight"));
            ChanceShrineSpawnChance = Config.Bind("Spawn", "LuckShrine spawn chance", 0.5f,
                new ConfigDescription("Multiply the spawn weight of LuckShrine. 0 is None. 1 is Original weight"));
            VoidItemPodSpawnChance = Config.Bind("Spawn", "VoidItem Pod spawn chance", 0.5f,
                new ConfigDescription("Multiply the spawn weight of VoidItem Pod. 0 is None. 1 is Original weight"));

            HealMultiply = Config.Bind("Stats", "Healing amount modify", 0.6f,
                new ConfigDescription("Multiply the amount of healing. If you enter 0.6, amount of all heal excluding regen will be 60%. Only valid for additional difficulty"));

            ItemPickerOptionAmount = Config.Bind("Item", "Option amount of ItemPicker", 2,
                new ConfigDescription("How many candidates are displayed when opeingItemPicker orb spawned from chests."));
            RandomItemAddPoolCount = Config.Bind("Item", "Random item amount add to Lottery pool", 1,
                new ConfigDescription("How many random items are added to Lottery pool. Between 1.0~5.0"));
            WhiteItemUpperLimit = Config.Bind("Item", "White item upper limit", 5,
                new ConfigDescription("You can't get new kind of white items when reach this limit. Like Vampire Survivors. You can only get the type of white items you already have."));
            GreenItemUpperLimit = Config.Bind("Item", "Green item upper limit", 3,
                new ConfigDescription("You can't get new kind of green items when reach this limit. Like Vampire Survivors. You can only get the type of green items you already have."));

            ReloadConfig();
        }

        private void ReloadConfig()
        {
            // ここはソロプレイでも呼ばれるので気をつける
            Config.Reload();

            // Clientに送信
            if (NetworkServer.active)
            {
                // hostなら自分でインスタンス化する
                // hostでないならSyncConfig.OnReceivedでRoVクラスからの参照を設定する
                syncConfig = new SyncConfig(PossessedItemChance.Value, OspPercent.Value, InvTime.Value, MoneyScaling.Value, HealPerSecond.Value, ItemPickerOptionAmount.Value, WhiteItemUpperLimit.Value, GreenItemUpperLimit.Value, RandomItemAddPoolCount.Value); 
                syncConfig.Send(NetworkDestination.Clients);
            }
        }

        private void HookOSP()
        {
            // OSPの無敵時間を設定
            On.RoR2.HealthComponent.TriggerOneShotProtection += (orig, self) =>
            {
                if (!NetworkServer.active)
                {
                    return;
                }
                orig(self);
                self.ospTimer = syncConfig.invTime;
                Logger.LogInfo(self.ospTimer);
            };

            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                // ospFractionはHP満タンから最大ダメージを受けたときに残る量
                // これ以上のダメージを受けたときは、ospTimer(1秒)の間だけ無敵になる
                // orig()の中でospFraction = 0.1f;に設定後、書き換えられてる。
                // this.oneShotProtectionFraction = Mathf.Max(0f, this.oneShotProtectionFraction - (1f - 1f / this.cursePenalty));
                orig(self);

                // もっと高い値に設定し直す
                // 1 - 0.9 = 0.1
                // 1 - 0.4 = 0.6
                float newOspFraction = 1f - syncConfig.ospPercent;
                // もとがif(NetworkServer.active)の外側なので、ifいらない
                self.oneShotProtectionFraction = Mathf.Max(0f, newOspFraction - (1f - 1f / self.cursePenalty));
            };
        }

        private void LoadAssets()
        {
            // pathは途中のinteractablesやitemsを抜いたものになる
            // 実際どこがいらないのかはよくわからん
            OptionPickup = Addressables.LoadAssetAsync<GameObject>(key: "RoR2/DLC1/OptionPickup/OptionPickup.prefab")
                .WaitForCompletion();
            //Logger.LogInfo(this.optionPickup);
            MonsoonIcon = Addressables.LoadAssetAsync<Sprite>(key: "RoR2/Junk/Common/texDifficulty2.png")
                .WaitForCompletion();
            ChatPrefab = Addressables.LoadAssetAsync<GameObject>(key: "RoR2/Base/UI/ChatBox, In Run.prefab")
                .WaitForCompletion();

            Logger.LogInfo(MonsoonIcon);
        }

        private void Hook_ChestBehavior()
        {
            On.RoR2.ChestBehavior.Open += (orig, self) =>
            {
                //Logger.LogInfo("open");
                orig(self);
            };

            On.RoR2.ChestBehavior.ItemDrop += (orig, self) =>
            {
                if (!NetworkServer.active)
                {
                    return;
                }
                if (!self.gameObject.name.Contains("Chest"))
                {
                    orig(self);
                    return;
                }
                if (self.gameObject.name.Contains("Lunar") || self.gameObject.name.Contains("Void"))
                {
                    orig(self);
                    return;
                }
                if (self.gameObject.name.Contains("Equipment"))
                {
                    orig(self);
                    return;
                }
                if (self.dropPickup == RoR2.PickupIndex.none || self.dropCount < 1)
                {
                    return;
                }
                float angle = 360f / (float)self.dropCount;
                Vector3 vector = Vector3.up * self.dropUpVelocityStrength + self.dropTransform.forward * self.dropForwardVelocityStrength;
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                for (int i = 0; i < self.dropCount; i++)
                {
                    // dropTableからの最初の候補の生成
                    //PickupPickerController.Option firstOption = PickupPickerController.GenerateOptionsFromDropTable(1, self.dropTable, self.rng)[0];
                    ItemTier firstTier = self.dropPickup.pickupDef.itemTier;
                    Run run = Run.instance;
                    // 最初がTier1だったらTier1のみから抽選
                    ItemTier lowestItemTier = firstTier;
                    BasicPickupDropTable newDropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
                    newDropTable.selector.Clear();
                    // weightedSelectionへアクセスするため
                    BasicPickupDropTable basicDropTable = self.dropTable as BasicPickupDropTable;
                    int availableChoiceCount = 0;
                    if (basicDropTable == null)
                    {
                        // キャストに失敗した場合、自分で構築する
                        if (firstTier == ItemTier.Tier1)
                        {
                            newDropTable.Add(run.availableTier1DropList, 1f);
                            availableChoiceCount = run.availableTier1DropList.Count;
                        }
                        else if (firstTier == ItemTier.Tier2)
                        {
                            newDropTable.Add(run.availableTier2DropList, 1f);
                            availableChoiceCount = run.availableTier2DropList.Count;
                        }
                        else if (firstTier == ItemTier.Tier3)
                        {
                            newDropTable.Add(run.availableTier3DropList, 1f);
                            availableChoiceCount = run.availableTier3DropList.Count;
                        }
                    }
                    else
                    {
                        // キャストに成功した場合、継承する
                        // この方式なら、回復箱・攻撃箱に対応できる
                        // 参照すると元の確率を壊してしまうかもなのでダメ
                        //newDropTable.selector = basicDropTable.selector;
                        int choiceIndex = 0;
                        foreach(var choice in basicDropTable.selector.choices)
                        {
                            // 最初のTierと一致しないものは選ばれないようにする
                            if (choice.value.pickupDef.itemTier != firstTier)
                            {
                                // 重みでの制御が効かないので、そもそも選択肢を追加しない
                                //newDropTable.selector.AddChoice(choice.value, 0.0001f);
                            }
                            else
                            {
                                newDropTable.selector.AddChoice(choice.value, 1f);
                                availableChoiceCount++;
                            }
                            choiceIndex++;
                        }
                    }
                    // 最初の抽選は含めず、新しいものを5つ生成
                    // これで被りが出ず、必ず5つにできる
                    // GenerateUniqueDropsは被り無しで生成
                    // 5以下の選択肢の時があるのでminを取る
                    int choiceCount = Math.Min(availableChoiceCount, 5);
                    //PickupIndex[] pickupIndices = newDropTable.GenerateUniqueDropsPreReplacement(choiceCount, self.rng);
                    //int j = 0;
                    PickupPickerController.Option[] newOptions = PickupPickerController.GenerateOptionsFromDropTable(choiceCount, newDropTable, self.rng);
                    //foreach(PickupIndex pickupIndex in pickupIndices)
                    //{
                    //    newOptions[j] = new PickupPickerController.Option
                    //    {
                    //        available = true,
                    //        pickupIndex = pickupIndex
                    //    };
                    //    j++;
                    //}

                    PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
                    {
                        pickupIndex = PickupCatalog.FindPickupIndex(lowestItemTier),
                        pickerOptions = newOptions,
                        //pickerOptions = PickupPickerController.SetOptionsFromInteractor(),
                        rotation = Quaternion.identity,
                        prefabOverride = OptionPickup
                    }, self.dropTransform.position + Vector3.up * 1.5f, vector);
                    vector = rotation * vector;
                    self.Roll();
                }
                self.dropPickup = PickupIndex.none;
            };
        }

        private bool[] IsReachLimits(Inventory inventory)
        {
            int[] counts = ItemRecount(inventory);
            int tier1Count = counts[0];
            int tier2Count = counts[1];

            bool isTier1ReachLimit = tier1Count >= syncConfig.whiteItemUpperLimit;
            bool isTier2ReachLimit = tier2Count >= syncConfig.greenItemUpperLimit;

            return new bool[] { isTier1ReachLimit, isTier2ReachLimit };
        }

        private void Hook_PickupPicker_OnInteractionBegin()
        {
            // そもそもこの関数がサーバー側でしか呼びだされない
            // PickupPickerControllerがNetworkBehaviorだからか？
            On.RoR2.PickupPickerController.OnInteractionBegin += (orig, self, activator) =>
            {
                // SetOptionsFromInteractor()から内容をコピーした
                //Logger.LogInfo(activator);
                // ここからエラーよけ
                if (self.gameObject.name.Contains("Scrapper"))
                {
                    orig(self, activator);
                    return;
                }
                if (self.gameObject.name != "OptionPickup(Clone)")
                {
                    orig(self, activator);
                    return;
                }
                if (!activator)
                {
                    Debug.Log("No activator.");
                    orig(self, activator);
                    return;
                }
                CharacterBody component = activator.GetComponent<CharacterBody>();
                if (!component)
                {
                    Debug.Log("No body.");
                    orig(self, activator);
                    return;
                }
                Inventory inventory = component.inventory;
                if (!inventory)
                {
                    Debug.Log("No inventory.");
                    orig(self, activator);
                    return;
                }
                // ここまでエラーよけ

                PickupIndex whiteScrapIndex = PickupCatalog.FindPickupIndex(scrapWhite.itemIndex);
                PickupIndex greenScrapIndex = PickupCatalog.FindPickupIndex(scrapGreen.itemIndex);
                // 上限設定のため、Itemcountを更新する
                var isLimits = IsReachLimits(inventory);
                bool isTier1ReachLimit = isLimits[0];
                bool isTier2ReachLimit = isLimits[1];
                
                // 抽選開始
                // whiteScrapが入っていればRolledとみなす
                // 入っていない場合
                if (!Array.Exists(self.options, option => option.pickupIndex == whiteScrapIndex))
                {
                    // 最小Tierの判定
                    //ItemTier lowestItemTier = self.options[0].pickupIndex.pickupDef.itemTier;
                    ItemTier lowestItemTier = ItemTier.Tier3;
                    foreach (var option in self.options)
                    {
                        if (option.pickupIndex.pickupDef.itemTier == ItemTier.Tier1)
                        {
                            lowestItemTier = ItemTier.Tier1;
                            break;
                        }
                        else if (option.pickupIndex.pickupDef.itemTier == ItemTier.Tier2)
                        {
                            if (lowestItemTier == ItemTier.Tier3)
                            {
                                lowestItemTier = ItemTier.Tier2;
                            }
                        }
                    }
                    // すべてvoidTierだったら
                    if (self.options.All(option => option.pickupIndex.pickupDef.itemTier == ItemTier.VoidTier1
                        || option.pickupIndex.pickupDef.itemTier == ItemTier.VoidTier2
                        || option.pickupIndex.pickupDef.itemTier == ItemTier.VoidTier3))
                    {
                        lowestItemTier = ItemTier.VoidTier1;
                    }


                    // 作成するOption list
                    HashSet<PickupPickerController.Option> list = new();

                    // item historyから追加
                    // itemAcquisitionOrderは被りなし、数を保持しない
                    // 数はinventory.GetItemCount(ItemIndex)で調べる
                    for (int i = 0; i < inventory.itemAcquisitionOrder.Count; i++)
                    {
                        ItemIndex itemIndex = inventory.itemAcquisitionOrder[i];
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                        ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                        PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                        // canScrapのみにすると、void itemが選ばれなくなる
                        if (!itemDef)
                            continue;
                        // スクラップアイテムの除去
                        if (itemDef.ContainsTag(ItemTag.Scrap))
                            continue;
                        if (!itemDef.canRemove || itemDef.hidden)
                            continue;
                        // 使用済みアイテムはNoTier
                        if (itemDef.tier == ItemTier.NoTier || itemDef.tier == ItemTier.Lunar)
                            continue;
                        // bossアイテムは追加しない
                        if (itemDef.tier == ItemTier.Boss || itemDef.tier == ItemTier.VoidBoss)
                            continue;
                        // すべてがVoidアイテムの場合
                        if (lowestItemTier == ItemTier.VoidTier1){
                            if (itemDef.tier == ItemTier.VoidTier1 || itemDef.tier == ItemTier.VoidTier2
                                || itemDef.tier == ItemTier.VoidTier3)
                            {
                                // pass
                            }
                            else
                            {
                                continue;
                            }
                            // 確率で弾く
                            if (syncConfig.possessedItemChance < UnityEngine.Random.value)
                            {
                                continue;
                            }
                        }
                        // 最小がTier3の場合
                        else if (lowestItemTier == ItemTier.Tier3)
                        {
                            if (itemDef.tier == ItemTier.Tier3 || itemDef.tier == ItemTier.VoidTier3)
                            {
                                // 確率で弾かず、Tier3以上の全アイテム追加
                                // ボスアイテムはテレポーターから出る分で十分
                                // pass
                            }
                            else
                            {
                                continue;
                            }
                            if (syncConfig.possessedItemChance < UnityEngine.Random.value)
                            {
                                continue;
                            }
                        }
                        // 最小がTier2の場合
                        else if (lowestItemTier == ItemTier.Tier2)
                        {
                            if (itemDef.tier == ItemTier.Tier2 || itemDef.tier == ItemTier.VoidTier2)
                            {
                                // pass
                            }
                            else
                            {
                                continue;
                            }
                            // tier2がまだ埋まっていない場合、抽選で候補に追加しない
                            // 逆にtier2が埋まっている場合は、全て追加する
                            if (!isTier2ReachLimit && syncConfig.possessedItemChance < UnityEngine.Random.value)
                            {
                                continue;
                            }
                            // Tier3以上は確率で弾く
                            //if (itemDef.tier == ItemTier.Tier3 || itemDef.tier == ItemTier.VoidTier3
                            //    || itemDef.tier == ItemTier.Boss || itemDef.tier == ItemTier.VoidBoss)
                            //{
                            //    if (UnityEngine.Random.value > 1f / 10f)
                            //        continue;
                            //}
                        }
                        // 最小がTier1の場合
                        else if (lowestItemTier == ItemTier.Tier1)
                        {
                            if (itemDef.tier == ItemTier.Tier1 || itemDef.tier == ItemTier.VoidTier1)
                            {
                                // pass
                            }
                            else { continue; }

                            // tier1がまだ埋まっていない場合、抽選で候補に追加しない
                            // 逆にtier1が埋まっている場合は、全て追加する
                            if (!isTier1ReachLimit && syncConfig.possessedItemChance < UnityEngine.Random.value)
                            {
                                continue;
                            }
                            // Tier1宝箱は配送要求表と同じドロップ率らしい
                            // 配送要求表は79/20/1%
                            //if (itemDef.tier == ItemTier.Tier2 || itemDef.tier == ItemTier.VoidTier2)
                            //{
                            //    if (UnityEngine.Random.value > 1f / 5f)
                            //        continue;
                            //}
                            //else if (itemDef.tier == ItemTier.Tier3 || itemDef.tier == ItemTier.VoidTier3
                            //    || itemDef.tier == ItemTier.Boss || itemDef.tier == ItemTier.VoidBoss)
                            //{
                            //    if (UnityEngine.Random.value > 1f / 100f)
                            //        continue;
                            //}
                        }
                        // 条件を生き残ったやつを候補に追加する
                        list.Add(new PickupPickerController.Option
                        {
                            available = true,
                            pickupIndex = pickupIndex
                        });
                    }

                    // ランダムアイテムを加える
                    int addItemCount = syncConfig.randomItemAddPoolCount;
                    // 上限は5
                    if (addItemCount > 5)
                        addItemCount = 5;
                    // 下限は1
                    if (addItemCount < 1)
                        addItemCount = 1;
                    // 候補は少なくとも4を確保する
                    if (list.Count + addItemCount < 4)
                        addItemCount = 4 - list.Count;
                    // self.optionsの数以上を追加しないように
                    addItemCount = Math.Min(addItemCount, self.options.Length);

                    for (var i = 0; i < addItemCount; i++)
                    {
                        var option = self.options[i];
                        var itemTier = option.pickupIndex.pickupDef.itemTier;
                        if (itemTier == ItemTier.Tier1)
                        {
                            if (!isTier1ReachLimit)
                            {
                                list.Add(self.options[i]);
                            }
                            continue;
                        }
                        else if (itemTier == ItemTier.Tier2)
                        {
                            if (!isTier2ReachLimit)
                            {
                                list.Add(self.options[i]);
                            }
                            continue;
                        }
                        else
                        {
                            // Tier1,2以外
                            list.Add(self.options[i]);
                        }
                    }
                    // shuffleして先頭nつだけ残す
                    List<PickupPickerController.Option> rolledList = list.ToList().OrderBy(x => UnityEngine.Random.value)
                        .Take(syncConfig.itemPickerOptionAmount).ToList();

                    ItemLockandAddScrap(self, inventory, rolledList, isTier1ReachLimit, isTier2ReachLimit);
                    //self.contextString = "Rolled";
                }
                else
                {
                    // すでにrolledだった場合
                    var list = self.options.ToList<PickupPickerController.Option>();
                    ItemLockandAddScrap(self, inventory, list, isTier1ReachLimit, isTier2ReachLimit);
                }
                // 先にoptionsの中身を変えてから、もとのOnInteractionBeginを呼び出す。
                orig(self, activator);
            };
        }

        private void ItemLockandAddScrap(PickupPickerController self, Inventory inventory, List<PickupPickerController.Option> list, bool isTier1ReachLimit, bool isTier2ReachLimit)
        {
            PickupIndex whiteScrapIndex = PickupCatalog.FindPickupIndex(scrapWhite.itemIndex);
            PickupIndex greenScrapIndex = PickupCatalog.FindPickupIndex(scrapGreen.itemIndex);
            bool containOwnedItem = false;
            ItemTier lowestItemTier = ItemTier.Tier3;
            // スクラップは一旦全部除去
            list.RemoveAll(x => x.pickupIndex == whiteScrapIndex || x.pickupIndex == greenScrapIndex);
            // 一旦全部available=trueにして開放する
            for(var i = 0; i< list.Count; i++)
            {
                list[i] = new PickupPickerController.Option
                {
                    available = true,
                    pickupIndex = list[i].pickupIndex
                };
            }

            // 所持アイテムでなく、アイテム上限だったらavailable=falseにする
            for (var i = 0; i < list.Count; i++)
            {
                // 一つ以上保有するアイテムを含むか
                int inventoryCount = inventory.GetItemCount(list[i].pickupIndex.pickupDef.itemIndex);
                if (inventoryCount >= 1)
                    containOwnedItem = true;
                // 最小Tierはいくつか
                var itemTier = list[i].pickupIndex.pickupDef.itemTier;
                if (itemTier == ItemTier.Tier1)
                {
                    lowestItemTier = ItemTier.Tier1;
                }
                else if (itemTier == ItemTier.Tier2)
                {
                    if (lowestItemTier == ItemTier.Tier3)
                    {
                        lowestItemTier = ItemTier.Tier2;
                    }
                }
                // 持っていない、かつ各TierがLimitに達していたらavailable=false
                if (inventoryCount < 1 && isTier1ReachLimit && (itemTier == ItemTier.Tier1 || itemTier == ItemTier.VoidTier1))
                {
                    // これだとエラーなので新規作成する必要がある
                    //list[i].available = false;
                    list[i] = new PickupPickerController.Option
                    {
                        available = false,
                        pickupIndex = list[i].pickupIndex
                    };
                }
                else if (inventoryCount < 1 && isTier2ReachLimit && (itemTier == ItemTier.Tier2 || itemTier == ItemTier.VoidTier2))
                {
                    list[i] = new PickupPickerController.Option
                    {
                        available = false,
                        pickupIndex = list[i].pickupIndex
                    };
                }
            }

            // Rolled判定のために常にwhiteScrapをavailable=false以上で追加
            // 所持アイテムがあるなら、それを取れるのでscrapはいらない
            var whiteScrapOption = new PickupPickerController.Option
            {
                available = false,
                pickupIndex = whiteScrapIndex
            };
            if (containOwnedItem == false)
            {
                if (lowestItemTier == ItemTier.Tier1)
                {
                    // whiteScrapのLockを外す
                    whiteScrapOption.available = true;
                }
                if (lowestItemTier != ItemTier.Tier1)
                {
                    // lowertItemTierがTier2以上なら、全部greenScrapを追加する
                    list.Add(new PickupPickerController.Option
                    {
                        available = true,
                        pickupIndex = greenScrapIndex
                    });
                }
            }
            list.Add(whiteScrapOption);
            // localメソッドっぽい
            // この中で実際にpickupOptionsのUIを書き換えてる
            // そもそもselfがNetworkBehaviorだが挙動がわからない
            self.SetOptionsServer(list.ToArray());
        }

        private void DefineDifficulties()
        {
            // make new Difficulties
            // Monsoonのスケーリング調節
            // default difficulty 1: 1f, 2: 2f, 3:3f
            // difficultyDefs[2]がmonsoon
            // DnSpyではreadonlyだが書き換えできる
            // Noral(2)基準の難易度計算 (x-2)/2

            DifficultyDef difficulty40Def;
            DifficultyDef difficulty45Def;
            DifficultyDef difficulty50Def;
            DifficultyDef difficulty55Def;
            DifficultyDef difficulty60Def;
            DifficultyDef difficulty65Def;
            DifficultyDef difficulty70Def;

            difficulty40Def = new(4f, "DestinyDifficulty_40_NAME", "Step13", "DestinyDifficulty_40_DESCRIPTION",
                ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin), "de", true);
            difficulty40Def.foundIconSprite = true;
            difficulty40Def.iconSprite = MonsoonIcon;
            DifficultyAPI.AddDifficulty(difficulty40Def);
            LanguageAPI.Add(difficulty40Def.nameToken, "Destiny 1");
            LanguageAPI.Add(difficulty40Def.descriptionToken, "<style=cStack>>Health Regeneration: <style=cIsHealth>-40%</style> \n>Difficulty Scaling: <style=cIsHealth>+100%</style></style>");

            difficulty45Def = new(4.5f, "DestinyDifficulty_45_NAME", "Step13", "DestinyDifficulty_45_DESCRIPTION",
                ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin), "de", true);
            difficulty45Def.foundIconSprite = true;
            difficulty45Def.iconSprite = MonsoonIcon;
            DifficultyAPI.AddDifficulty(difficulty45Def);
            LanguageAPI.Add(difficulty45Def.nameToken, "Destiny 2");
            LanguageAPI.Add(difficulty45Def.descriptionToken, "<style=cStack>>Health Regeneration: <style=cIsHealth>-40%</style> \n>Difficulty Scaling: <style=cIsHealth>+125%</style></style>");

            difficulty50Def = new(5f, "DestinyDifficulty_50_NAME", "Step13", "DestinyDifficulty_50_DESCRIPTION",
                ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin), "de", true);
            difficulty50Def.foundIconSprite = true;
            difficulty50Def.iconSprite = MonsoonIcon;
            DifficultyAPI.AddDifficulty(difficulty50Def);
            LanguageAPI.Add(difficulty50Def.nameToken, "Destiny 3");
            LanguageAPI.Add(difficulty50Def.descriptionToken, "<style=cStack>>Health Regeneration: <style=cIsHealth>-40%</style> \n>Difficulty Scaling: <style=cIsHealth>+150%</style></style>");

            difficulty55Def = new(5.5f, "DestinyDifficulty_55_NAME", "Step13", "DestinyDifficulty_55_DESCRIPTION",
                ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin), "de", true);
            difficulty55Def.foundIconSprite = true;
            difficulty55Def.iconSprite = MonsoonIcon;
            DifficultyAPI.AddDifficulty(difficulty55Def);
            LanguageAPI.Add(difficulty55Def.nameToken, "Insane 1");
            LanguageAPI.Add(difficulty55Def.descriptionToken, "<style=cStack>>Health Regeneration: <style=cIsHealth>-40%</style> \n>Difficulty Scaling: <style=cIsHealth>+175%</style></style>");

            difficulty60Def = new(6f, "DestinyDifficulty_60_NAME", "Step13", "DestinyDifficulty_60_DESCRIPTION",
                ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin), "de", true);
            difficulty60Def.foundIconSprite = true;
            difficulty60Def.iconSprite = MonsoonIcon;
            DifficultyAPI.AddDifficulty(difficulty60Def);
            LanguageAPI.Add(difficulty60Def.nameToken, "Insane 2");
            LanguageAPI.Add(difficulty60Def.descriptionToken, "<style=cStack>>Health Regeneration: <style=cIsHealth>-40%</style> \n>Difficulty Scaling: <style=cIsHealth>+200%</style></style>");

            difficulty65Def = new(6.5f, "DestinyDifficulty_65_NAME", "Step13", "DestinyDifficulty_65_DESCRIPTION",
                ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin), "de", true);
            difficulty65Def.foundIconSprite = true;
            difficulty65Def.iconSprite = MonsoonIcon;
            DifficultyAPI.AddDifficulty(difficulty65Def);
            LanguageAPI.Add(difficulty65Def.nameToken, "Impossible 1");
            LanguageAPI.Add(difficulty65Def.descriptionToken, "<style=cStack>>Health Regeneration: <style=cIsHealth>-40%</style> \n>Difficulty Scaling: <style=cIsHealth>+225%</style></style>");

            difficulty70Def = new(7f, "DestinyDifficulty_70_NAME", "Step13", "DestinyDifficulty_70_DESCRIPTION",
                ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarCoin), "de", true);
            difficulty70Def.foundIconSprite = true;
            difficulty70Def.iconSprite = MonsoonIcon;
            DifficultyAPI.AddDifficulty(difficulty70Def);
            LanguageAPI.Add(difficulty70Def.nameToken, "Impossible 2");
            LanguageAPI.Add(difficulty70Def.descriptionToken, "<style=cStack>>Health Regeneration: <style=cIsHealth>-40%</style> \n>Difficulty Scaling: <style=cIsHealth>+250%</style></style>");
        }



        //The Update() method is run on every frame of the game.
        private void Update()
        {
            //This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F5))
            {
                //Get the player body to use a position:	
                //var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                Log.LogInfo("Player pressed F5.");
                // configをreloadして反映させる
                ReloadConfig();

                Chat.AddMessage(new Chat.SimpleChatMessage
                {
                    baseToken = "Risk of Vampire> Config Reloaded."
                });

                Logger.LogInfo("Config Reload finished");


                //PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);


                //var catalogPath = "C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data/StreamingAssets/aa/catalog.json";
                //var bundlePath = "C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data/StreamingAssets/aa/StandaloneWindows64/ror2-base-parent_bin_assets_all";

                //var bundlePath = Application.streamingAssetsPath + "/aa/StandaloneWindows64/ror2-base-parent_bin_assets_all.bundle";

                //var loadedBundle = AssetBundle.LoadFromFile(bundlePath);


                //var prefab = Addressables.LoadAsset<GameObject>(bundlePath);

                //AsyncOperationHandle<IResourceLocator> catalog = Addressables.LoadContentCatalog(catalogPath);
                //catalog = Addressables.LoadContentCatalogAsync(catalogPath);


                //Addressables.LoadAssetAsync<GameObject>(catalogPath).Completed += gameObject =>
                //{
                //    //Logger.LogInfo(gameObject);
                //};


                //PickupDropletController.CreatePickupDroplet(
                //    new GenericPickupController.CreatePickupInfo
                //    {
                //        pickupIndex = PickupCatalog.FindPickupIndex(myItemDef.itemIndex),
                //        pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, 
                //            this.rewardDropTable, this.rng),
                //        rotation = Quaternion.identity,
                //    }
                //);

                //GameObject pickupMystery =  Resources.Load<GameObject>("Prefabs/PickupModels/PickupMystery");
                //GameObject[] pickupModels = Resources.LoadAll<GameObject>("");
                //foreach (var pickupModel in pickupModels)
                //{
                //    //Debug.Log(pickupModel.name);
                //}
                //Log.LogInfo(pickupModels);
            }
        }
    }
}

#pragma warning restore Publicizer001 // Accessing a member that was not originally public
