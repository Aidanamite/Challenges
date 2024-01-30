using HarmonyLib;
using SRML;
using SRML.Console;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SRML.SR.SaveSystem;
using SRML.SR.SaveSystem.Data;
using SRML.SR;
using SRML.Config.Attributes;
using System.Linq;
using SRML.SR.Patches;
using InControl;

namespace Challenges
{
    internal class Main : ModEntryPoint
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{System.Environment.CurrentDirectory}\\SRML\\Mods\\{modName}";
        internal static ChallengeManager challengeManager;
        internal static System.Random rand = new System.Random();
        internal static CorralUI uiPrefab;
        internal static bool Waiting = false;
        internal static int freePurchases = 0;
        internal static PlayerAction menu;
        internal static Console.ConsoleInstance ConsoleI;

        public override void PreLoad()
        {
            ConsoleI = ConsoleInstance;
            (menu = BindingRegistry.RegisterAction("key.challengeMenuOpen")).AddDefaultBinding(Key.P);
            TranslationPatcher.AddUITranslation("key.key.challengemenuopen", "Open Challenge Menu");
            HarmonyInstance.PatchAll();
            Console.RegisterCommand(new BlacklistCommand());
            uiPrefab = Resources.FindObjectsOfTypeAll<CorralUI>().First((x) => !x.name.EndsWith("(Clone)"));
            TranslationPatcher.AddUITranslation("challenge.no_data", "No challenge data was found on this save. Would you like to generate a challenge set now?");
            TranslationPatcher.AddUITranslation("challenge.no_challenges", "You have no challenges. Would you like to generate a challenge set now?");
            TranslationPatcher.AddUITranslation("t.challenges", "Challenges");
            TranslationPatcher.AddUITranslation("t.challenge_perms", "Allowed Challenges");
            TranslationPatcher.AddUITranslation("b.blank", "");
            TranslationPatcher.AddUITranslation("b.add", "Add");
            TranslationPatcher.AddUITranslation("b.remove", "Remove");
            TranslationPatcher.AddUITranslation("b.active", "Active");
            TranslationPatcher.AddUITranslation("b.inactive", "Inactive");
            TranslationPatcher.AddUITranslation("b.allowed", "Allowed");
            TranslationPatcher.AddUITranslation("b.disallowed", "Not Allowed");
            SaveRegistry.RegisterWorldDataLoadDelegate(LoadData);
            SaveRegistry.RegisterWorldDataSaveDelegate(WriteData);
            TranslationPatcher.AddPediaTranslation("c.name.infiniteEnergy", "Infinite Energy");
            TranslationPatcher.AddPediaTranslation("c.desc.infiniteEnergy", "Makes you never run out of energy");
            TranslationPatcher.AddPediaTranslation("c.name.1hp", "Max HP = 1");
            TranslationPatcher.AddPediaTranslation("c.desc.1hp", "Limits you to 1 HP");
            TranslationPatcher.AddPediaTranslation("c.name.vacCost", "Ineffecient Vaccing");
            TranslationPatcher.AddPediaTranslation("c.desc.vacCost", "Sucking and shooting with the vac gun now costs energy");
            TranslationPatcher.AddPediaTranslation("c.name.allFeral", "Aggressive");
            TranslationPatcher.AddPediaTranslation("c.desc.allFeral", "All newly spawned slimes and largos are feral");
            TranslationPatcher.AddPediaTranslation("c.name.bigSale", "Big Sale");
            TranslationPatcher.AddPediaTranslation("c.desc.bigSale", "First 3 purchases are free");
            TranslationPatcher.AddPediaTranslation("c.name.desert", "Baren Desert");
            TranslationPatcher.AddPediaTranslation("c.desc.desert", "All slime and animal spawns are greatly reduced");
            TranslationPatcher.AddPediaTranslation("c.name.fragile", "Fragile");
            TranslationPatcher.AddPediaTranslation("c.desc.fragile", "Everything takes double damage");
            TranslationPatcher.AddPediaTranslation("c.name.upgrade", "Upgraded");
            TranslationPatcher.AddPediaTranslation("c.desc.upgrade", "Start the game with 2 free upgrades");
            TranslationPatcher.AddPediaTranslation("c.name.weighted", "Weighted");
            TranslationPatcher.AddPediaTranslation("c.desc.weighted", "Movement speed is affected by how full your inventory is");
            TranslationPatcher.AddPediaTranslation("c.name.slide", "Icey Movement");
            TranslationPatcher.AddPediaTranslation("c.desc.slide", "Beatrix's friction is reduced to 10%");
            TranslationPatcher.AddPediaTranslation("c.name.thickSkin", "Thick Skinned");
            TranslationPatcher.AddPediaTranslation("c.desc.thickSkin", "Beatrix is immune is to collision damage");
            TranslationPatcher.AddPediaTranslation("c.name.noFav", "No Favourites");
            TranslationPatcher.AddPediaTranslation("c.desc.noFav", "Slimes do not produce extra plorts from eating their favourite foods");
            TranslationPatcher.AddPediaTranslation("c.name.delicate", "Delicate Slimes");
            TranslationPatcher.AddPediaTranslation("c.desc.delicate", "All slimes pop on contact with Beatrix");
            TranslationPatcher.AddPediaTranslation("c.name.lavaFloor", "The Floor is Lava");
            TranslationPatcher.AddPediaTranslation("c.desc.lavaFloor", "Start with a full water slot but take damage while touching the ground without being damp or having water in your vac");
            TranslationPatcher.AddPediaTranslation("c.name.marketRandom", "Unstable Market");
            TranslationPatcher.AddPediaTranslation("c.desc.marketRandom", "Greatly increased market variation. Both good and bad");
            SRCallbacks.OnMainMenuLoaded += (x) =>
            {
                Challenge.ActiveChallenges.Clear();
                TimeDirector.onUnpauseDelegate += () =>
                {
                    if (Waiting)
                    {
                        Waiting = false;
                        AskForChallenge();
                    }
                };
            };
            SRCallbacks.OnActorSpawn += (x, y, z) => {
                if (x == Identifiable.Id.PLAYER)
                {
                    y.AddComponent<DamageOnDryness>();
                    challengeManager = new GameObject("ChallengeManager", typeof(ChallengeManager)).GetComponent<ChallengeManager>();
                }
                if (Identifiable.IsSlime(x))
                {
                    if (!y.GetComponent<AttackPlayer>())
                    {
                        var c = y.AddComponent<AttackPlayer>();
                        c.damagePerAttack = 5;
                    }
                    if (!y.GetComponent<GotoPlayer>())
                    {
                        var c = y.AddComponent<GotoPlayer>();
                    }
                    if (!y.GetComponent<DestroyOnPlayer>())
                    {
                        var c = y.AddComponent<DestroyOnPlayer>();
                    }
                }
            };
        }
        public override void Load()
        {
            DestroyOnPlayer.destroyFX = GameContext.Instance.LookupDirector.GetPrefab(Identifiable.Id.WATER_LIQUID).GetComponent<DestroyOnTouching>().destroyFX;

            new Challenge("infiniteEnergy", "c.name.infiniteEnergy", "c.desc.infiniteEnergy", GameContext.Instance.LookupDirector.GetUpgradeDefinition(PlayerState.Upgrade.ENERGY_1).Icon, Challenge.ChallengeType.Good, () => SceneContext.Instance.PlayerState.SetEnergy(SceneContext.Instance.PlayerState.GetMaxEnergy()), null);
            new Challenge("1hp", "c.name.1hp", "c.desc.1hp", GameContext.Instance.LookupDirector.GetUpgradeDefinition(PlayerState.Upgrade.HEALTH_1).Icon, Challenge.ChallengeType.Bad, () => { SceneContext.Instance.PlayerState.model.maxHealth = 1; if (SceneContext.Instance.PlayerState.GetCurrHealth() > 1) SceneContext.Instance.PlayerState.SetHealth(1); },null);
            new Challenge("vacCost", "c.name.vacCost", "c.desc.vacCost", GameContext.Instance.LookupDirector.GetIcon(Identifiable.Id.VALLEY_AMMO_2), Challenge.ChallengeType.Bad, null,null);
            new Challenge("allFeral", "c.name.allFeral", "c.desc.allFeral", SceneContext.Instance.PediaDirector.entryDict[PediaDirector.Id.FERAL_SLIME].icon, Challenge.ChallengeType.Bad, null,null);
            new Challenge("bigSale", "c.name.bigSale", "c.desc.bigSale", SceneContext.Instance.ExchangeDirector.GetSpecRewardIcon(ExchangeDirector.NonIdentReward.NEWBUCKS_HUGE), Challenge.ChallengeType.Good, null, () => freePurchases = 3);
            new Challenge("desert", "c.name.desert", "c.desc.desert", SceneContext.Instance.PediaDirector.entryDict[PediaDirector.Id.DESERT].icon, Challenge.ChallengeType.Nuetral, null, null);
            new Challenge("fragile", "c.name.fragile", "c.desc.fragile", GameContext.Instance.LookupDirector.GetIcon(Identifiable.Id.GLASS_SHARD_CRAFT), Challenge.ChallengeType.Nuetral, null, null);
            new Challenge("upgrade", "c.name.upgrade", "c.desc.upgrade", SceneContext.Instance.PediaDirector.entryDict[PediaDirector.Id.THE_RANCH].icon, Challenge.ChallengeType.Good, null, () => { SceneContext.Instance.PlayerState.AddUpgrade(GetRandomEnum<PlayerState.Upgrade>()); SceneContext.Instance.PlayerState.AddUpgrade(GetRandomEnum<PlayerState.Upgrade>()); });
            new Challenge("weighted", "c.name.weighted", "c.desc.weighted", GameContext.Instance.LookupDirector.GetToyDefinition(Identifiable.Id.BIG_ROCK_TOY).Icon, Challenge.ChallengeType.Bad, null, null);
            new Challenge("slide", "c.name.slide", "c.desc.slide", SceneContext.Instance.PediaDirector.entryDict[PediaDirector.Id.POND].icon, Challenge.ChallengeType.Bad, null, null);
            new Challenge("thickSkin", "c.name.thickSkin", "c.desc.thickSkin", GameContext.Instance.LookupDirector.GetIcon(Identifiable.Id.CRYSTAL_PLORT), Challenge.ChallengeType.Good, null, null);
            new Challenge("noFav", "c.name.noFav", "c.desc.noFav", SceneContext.Instance.PediaDirector.entryDict[PediaDirector.Id.PLORT_MARKET].icon, Challenge.ChallengeType.Bad, null, null);
            new Challenge("delicate", "c.name.delicate", "c.desc.delicate", GameContext.Instance.LookupDirector.GetIcon(Identifiable.Id.WATER_LIQUID), Challenge.ChallengeType.Bad, null, null);
            new Challenge("lavaFloor", "c.name.lavaFloor", "c.desc.lavaFloor", GameContext.Instance.LookupDirector.GetIcon(Identifiable.Id.FIRE_SLIME), Challenge.ChallengeType.Bad, null, () => { SceneContext.Instance.PlayerState.AddUpgrade(PlayerState.Upgrade.LIQUID_SLOT); FillWaterSlot(); });
            new Challenge("marketRandom", "c.name.marketRandom", "c.desc.marketRandom", SceneContext.Instance.ExchangeDirector.GetSpecRewardIcon(ExchangeDirector.NonIdentReward.NEWBUCKS_MOCHI), Challenge.ChallengeType.Nuetral, null, null);
        }
        public static void Log(string message) => ConsoleI.Log($"[{modName}]: " + message);
        public static void LogError(string message) => ConsoleI.LogError($"[{modName}]: " + message);
        public static void LogWarning(string message) => ConsoleI.LogWarning($"[{modName}]: " + message);
        public static void LogSuccess(string message) => ConsoleI.LogSuccess($"[{modName}]: " + message);

