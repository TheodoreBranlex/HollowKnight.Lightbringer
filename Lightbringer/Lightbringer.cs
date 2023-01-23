using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GlobalEnums;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;
using IntCompare = On.HutongGames.PlayMaker.Actions.IntCompare;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Lightbringer
{
    public partial class Lightbringer : Mod, IMenuMod, ITogglableMod, IGlobalSettings<Settings>
    {
        private const float ORIG_RUN_SPEED = 8.3f;
        private const float ORIG_RUN_SPEED_CH = 12f;
        private const float ORIG_RUN_SPEED_CH_COMBO = 13.5f;

        internal static Lightbringer instance;

        private GameObject canvas;

        private GameObject gruz;
        private GameObject kin;

        private int hits;

        // Update Function Variables
        private float manaRegenTime = Time.deltaTime;

        private float origNailTerrainCheckTime;
        private bool passionDirection = true;
        private float passionTime = Time.deltaTime;

        private Text muzznickText;
        public static float timeFracture = 1f;

        public static SpriteFlash spriteFlash;

        internal static readonly Random random = new Random();

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Settings settings = new Settings();
        public void OnLoadGlobal(Settings s) => settings = s;
        public Settings OnSaveGlobal() => settings;

        // Mod menu
        public bool ToggleButtonInsideMenu => true;
        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry) =>
            new List<IMenuMod.MenuEntry> {
                toggleButtonEntry.Value,
                new IMenuMod.MenuEntry {
                    Name = "Empress Muzznik",
                    Description = "Turns Gruz Mother into a worthy challenge",
                    Values = new string[] { "On", "Off" },
                    Saver = option => settings.EmpressMuzznik = (option == 0),
                    Loader = () => settings.EmpressMuzznik ? 0 : 1
                },
                new IMenuMod.MenuEntry {
                    Name = "Double Kin",
                    Description = "Doubles the Lost Kin for double the fun",
                    Values = new string[] { "On", "Off" },
                    Saver = option => settings.DoubleKin = (option == 0),
                    Loader = () => settings.DoubleKin ? 0 : 1
                },
                new IMenuMod.MenuEntry {
                    Name = "Nail Collision",
                    Description = "At short distance, your lance acts as a nail to break otherwise unbreakable objets. You can use your upward slash instead.",
                    Values = new string[] { "On", "Off" },
                    Saver = option => settings.NailCollision = (option == 0),
                    Loader = () => settings.NailCollision ? 0 : 1
                }
            };

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            instance = this;

            if (preloadedObjects != null) // Else prefabs where already loaded
                ChildOfLight.Setup(preloadedObjects);
            ChildOfLight.Launch();

            Sprites.Load();
            Sprites.Enable(true);

            if (PlayerData.instance != null)
                SaveGameSave();

            RegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            ModHooks.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.SavegameSaveHook += SaveGameSave;
            ModHooks.BeforeSavegameSaveHook += BeforeSaveGameSave;
            ModHooks.NewGameHook += OnNewGame;

            On.HeroController.Attack += Attack;
            ModHooks.DoAttackHook += DoAttack;
            ModHooks.AfterAttackHook += AfterAttack;
            On.NailSlash.StartSlash += StartSlash;
            IntCompare.DoIntCompare += DoIntCompare;

            On.HeroController.FaceLeft += FaceLeft;
            On.HeroController.FaceRight += FaceRight;
            On.HeroController.RecoilLeft += RecoilLeft;
            On.HeroController.RecoilRight += RecoilRight;

            ModHooks.CharmUpdateHook += CharmUpdate;

            ModHooks.BeforeAddHealthHook += EvaluatePanic;
            ModHooks.TakeHealthHook += EvaluatePanic;
            ModHooks.TakeHealthHook += TakeHealth;
            On.PlayerData.UpdateBlueHealth += UpdateBlueHealth;

            On.PlayerData.AddGeo += AddGeo;
            ModHooks.SoulGainHook += SoulGain;
            On.ShopItemStats.Awake += ShopAwake;

            USceneManager.sceneLoaded += SceneLoadedHook;
            ModHooks.HeroUpdateHook += Update;

            ModHooks.LanguageGetHook += Language.LangGet;
        }

        public void Unload()
        {
            ModHooks.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.SavegameSaveHook -= SaveGameSave;
            ModHooks.BeforeSavegameSaveHook -= BeforeSaveGameSave;
            ModHooks.NewGameHook -= OnNewGame;

            On.HeroController.Attack -= Attack;
            ModHooks.DoAttackHook -= DoAttack;
            ModHooks.AfterAttackHook -= AfterAttack;
            On.NailSlash.StartSlash -= StartSlash;
            IntCompare.DoIntCompare -= DoIntCompare;

            On.HeroController.FaceLeft -= FaceLeft;
            On.HeroController.FaceRight -= FaceRight;
            On.HeroController.RecoilLeft -= RecoilLeft;
            On.HeroController.RecoilRight -= RecoilRight;

            ModHooks.CharmUpdateHook -= CharmUpdate;

            ModHooks.TakeHealthHook -= TakeHealth;
            ModHooks.TakeHealthHook -= EvaluatePanic;
            ModHooks.BeforeAddHealthHook += EvaluatePanic;
            On.PlayerData.UpdateBlueHealth -= UpdateBlueHealth;

            On.PlayerData.AddGeo -= AddGeo;
            ModHooks.SoulGainHook -= SoulGain;
            On.ShopItemStats.Awake -= ShopAwake;

            USceneManager.sceneLoaded -= SceneLoadedHook;
            ModHooks.HeroUpdateHook -= Update;

            ModHooks.LanguageGetHook -= Language.LangGet;

            ChildOfLight.Cancel();

            Sprites.Enable(false);

            if (PlayerData.instance != null)
                BeforeSaveGameSave();
        }

        private void AfterSaveGameLoad(SaveGameData data)
        {
            SaveGameSave();
            ChildOfLight.Launch();
        }

        private static void SaveGameSave(int id = 0)
        {
            PlayerData.instance.charmCost_21 = 1;
            PlayerData.instance.charmCost_19 = 4;
            PlayerData.instance.charmCost_15 = 3;
            PlayerData.instance.charmCost_14 = 2;
            PlayerData.instance.charmCost_8 = 3;
            PlayerData.instance.charmCost_35 = 4;
            PlayerData.instance.charmCost_18 = 3;
            PlayerData.instance.charmCost_3 = 2;
            PlayerData.instance.charmCost_38 = 2;
        }

        private static void BeforeSaveGameSave(SaveGameData data = null)
        {
            PlayerData.instance.charmCost_21 = 4;
            PlayerData.instance.charmCost_19 = 3;
            PlayerData.instance.charmCost_15 = 2;
            PlayerData.instance.charmCost_14 = 1;
            PlayerData.instance.charmCost_8 = 2;
            PlayerData.instance.charmCost_35 = 3;
            PlayerData.instance.charmCost_18 = 2;
            PlayerData.instance.charmCost_3 = 1;
            PlayerData.instance.charmCost_38 = 3;
            PlayerData.instance.nailDamage = PlayerData.instance.nailSmithUpgrades * 4 + 5;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }

        private static void OnNewGame()
        {
            PlayerData.instance.maxHealthBase = PlayerData.instance.maxHealth = PlayerData.instance.health = 4;
            PlayerData.instance.charmSlots += 1;
        }

        private void Attack(On.HeroController.orig_Attack orig, HeroController self, AttackDirection attackdir)
        {
            new AttackHandler(timeFracture).Attack(self, attackdir);
        }

        private void DoAttack()
        {
            if (origNailTerrainCheckTime == 0)
                origNailTerrainCheckTime = ReflectionHelper.GetField<HeroController, float>(HeroController.instance, "NAIL_TERRAIN_CHECK_TIME");

            if (!(HeroController.instance.vertical_input < Mathf.Epsilon) &&
                !(HeroController.instance.vertical_input < -Mathf.Epsilon &&
                  HeroController.instance.hero_state != ActorStates.idle &&
                  HeroController.instance.hero_state != ActorStates.running))
                ReflectionHelper.SetField(HeroController.instance, "NAIL_TERRAIN_CHECK_TIME", 0f);
        }

        private void AfterAttack(AttackDirection dir)
        {
            ReflectionHelper.SetField(HeroController.instance, "NAIL_TERRAIN_CHECK_TIME", origNailTerrainCheckTime);
        }

        private static void StartSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            orig(self);
            var slashFsm = ReflectionHelper.GetField<NailSlash, PlayMakerFSM>(self, "slashFsm");
            float slashAngle = slashFsm.FsmVariables.FindFsmFloat("direction").Value;
            var anim = ReflectionHelper.GetField<NailSlash, tk2dSpriteAnimator>(self, "anim");
            if (slashAngle == 0f || slashAngle == 180f)
            {
                self.transform.localScale = new Vector3(self.scale.x * 0.32f, self.scale.y * 0.32f, self.scale.z);
                self.transform.SetPositionZ(9999f);
                anim.Play(self.animName);
                return;
            }

            if (ReflectionHelper.GetField<NailSlash, bool>(self, "mantis")) // Burning blade
            {
                self.transform.localScale = new Vector3(self.scale.x * 1.35f, self.scale.y * 1.35f, self.scale.z);
                anim.Play(self.animName + " F");
            }
            else
            {
                self.transform.localScale = self.scale;
                anim.Play(self.animName);
            }

            if (ReflectionHelper.GetField<NailSlash, bool>(self, "fury")) anim.Play(self.animName + " F");
        }

        // It should take more hits to stun bosses 
        private static void DoIntCompare(IntCompare.orig_DoIntCompare orig, HutongGames.PlayMaker.Actions.IntCompare self)
        {
            if (self.integer2.Name.StartsWith("Stun"))
            {
                self.integer2.Value *= 3;
                orig(self);
                self.integer2.Value /= 3;
            }
            else
                orig(self);
        }

        // Remove Steady Blow's effect
        private static void RecoilLeft(On.HeroController.orig_RecoilLeft orig, HeroController self)
        {
            bool temp = self.playerData.equippedCharm_14;
            self.playerData.equippedCharm_14 = false;
            orig(self);
            self.playerData.equippedCharm_14 = temp;
        }

        private static void RecoilRight(On.HeroController.orig_RecoilRight orig, HeroController self)
        {
            bool temp = self.playerData.equippedCharm_14;
            self.playerData.equippedCharm_14 = false;
            orig(self);
            self.playerData.equippedCharm_14 = temp;
        }

        private static void FaceLeft(On.HeroController.orig_FaceLeft orig, HeroController self)
        {
            self.cState.facingRight = false;
            Vector3 localScale = self.transform.localScale;
            localScale.x = self.playerData.equippedCharm_4 ? 0.75f : 1f;
            self.transform.localScale = localScale;
        }

        private static void FaceRight(On.HeroController.orig_FaceRight orig, HeroController self)
        {
            self.cState.facingRight = true;
            Vector3 localScale = self.transform.localScale;
            localScale.x = self.playerData.equippedCharm_4 ? -0.75f : -1f;
            self.transform.localScale = localScale;
        }

        private void CharmUpdate(PlayerData pd, HeroController self)
        {
            // Charm Costs
            SaveGameSave();

            // Tiny Shell charm
            if (PlayerData.instance.equippedCharm_4)
            {
                self.transform.SetScaleX(.75f * Math.Sign(self.transform.GetScaleX()));
                self.transform.SetScaleY(.75f * Math.Sign(self.transform.GetScaleY()));
            }
            else
            {
                self.transform.SetScaleX(1f * Math.Sign(self.transform.GetScaleX()));
                self.transform.SetScaleY(1f * Math.Sign(self.transform.GetScaleY()));
            }

            if (!PlayerData.instance.equippedCharm_2)
            {
                HeroController.instance.RUN_SPEED = ORIG_RUN_SPEED;
                HeroController.instance.RUN_SPEED_CH = ORIG_RUN_SPEED_CH;
                HeroController.instance.RUN_SPEED_CH_COMBO = ORIG_RUN_SPEED_CH_COMBO;
            }

            pd.isInvincible = false;

            // Reset time to normal
            Time.timeScale = 1f;
            timeFracture = 1f;
        }

        private static int EvaluatePanic(int amount)
        {
            float panicSpeed = 1f;

            HeroController heroController = HeroController.instance;

            if (PlayerData.instance.equippedCharm_2)
            {
                int missingHealth = PlayerData.instance.maxHealth - PlayerData.instance.health;
                panicSpeed += missingHealth * .03f;
                heroController.RUN_SPEED = ORIG_RUN_SPEED * panicSpeed;
                heroController.RUN_SPEED_CH = ORIG_RUN_SPEED_CH * panicSpeed;
                heroController.RUN_SPEED_CH_COMBO = ORIG_RUN_SPEED_CH_COMBO * panicSpeed;
            }
            else
            {
                heroController.RUN_SPEED = ORIG_RUN_SPEED;
                heroController.RUN_SPEED_CH = ORIG_RUN_SPEED_CH;
                heroController.RUN_SPEED_CH_COMBO = ORIG_RUN_SPEED_CH_COMBO;
            }

            return amount;
        }

        private static int TakeHealth(int amount)
        {
            PlayerData.instance.ghostCoins = 1; // for timefracture

            if (!PlayerData.instance.equippedCharm_6) return amount;
            PlayerData.instance.health = 0;
            return 0;
        }

        private static void UpdateBlueHealth(On.PlayerData.orig_UpdateBlueHealth orig, PlayerData self)
        {
            self.SetInt("healthBlue", 0);
            if (self.GetBool("equippedCharm_9"))
                self.SetInt("healthBlue", self.GetInt("healthBlue") + 4);
        }

        private static void AddGeo(On.PlayerData.orig_AddGeo orig, PlayerData self, int amount)
        {
            // Don't let Faulty Wallet hurt people with full SOUL
            if (PlayerData.instance.equippedCharm_21 &&
                (PlayerData.instance.MPCharge < PlayerData.instance.maxMP ||
                 PlayerData.instance.MPReserve != PlayerData.instance.MPReserveMax))
            {
                int lostGeo = (PlayerData.instance.maxMP - 1 - PlayerData.instance.MPCharge) / 3 +
                              (PlayerData.instance.MPReserveMax - PlayerData.instance.MPReserve) / 3 + 1;
                HeroController.instance.AddMPChargeSpa(lostGeo > amount ? amount * 3 : lostGeo * 3);
                orig(self, lostGeo > amount ? 0 : amount - lostGeo);
            }
            else
                orig(self, amount);
        }

        private int SoulGain(int amount)
        {
            if (!PlayerData.instance.equippedCharm_15) return 0;
            hits++;
            if (hits != 5) return 0;
            HeroController.instance.AddHealth(1);
            hits = 0;
            spriteFlash.flash(Color.red, 0.7f, 0.45f, 0f, 0.45f);
            return 0;
        }

        private void ShopAwake(On.ShopItemStats.orig_Awake orig, ShopItemStats self)
        {
            orig(self);

            string pdbool = self.playerDataBoolName;
            if (!pdbool.StartsWith("gotCharm_")) return;

            string key = "Charms." + pdbool.Substring(9, pdbool.Length - 9);
            if (Sprites.customSprites.ContainsKey(key))
            {
                var itemSprite = ReflectionHelper.GetField<ShopItemStats, GameObject>(self, "itemSprite");
                itemSprite.GetComponent<SpriteRenderer>().sprite = Sprites.customSprites[key];
            }
        }

        private void SceneLoadedHook(Scene arg0, LoadSceneMode lsm)
        {
            // Without this your shade doesn't go away when you die.
            if (GameManager.instance == null) return;
            GameManager.instance.StartCoroutine(SceneLoaded(arg0));
        }

        private IEnumerator SceneLoaded(Scene arg0)
        {
            Sprites.Enable(true);

            CreateCanvas();

            if (!settings.EmpressMuzznik || arg0.name != "Crossroads_04" || PlayerData.instance.killedBigFly) yield break;

            PlayerData.instance.CountGameCompletion();

            if (PlayerData.instance.completionPercentage > 80)
                muzznickText.text = "You are ready. Empress Muzznik awaits you.";
            else if (PlayerData.instance.completionPercentage > 60)
                muzznickText.text = "You might just stand a chance...";
            else
                muzznickText.text = "You are unworthy. Come back when you are stronger.";

            muzznickText.CrossFadeAlpha(1f, 0f, false);
            muzznickText.CrossFadeAlpha(0f, 7f, false);
        }

        private void CreateCanvas()
        {
            if (canvas != null) return;

            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            Object.DontDestroyOnLoad(canvas);

            GameObject gameObject = CanvasUtil.CreateTextPanel
            (
                canvas,
                "",
                27,
                TextAnchor.MiddleCenter,
                new CanvasUtil.RectData
                (
                    new Vector2(0, 50),
                    new Vector2(0, 45),
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0.5f, 0.5f)
                )
            );

            muzznickText = gameObject.GetComponent<Text>();
            muzznickText.font = CanvasUtil.TrajanBold;
            muzznickText.text = "";
            muzznickText.fontSize = 42;
        }

        private void Update()
        {
            if (timeFracture < 1f || PlayerData.instance.ghostCoins == 1)
            {
                PlayerData.instance.ghostCoins = 0;
                timeFracture = 1f;
            }

            if (timeFracture > .99f && PlayerData.instance.equippedCharm_14 && !HeroController.instance.cState.isPaused)
                Time.timeScale = timeFracture;

            // Double Kin
            if (settings.DoubleKin && kin == null)
            {
                kin = GameObject.Find("Lost Kin");
                if (kin != null) kin.AddComponent<DoubleKin>();
            }

            // EMPRESS MUZZNIK BOSS FIGHT
            if (settings.EmpressMuzznik && gruz == null)
            {
                gruz = GameObject.Find("Giant Fly");
                if (gruz != null && GameManager.instance.GetSceneNameString() == "Crossroads_04") gruz.AddComponent<Muzznik>();
            }

            manaRegenTime += Time.deltaTime * Time.timeScale;
            if (manaRegenTime >= settings.SoulRegenRate && GameManager.instance.soulOrb_fsm != null)
            {
                if (spriteFlash == null)
                    spriteFlash = HeroController.instance.GetComponent<SpriteFlash>();
                
                manaRegenTime -= settings.SoulRegenRate;
                HeroController.instance.AddMPChargeSpa(1);
                foreach (int i in new int[] {17, 19, 34, 30, 28, 22, 25})
                    if (ReflectionHelper.GetField<PlayerData, bool>(PlayerData.instance, "equippedCharm_" + i))
                        if (i != 25 || !PlayerData.instance.brokenCharm_25)
                            HeroController.instance.AddMPChargeSpa(1);
            }

            #region Nailmaster's Passion

            if (!PlayerData.instance.equippedCharm_26) return;

            passionTime += Time.deltaTime * Time.timeScale;
            if (passionTime < 2f) return;

            passionTime -= 2f;
            passionDirection = !passionDirection;

            float posX = (passionDirection ? -1 : 1) * random.Next(3, 12);
            if (!passionDirection)
            {
                new AttackHandler().SpawnBeam
                (
                    passionDirection,
                    1f,
                    1f,
                    posX,
                    -0.5f + posX / 6f
                );
            }
            else
            {
                new AttackHandler().SpawnBeam
                (
                    passionDirection,
                    1f,
                    1f,
                    posX,
                    0.5f - posX / 6f
                );
            }
            if (AttackHandler.BeamAudioClip == null)
            {
                GameObject beamPrefab = ReflectionHelper.GetField<HeroController, GameObject>(HeroController.instance, "grubberFlyBeamPrefabU");
                AudioSource beamAudio = beamPrefab.GetComponent<AudioSource>();
                AttackHandler.BeamAudioClip = beamAudio.clip;
            }
            ReflectionHelper.GetField<HeroController, AudioSource>(HeroController.instance, "audioSource").PlayOneShot(AttackHandler.BeamAudioClip, 0.1f);
            
            #endregion
        }
    }
}