        static void LoadData(CompoundDataPiece data)
        {
            freePurchases = 0;
            if (data.HasPiece("freePurchases"))
                freePurchases = data.GetValue<int>("freePurchases");
            if (data.HasPiece("challenges"))
            {
                data = data.GetCompoundPiece("challenges");
                var c = data.GetValue<int>("count");
                for (int i = 0; i < c; i++)
                    Challenge.TryActivate(data.GetValue<string>(i + "_id"),false);
            } else
                Waiting = true;
        }

        public static void AskForChallenge(bool noData = true)
        {
            GameContext.Instance.UITemplates.CreateConfirmDialog(noData ? "challenge.no_data" : "challenge.no_challenges", () =>
            {
                if (Config.PickChallenges)
                    ShowChallengeSelector();
                else
                {
                    var g = Mathf.Clamp01(Config.GoodPercentage);
                    var n = Mathf.Min(1 - g, Mathf.Clamp01(Config.NuetralPercentage));
                    var Good = Config.ChallengeCount * g;
                    var Nuetral = Config.ChallengeCount * n;
                    var Bad = Config.ChallengeCount - Good - Nuetral;
                    Challenge.ActivateRandom(Challenge.ChallengeType.Good, (int)Good, true);
                    Challenge.ActivateRandom(Challenge.ChallengeType.Nuetral, (int)Nuetral, true);
                    Challenge.ActivateRandom(Challenge.ChallengeType.Bad, (int)Bad, true);
                }
            });
        }

        static void WriteData(CompoundDataPiece data)
        {
            data.SetValue("freePurchases", freePurchases);
            if (data.HasPiece("challenges"))
            {
                data = data.GetCompoundPiece("challenges");
                data.DataList.Clear();
            }
            else
            {
                var d = new CompoundDataPiece("challenges");
                data.AddPiece(d);
                data = d;
            }
            data.SetValue("count", Challenge.ActiveChallenges.Count);
            for (int i = 0; i < Challenge.ActiveChallenges.Count; i++)
                data.SetValue(i + "_id", Challenge.ActiveChallenges[i].Id);
        }

        public static void ShowChallengeInfo()
        {
            GameObject ui = null;
            var items = new List<PurchaseUI.Purchasable>();
            foreach (var c in Challenge.ActiveChallenges)
                items.Add(new PurchaseUI.Purchasable(c.NameKey, c.Icon, c.Icon, c.DescKey, 0, null, () => { }, () => true, () => true,"b.blank"));
            ui = GameContext.Instance.UITemplates.CreatePurchaseUI(SceneContext.Instance.PediaDirector.lockedEntry.icon, MessageUtil.Qualify("ui", "t.challenges"), items.ToArray(), true, () => Object.Destroy(ui));
        }

        public static T GetRandomEnum<T>() where T : System.Enum
        {
            var a = System.Enum.GetValues(typeof(PlayerState.Upgrade));
            return (T)a.GetValue((int)(a.Length * rand.NextDouble()));
        }

        public static void FillWaterSlot()
        {
            var ammo = SceneContext.Instance.PlayerState.Ammo;
            var i = ammo.Slots.IndexOf((x, y) => ammo.GetSlotPredicate(y) == null ? true : ammo.GetSlotPredicate(y)(Identifiable.Id.WATER_LIQUID));
            ammo.Slots[i] = new Ammo.Slot(Identifiable.Id.WATER_LIQUID, ammo.GetSlotMaxCount(Identifiable.Id.WATER_LIQUID, i));
        }

        public static GameObject ShowChallengeSelector(List<Challenge> active = null)
        {
            if (active == null)
                active = new List<Challenge>(Challenge.ActiveChallenges);
            GameObject ui = null;
            var items = new List<PurchaseUI.Purchasable>();
            Dictionary<string, List<PurchaseUI.Purchasable>> dictionary = new Dictionary<string, List<PurchaseUI.Purchasable>>();
            dictionary.Add("active", new List<PurchaseUI.Purchasable>());
            dictionary.Add("inactive", new List<PurchaseUI.Purchasable>());
            foreach (var l in Challenge.Challenges.Values)
                foreach (var c in l)
                {
                    var flag = active.Contains(c);
                    var item = new PurchaseUI.Purchasable(c.NameKey, c.Icon, c.Icon, c.DescKey, 0, null, () => {
                        if (flag)
                            active.Remove(c);
                        else
                            active.Add(c);
                        Object.Destroy(ui);
                        var ui2 = ShowChallengeSelector(active);
                        var cat = ui2.GetComponent<PurchaseUI>().categories[flag ? "active" : "inactive"];
                        if (cat.items.Length > 0)
                        {
                            ui2.GetComponent<PurchaseUI>().ActivateCategory(cat);
                            ui2.GetComponent<PurchaseUI>().Select(cat.items.FirstOrDefault());
                        }
                    }, () => true, () => true, flag ? "b.remove" : "b.add");
                    items.Add(item);
                    dictionary[flag ? "active" : "inactive"].Add(item);
                }
            ui = GameContext.Instance.UITemplates.CreatePurchaseUI(SceneContext.Instance.PediaDirector.lockedEntry.icon, MessageUtil.Qualify("ui", "t.challenges"), items.ToArray(), true, () => {
                Object.Destroy(ui);
                foreach (var c in active)
                    if (!Challenge.ActiveChallenges.Contains(c))
                        Challenge.Activate(c, true);
                foreach (var c in Challenge.ActiveChallenges)
                    if (!active.Contains(c))
                        Challenge.ActiveChallenges.Remove(c);
            });
            List<PurchaseUI.Category> categories = new List<PurchaseUI.Category>();
            foreach (var p in dictionary)
                categories.Add(new PurchaseUI.Category(p.Key, p.Value.ToArray()));
            ui.GetComponent<PurchaseUI>().SetCategories(categories);
            return ui;
        }

        public static GameObject ShowBlacklistSelector(List<Challenge> active = null)
        {
            if (active == null)
                active = GetCurrentBlacklist();
            GameObject ui = null;
            var items = new List<PurchaseUI.Purchasable>();
            Dictionary<string, List<PurchaseUI.Purchasable>> dictionary = new Dictionary<string, List<PurchaseUI.Purchasable>>();
            dictionary.Add("allowed", new List<PurchaseUI.Purchasable>());
            dictionary.Add("disallowed", new List<PurchaseUI.Purchasable>());
            foreach (var l in Challenge.Challenges.Values)
                foreach (var c in l)
                {
                    var flag = !active.Contains(c);
                    var item = new PurchaseUI.Purchasable(c.NameKey, c.Icon, c.Icon, c.DescKey, 0, null, () => {
                        if (flag)
                            active.Add(c);
                        else
                            active.Remove(c);
                        Object.Destroy(ui);
                        var ui2 = ShowBlacklistSelector(active);
                        var cat = ui2.GetComponent<PurchaseUI>().categories[flag ? "allowed" : "disallowed"];
                        if (cat.items.Length > 0)
                        {
                            ui2.GetComponent<PurchaseUI>().ActivateCategory(cat);
                            ui2.GetComponent<PurchaseUI>().Select(cat.items.FirstOrDefault());
                        }
                    }, () => true, () => true, flag ? "b.remove" : "b.add");
                    items.Add(item);
                    dictionary[flag ? "allowed" : "disallowed"].Add(item);
                }
            ui = GameContext.Instance.UITemplates.CreatePurchaseUI(SceneContext.Instance.PediaDirector.lockedEntry.icon, MessageUtil.Qualify("ui", "t.challenge_perms"), items.ToArray(), true, () => {
                Object.Destroy(ui);
                SetCurrentBlacklist(active);
            });
            List<PurchaseUI.Category> categories = new List<PurchaseUI.Category>();
            foreach (var p in dictionary)
                categories.Add(new PurchaseUI.Category(p.Key, p.Value.ToArray()));
            ui.GetComponent<PurchaseUI>().SetCategories(categories);
            return ui;
        }

        public static List<Challenge> GetCurrentBlacklist()
        {
            var l = new List<Challenge>();
            var s = Config2.blacklist.Split('|').ToList();
            for (int i = s.Count - 2; i >= 0; i--)
            {
                int c = 0;
                for (int j = s[i].Length - 1; j >= 0; j--)
                    if (s[i][j] == '\\')
                        c++;
                    else
                        break;
                if (c % 2 == 1)
                {
                    s[i] += "|" + s[i + 1];
                    s.RemoveAt(i + 1);
                }
            }
            for (int i = 0; i < s.Count; i++)
            {
                s[i] = s[i].Replace("\\|", "|").Replace("\\\\", "\\");
                foreach (var c in Challenge.Challenges.Values)
                    l.AddRange(c.FindAll((x) => x.Id == s[i]));
            }
            return l;
        }

        public static void SetCurrentBlacklist(List<Challenge> challenges)
        {
            var l = new List<string>();
            foreach (var c in challenges)
                l.Add(c.Id.Replace("\\", "\\\\").Replace("|", "\\|"));
            Config2.blacklist = l.Join(null,"|");
            foreach (var f in SRModLoader.GetModForAssembly(modAssembly).Configs)
                f.SaveToFile();
        }
    }

    static class ExtentionMethods
    {
        public static Ammo.Slot GetLiquid(this Ammo ammo)
        {
            for (int i = 0; i < ammo.GetUsableSlotCount(); i++)
                if (ammo.Slots[i] != null && Identifiable.IsLiquid(ammo.Slots[i].id))
                    return ammo.Slots[i];
            return null;
        }
        public static void CheckEmpty(this Ammo ammo, Ammo.Slot slot)
        {
            var i = ammo.Slots.IndexOf((x) => x == slot);
            if (i >= 0 && slot.count <= 0)
                ammo.Slots[i] = null;
        }
        public static int IndexOf<T>(this T[] t, System.Predicate<T> predicate)
        {
            for (int i = 0; i < t.Length; i++)
                if (predicate(t[i]))
                    return i;
            return -1;
        }
        public static int IndexOf<T>(this T[] t, System.Func<T, int, bool> predicate)
        {
            for (int i = 0; i < t.Length; i++)
                if (predicate(t[i], i))
                    return i;
            return -1;
        }
    }

    public class Challenge
    {
        internal static List<Challenge> ActiveChallenges = new List<Challenge>();
        public static Challenge[] Active => ActiveChallenges.ToArray();
        internal static Dictionary<ChallengeType, List<Challenge>> Challenges = new Dictionary<ChallengeType, List<Challenge>>();

        string id;
        string nameKey;
        string descKey;
        Sprite icon;
        ChallengeType type;
        System.Action onUpdate;
        System.Action onStart;
        public string Id => id;
        public string NameKey => nameKey;
        public string DescKey => descKey;
        public Sprite Icon => icon;
        public ChallengeType Type => type;
        public Challenge(string Id, string NameKey, string DescKey, Sprite Icon, ChallengeType Type, System.Action OnUpdate = null, System.Action OnStart = null)
        {
            id = Id;
            nameKey = NameKey;
            descKey = DescKey;
            icon = Icon;
            type = Type;
            onUpdate = OnUpdate;
            onStart = OnStart;
            if (Challenges.ContainsKey(type))
                Challenges[type].Add(this);
            else
                Challenges.Add(Type, new List<Challenge>() { this });
        }
        internal void Update()
        {
            try
            {
                onUpdate?.Invoke();
            } catch (System.Exception e)
            {
                Main.LogError($"An error has occured in the update of challenge \"{Id}\":\n{e}");
            }
        }
        void Start()
        {
            try
            {
                onStart?.Invoke();
            }
            catch (System.Exception e)
            {
                Main.LogError($"An error has occured in the start of challenge \"{Id}\":\n{e}");
            }
        }
        public enum ChallengeType
        {
            Good, Bad, Nuetral
        }
        internal static void ActivateRandom(ChallengeType type, int count, bool triggerStart)
        {
            if (Challenges.TryGetValue(type, out var vs))
            {
                vs = vs.Except(Main.GetCurrentBlacklist()).ToList();
                for (int i = 0; i < vs.Count; i++)
                    if (Main.rand.NextDouble() < count / (double)(vs.Count - i))
                    {
                        count--;
                        ActiveChallenges.Add(vs[i]);
                        if (triggerStart)
                            vs[i].Start();
                    }
            }
        }
        internal static void TryActivate(string id, bool triggerStart)
        {
            foreach (var c in Challenges.Values)
                foreach (var v in c)
                    if (v.Id == id)
                    {
                        Activate(v, triggerStart);
                        return;
                    }
        }
        internal static void Activate(Challenge v, bool triggerStart)
        {
            ActiveChallenges.Add(v);
            if (triggerStart)
                v.Start();
        }
        public static bool IsActive(string ChallengeId) => ActiveChallenges.Exists((x) => x.Id == ChallengeId);
        public bool IsActive() => ActiveChallenges.Contains(this);
    }

    internal class ChallengeManager : SRBehaviour
    {
        void Update()
        {
            if (Main.menu.WasPressed)
            {
                if (Challenge.ActiveChallenges.Count == 0)
                    Main.AskForChallenge(false);
                else
                    Main.ShowChallengeInfo();
            }
            foreach (var c in Challenge.ActiveChallenges)
                c.Update();
        }
    }

    [ConfigFile("settings")]
    static class Config
    {
        public static int ChallengeCount = 4;
        public static float GoodPercentage = 0.25f;
        public static float NuetralPercentage = 0.25f;
        public static bool PickChallenges = false;
    }

    [ConfigFile(" ")]
    static class Config2
    {
        public static string blacklist = "";
    }

    public class DamageOnDryness : SRBehaviour, LiquidConsumer
    {
        const float timeUntilDry = 10;
        const float damageDelay = 1;
        float lastWaterTime;
        bool isPlayer;
        vp_FPController controller;
        public List<LiquidSource> sources = new List<LiquidSource>();
        void Awake()
        {
            lastWaterTime = Time.time;
            isPlayer = gameObject == SceneContext.Instance.Player;
            controller = GetComponent<vp_FPController>();
        }
        public void AddLiquid(Identifiable.Id id, float units) => lastWaterTime = Time.time;

        void Update()
        {
            
            if (!Challenge.IsActive("lavaFloor") || sources.Count > 0)
                lastWaterTime = Time.time;
            if (lastWaterTime + timeUntilDry < Time.time)
            {
                if (isPlayer)
                {
                    var slot = SceneContext.Instance.PlayerState.Ammo.GetLiquid();
                    if (slot != null)
                    {
                        while (lastWaterTime + timeUntilDry < Time.time && slot.count > 0)
                        {
                            lastWaterTime += timeUntilDry;
                            slot.count--;
                        }
                        SceneContext.Instance.PlayerState.Ammo.CheckEmpty(slot);
                    }
                }
            }
            while (lastWaterTime + timeUntilDry < Time.time)
            {
                if (!controller || controller.Grounded)
                {
                    lastWaterTime += damageDelay;
                    if (GetComponent<Damageable>().Damage(5, gameObject))
                        DeathHandler.Kill(gameObject, DeathHandler.Source.SLIME_IGNITE, gameObject, "DamageOnDryness.Update");
                }
                else
                    lastWaterTime += Time.deltaTime;
            }
        }
    }

    public class DestroyOnPlayer : SRBehaviour, ControllerCollisionListener
    {
        public static GameObject destroyFX;
        SlimeDiet diet;
        void Awake()
        {
            diet = GetComponent<SlimeEat>().slimeDefinition.Diet;
        }
        public void OnControllerCollision(GameObject gO)
        {
            if (gO == SceneContext.Instance.Player && Challenge.IsActive("delicate"))
            {
                Identifiable.Id id = Identifiable.Id.NONE;
                if (diet != null && diet.Produces != null && diet.Produces.Length > 0)
                    id = diet.Produces[(int)(diet.Produces.Length * Main.rand.NextDouble())];
                if (id != Identifiable.Id.NONE)
                {
                    var g = InstantiateActor(GameContext.Instance.LookupDirector.GetPrefab(id), MonomiPark.SlimeRancher.Regions.RegionRegistry.RegionSetId.HOME);
                    g.transform.position = transform.position;
                    g.transform.rotation = transform.rotation;
                }
                if (destroyFX)
                    SpawnAndPlayFX(destroyFX, transform.position, transform.rotation);
                Destroyer.DestroyActor(gameObject, "DestroyOnPlayer");
            }
        }
    }

    [HarmonyPatch(typeof(AutoSaveDirector), "OnNewGameLoaded")]
    class Patch_LoadNewGame { static void Prefix() => Main.Waiting = true; }

    [HarmonyPatch(typeof(WeaponVacuum), "Expel", typeof(HashSet<GameObject>))]
    class Patch_ShootFromVac
    {
        static bool Prefix(WeaponVacuum __instance)
        {
            if (!Challenge.IsActive("vacCost")) return true;
            if (__instance.player.GetCurrEnergy() < 5)
            {
                __instance.PlayTransientAudio(__instance.vacShootEmptyCue);
                return false;
            }
            if (__instance.held || __instance.player.Ammo.HasSelectedAmmo())
                __instance.player.SpendEnergy(5);
            return true;
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum), "Consume")]
    class Patch_VacSucc
    {
        public static float lastTime;
        public static bool called = false;
        static void Postfix(WeaponVacuum __instance, HashSet<GameObject> inVac)
        {
            if (!Challenge.IsActive("vacCost"))
                return;
            if (__instance.player.GetCurrEnergy() < 1)
                foreach (var i in inVac)
                {
                    var c = i.GetComponent<Vacuumable>();
                    if (c) c.release();
                }
            else
            {
                called = true;
                var n = Time.time - lastTime;
                while (n > 0.02f)
                {
                    n -= 0.02f;
                    lastTime += 0.02f;
                    __instance.player.SpendEnergy(1);
                }
            }
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum), "Update")]
    class Patch_VacUpdate
    {
        static void Prefix() => Patch_VacSucc.called = false;
        static void Postfix() { if (!Patch_VacSucc.called) Patch_VacSucc.lastTime = Time.time; }
    }

    [HarmonyPatch(typeof(DirectedSlimeSpawner),"SpawnFX")]
    class Patch_SlimeSpawner
    {
        static void Postfix(GameObject spawnedObj) {
            if (!Challenge.IsActive("allFeral")) return;
            var feral = spawnedObj.GetComponent<SlimeFeral>();
            if (feral)
            {
                feral.SetFeral();
                feral.expireAt = double.PositiveInfinity;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerState), "SpendCurrency")]
    class Patch_SpendMoney
    {
        static void Prefix(ref int adjust)
        {
            if (Main.freePurchases > 0)
            {
                Main.freePurchases--;
                adjust = 0;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerState), "GetCurrency")]
    class Patch_GetMoney
    {
        static void Postfix(ref int __result)
        {
            if (Main.freePurchases > 0 && new System.Diagnostics.StackTrace().GetFrames().FirstOrDefault((x) =>
            {
                var t = x.GetMethod().DeclaringType;
                return t == typeof(ScoreUI) || t.IsSubclassOf(typeof(ScoreUI)) || t == typeof(DroneFastForwarder) || t.IsSubclassOf(typeof(DroneFastForwarder));
            }) == null)
            {
                __result = int.MaxValue;
            }
        }
    }

    [HarmonyPatch(typeof(CellDirector), "GetNonignoredAnimalCount")]
    class Patch_GetAnimalCount { static void Postfix(ref int __result) { if (Challenge.IsActive("desert")) __result *= 4; } }

    [HarmonyPatch(typeof(GameObjectActorModelIdentifiableIndex), "GetSlimeCount")]
    class Patch_GetSlimeCount { static void Postfix(ref int __result) { if (Challenge.IsActive("desert")) __result *= 4; } }

    [HarmonyPatch(typeof(GameObjectActorModelIdentifiableIndex), "GetLargoCount")]
    class Patch_GetLargoCount { static void Postfix(ref int __result) { if (Challenge.IsActive("desert")) __result *= 4; } }

    [HarmonyPatch(typeof(DirectedActorSpawner), "Spawn")]
    class Patch_SpawnActors { static void Prefix(ref int count) { if (Challenge.IsActive("desert")) count /= 2; } }

    [HarmonyPatch(typeof(PlayerDamageable), "Damage")]
    class Patch_DamagePlayer {
        static void Prefix(ref int healthLoss)
        {
            if (Challenge.IsActive("fragile"))
                healthLoss *= 2;
            if (Challenge.IsActive("thickSkin"))
            {
                var m = new System.Diagnostics.StackTrace(2).GetFrame(0).GetMethod();
                var n = m.Name.ToLowerInvariant();
                var t = m.DeclaringType.Name.ToLowerInvariant();
                if (n.Contains("trigger") || n.Contains("touch") || n.Contains("collision") || t.Contains("trigger") || t.Contains("touch") || t.Contains("collision"))
                    healthLoss = 0;
            }
        }
    }

    [HarmonyPatch(typeof(SlimeHealth), "Damage")]
    class Patch_DamageSlime { static void Prefix(ref int healthLoss) { if (Challenge.IsActive("fragile")) healthLoss *= 2; } }

    [HarmonyPatch(typeof(vp_FPController), "UpdateThrottleWalk")]
    class Patch_PlayerWalkUpdate
    {
        static void Prefix(vp_FPController __instance, ref Vector4 __state)
        {
            __state = Vector4.zero;
            if (Challenge.IsActive("slide"))
            {
                __state.x = __instance.m_Grounded ? 1 : 2;
                __state.y = __instance.MotorAirSpeed;
                __instance.m_Grounded = false;
                __instance.MotorAirSpeed = (__instance.m_Grounded ? 1 : __instance.MotorAirSpeed) * 0.1f;
            }
            if (Challenge.IsActive("weighted"))
            {
                __state.z = 1;
                __state.w = __instance.MotorAcceleration;
                var ammo = SceneContext.Instance.PlayerState.Ammo;
                var f = 0f;
                for (int i = 0; i < ammo.GetUsableSlotCount(); i++)
                    f += ammo.GetFullness(i);
                __instance.MotorAcceleration *= 1f - f / ammo.GetUsableSlotCount() * 0.9f;
            }
        }
        static void Postfix(vp_FPController __instance, ref Vector4 __state)
        {
            if (__state.x != 0)
            {
                __instance.m_Grounded = __state.x == 1;
                __instance.MotorAirSpeed = __state.y;
            }
            if (__state.z == 1)
                __instance.MotorAcceleration = __state.w;
        }
    }

    [HarmonyPatch(typeof(SlimeEat), "GetProducedIds", typeof(Identifiable.Id), typeof(List<Identifiable.Id>))]
    class Patch_GetProducedPlorts
    {
        static void Prefix(ref List<SlimeDiet.EatMapEntry> __state, Identifiable.Id foodId, SlimeEat __instance)
        {
            __state = new List<SlimeDiet.EatMapEntry>();
            if (Challenge.IsActive("noFav"))
                foreach (var e in __instance.slimeDefinition.Diet.EatMap)
                    if (e.eats == foodId && e.isFavorite)
                    {
                        __state.Add(e);
                        e.isFavorite = false;
                    }
        }
        static void Postfix(ref List<SlimeDiet.EatMapEntry> __state)
        {
            foreach (var e in __state)
                e.isFavorite = true;
        }
    }

    [HarmonyPatch(typeof(SlimeDiet.EatMapEntry), "NumToProduce")]
    class Patch_NumPlorts
    {
        static void Postfix(ref int __result)
        {
            if (Challenge.IsActive("noFav"))
                __result = 1;
        }
    }

    [HarmonyPatch(typeof(LiquidSource), "OnTriggerEnter")]
    class Patch_EnterWater
    {
        static void Postfix(LiquidSource __instance, Collider collider)
        {
            if (!collider.attachedRigidbody)
                return;
            var d = collider.attachedRigidbody.GetComponent<DamageOnDryness>();
            if (d)
                d.sources.Add(__instance);
        }
    }

    [HarmonyPatch(typeof(LiquidSource), "OnTriggerExit")]
    class Patch_ExitWater
    {
        static void Postfix(LiquidSource __instance, Collider collider)
        {
            if (!collider.attachedRigidbody)
                return;
            var d = collider.attachedRigidbody.GetComponent<DamageOnDryness>();
            if (d)
                d.sources.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerDeathHandler),"ResetPlayer")]
    class Patch_PlayerDeath
    {
        static void Prefix(ref UnityEngine.Events.UnityAction onComplete)
        {
            if (Challenge.IsActive("lavaFloor"))
            {
                if (onComplete == null)
                    onComplete = Main.FillWaterSlot;
                else
                    onComplete += Main.FillWaterSlot;
            }
        }
    }

    [HarmonyPatch(typeof(EconomyDirector), "GetTargetValue")]
    class Patch_GetEconomyValue
    {
        public static bool calling = false;
        static void Prefix() => calling = true;
        static void Postfix() => calling = false;
    }

    [HarmonyPatch(typeof(Noise.Noise), "PerlinNoise")]
    class Patch_NoiseRandom
    {
        const float multi = 3;
        static void Prefix(ref float height, ref float __state)
        {
            if (Patch_GetEconomyValue.calling && Challenge.IsActive("marketRandom"))
            {
                __state = height * (multi - 1) / 2;
                height *= multi;
            }
        }
        static void Postfix(ref float __result, ref float __state)
        {
            if (Patch_GetEconomyValue.calling && Challenge.IsActive("marketRandom"))
                __result -= __state;
        }
    }

    [HarmonyPatch(typeof(SlimeFeral), "Awake")]
    class Patch_FeralAwake
    {
        public static bool calling = false;
        static void Prefix() => calling = true;
        static void Postfix() => calling = false;
    }

    [HarmonyPatch(typeof(Destroyer), "Destroy", typeof(Object), typeof(string))]
    class Patch_DestroyObject
    {
        static bool Prefix() => !Patch_FeralAwake.calling;
    }

    [HarmonyPatch(typeof(SlimeEat), "EatAndTransform")]
    class Patch_SlimeTransform
    {
        public static bool calling = false;
        static void Prefix(SlimeEat __instance) => calling = true;
        static void Postfix() => calling = false;
    }

    [HarmonyPatch(typeof(SRBehaviour), "InstantiateActor", typeof(GameObject), typeof(MonomiPark.SlimeRancher.Regions.RegionRegistry.RegionSetId), typeof(Vector3), typeof(Quaternion), typeof(bool))]
    class Patch_InstantiateActor
    {
        static void Postfix(GameObject __result)
        {
            if (!Patch_SlimeTransform.calling)
                return;
            var c = __result.GetComponent<SlimeFeral>();
            if (c && Challenge.IsActive("allFeral"))
            {
                c.SetFeral();
                c.expireAt = double.PositiveInfinity;
            }
        }
    }

    class BlacklistCommand : ConsoleCommand
    {
        public override string Usage => "challengeblacklist";
        public override string ID => "challengeblacklist";
        public override string Description => "Opens the challenge blacklist menu";
        public override bool Execute(string[] args)
        {
            Main.ShowBlacklistSelector();
            return true;
        }
    }
}