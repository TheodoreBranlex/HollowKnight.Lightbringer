using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using GlobalEnums;
using HutongGames.PlayMaker;
using JetBrains.Annotations;
using Vasi;
using Modding;
using On.HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Lightbringer
{
    [UsedImplicitly]
    public partial class Lightbringer : Mod, ITogglableMod, IGlobalSettings<LightbringerSettings>
    {
        private const float ORIG_RUN_SPEED = 8.3f;
        private const float ORIG_RUN_SPEED_CH = 12f;
        private const float ORIG_RUN_SPEED_CH_COMBO = 13.5f;

        internal static Lightbringer Instance;

        private Assembly _asm;
        private GameObject _canvas;

        private GameObject _gruz;
        private int _hitNumber;
        private GameObject _kin;

        // Update Function Variables
        private float _manaRegenTime = Time.deltaTime;

        private float _origNailTerrainCheckTime;
        private bool _passionDirection = true;
        private float _passionTime = Time.deltaTime;

        private Text _textObj;
        public static float _timefracture = 1f;

        internal Dictionary<string, Sprite> Sprites;

        internal static readonly Random Random = new Random();

        public static SpriteFlash _SpriteFlash;

        public LightbringerSettings Settings = new LightbringerSettings();
        public void OnLoadGlobal(LightbringerSettings s) => Settings = s;
        public LightbringerSettings OnSaveGlobal() => Settings;

        public override string GetVersion()
        {
            return "v1.30";
        }

        public override void Initialize()
        {
            Instance = this;
            RegisterCallbacks();
        }

        public void Unload()
        {
            On.HeroController.FaceLeft -= TinyShell.FaceLeft;
            On.HeroController.FaceRight -= TinyShell.FaceRight;
            IntCompare.DoIntCompare -= DoIntCompare;
            On.ShopItemStats.Awake -= Awake;
            On.HeroController.Attack -= Attack;
            On.PlayerData.AddGeo -= AddGeo;
            On.NailSlash.StartSlash -= StartSlash;
            On.PlayerData.UpdateBlueHealth -= UpdateBlueHealth;
            On.HeroController.RecoilLeft -= RecoilLeft;
            On.HeroController.RecoilRight -= RecoilRight;
            ModHooks.BeforeSavegameSaveHook -= BeforeSaveGameSave;
            ModHooks.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.SavegameSaveHook -= SaveGameSave;
            ModHooks.NewGameHook -= OnNewGame;
            ModHooks.TakeHealthHook -= Health;
            ModHooks.DoAttackHook -= DoAttack;
            ModHooks.AfterAttackHook -= AfterAttack;
            ModHooks.TakeHealthHook -= TakeHealth;
            ModHooks.SoulGainHook -= SoulGain;
            ModHooks.HeroUpdateHook -= Update;
            ModHooks.CharmUpdateHook -= CharmUpdate;
            ModHooks.LanguageGetHook -= LangGet;
            USceneManager.sceneLoaded -= SceneLoadedHook;

            if (PlayerData.instance != null)
                BeforeSaveGameSave();
        }

        private void Attack(On.HeroController.orig_Attack orig, HeroController self, AttackDirection attackdir)
        {
            new AttackHandler(_timefracture).Attack(self, attackdir);
        }

        private void CreateCanvas()
        {
            if (_canvas != null) return;

            _canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            Object.DontDestroyOnLoad(_canvas);

            GameObject gameObject = CanvasUtil.CreateTextPanel
            (
                _canvas,
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

            _textObj = gameObject.GetComponent<Text>();
            _textObj.font = CanvasUtil.TrajanBold;
            _textObj.text = "";
            _textObj.fontSize = 42;
        }

        private void RegisterCallbacks()
        {
            // Tiny Shell fixes
            On.HeroController.FaceLeft += TinyShell.FaceLeft;
            On.HeroController.FaceRight += TinyShell.FaceRight;

            // Stun Resistance
            IntCompare.DoIntCompare += DoIntCompare;

            // Sprites!
            On.ShopItemStats.Awake += Awake;

            // Lance Spawn 
            On.HeroController.Attack += Attack;

            // Faulty Wallet
            On.PlayerData.AddGeo += AddGeo;

            // Burning Blade, Fury
            On.NailSlash.StartSlash += StartSlash;

            // Ascending Light won't give 2 hearts
            On.PlayerData.UpdateBlueHealth += UpdateBlueHealth;

            // Fix Recoil with Steady Blow
            On.HeroController.RecoilLeft += RecoilLeft;
            On.HeroController.RecoilRight += RecoilRight;

            // Charm Values 
            // Restore Nail Damage 
            // SPRITES!
            ModHooks.BeforeSavegameSaveHook += BeforeSaveGameSave;
            ModHooks.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.SavegameSaveHook += SaveGameSave;

            // Notches/HP
            ModHooks.NewGameHook += OnNewGame;

            // Panic Compass
            ModHooks.BeforeAddHealthHook += Health;
            ModHooks.TakeHealthHook += Health;

            // Don't hit walls w/ lances
            ModHooks.DoAttackHook += DoAttack;
            ModHooks.AfterAttackHook += AfterAttack;

            // Glass Soul
            ModHooks.TakeHealthHook += TakeHealth;

            // Disable Soul Gain 
            // Bloodlust
            ModHooks.SoulGainHook += SoulGain;

            // Soul Gen 
            // 753/56 Easter Egg 
            // Nailmaster's Passion 
            // Add Muzznik & DoubleKin Behaviours
            ModHooks.HeroUpdateHook += Update;

            // Beam Damage 
            // Timescale 
            // Panic Compass 
            // Tiny Shell
            ModHooks.CharmUpdateHook += CharmUpdate;

            // Custom Text
            ModHooks.LanguageGetHook += LangGet;

            // Lance Textures 
            // Canvas for Muzznik Text Soul Orb FSM
            USceneManager.sceneLoaded += SceneLoadedHook;

            _asm = Assembly.GetExecutingAssembly();
            Sprites = new Dictionary<string, Sprite>();

            Stopwatch overall = Stopwatch.StartNew();
            foreach (string res in _asm.GetManifestResourceNames())
            {
                if (!res.EndsWith(".png") && !res.EndsWith(".tex"))
                {
                    Log("Unknown resource: " + res);
                    continue;
                }

                using (Stream s = _asm.GetManifestResourceStream(res))
                {
                    if (s == null) continue;

                    byte[] buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Dispose();

                    // Create texture from bytes 
                    var tex = new Texture2D(2, 2);

                    tex.LoadImage(buffer, true);

                    // Create sprite from texture 
                    // Substring is to cut off the Lightbringer. and the .png 
                    Sprites.Add(res.Substring(23, res.Length - 27), Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));

                    Log("Created sprite from embedded image: " + res);
                }
            }

            Log("Finished loading all images in " + overall.ElapsedMilliseconds + "ms");
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
            {
                orig(self);
            }
        }

        private static void OnNewGame()
        {
            PlayerData.instance.maxHealthBase = PlayerData.instance.maxHealth = PlayerData.instance.health = 4;
            PlayerData.instance.charmSlots += 1;
        }


        private void Awake(On.ShopItemStats.orig_Awake orig, ShopItemStats self)
        {
            orig(self);

            string pdbool = self.playerDataBoolName;
            if (!pdbool.StartsWith("gotCharm_")) return;

            string key = "Charms." + pdbool.Substring(9, pdbool.Length - 9);
            if (Sprites.ContainsKey(key))
                Mirror.GetField<ShopItemStats, GameObject>(self, "itemSprite").GetComponent<SpriteRenderer>().sprite = Sprites[key];
        }

        private void AfterSaveGameLoad(SaveGameData data)
        {
            SaveGameSave();
            GameManager.instance.StartCoroutine(ChangeSprites());
        }

        private IEnumerator ChangeSprites()
        {
            while (CharmIconList.Instance                       == null ||
                   GameManager.instance                         == null ||
                   HeroController.instance                      == null ||
                   HeroController.instance.geoCounter           == null ||
                   HeroController.instance.geoCounter.geoSprite == null ||
                   Sprites.Count                                < 22)
                yield return null;

            foreach (int i in new int[] {2, 3, 4, 6, 8, 13, 14, 15, 18, 19, 20, 21, 25, 26, 35}) CharmIconList.Instance.spriteList[i] = Sprites["Charms." + i];

            HeroController.instance.geoCounter.geoSprite.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material.mainTexture = Sprites["UI"].texture;

            CharmIconList.Instance.unbreakableStrength = Sprites["Charms.ustr"];

            HeroController.instance.grubberFlyBeamPrefabL.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material
                          .mainTexture = Sprites["Lances"].texture;

            HeroController.instance.gameObject.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material
                          .mainTexture = Sprites["Knight"].texture;

            HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>()
                          .GetClipByName("Sprint")
                          .frames[0]
                          .spriteCollection.spriteDefinitions[0]
                          .material.mainTexture = Sprites["Sprint"].texture;

            GameObject inventory = GameManager.instance.inventoryFSM.gameObject;
            var invNailSprite = GameObjectUtil.Child(inventory, "Inv/Inv_Items/Nail").GetComponent<InvNailSprite>();

            invNailSprite.level1 = Sprites["LanceInv"];
            invNailSprite.level2 = Sprites["LanceInv"];
            invNailSprite.level3 = Sprites["LanceInv"];
            invNailSprite.level4 = Sprites["LanceInv"];
            invNailSprite.level5 = Sprites["LanceInv"];
        }


        private static void SaveGameSave(int id = 0)
        {
            PlayerData.instance.charmCost_21 = 1; // Faulty Wallet update patch
            PlayerData.instance.charmCost_19 = 4; // Eye of the Storm update patch
            PlayerData.instance.charmCost_15 = 3; // Bloodlust update patch
            PlayerData.instance.charmCost_14 = 2; // Glass Soul update patch
            PlayerData.instance.charmCost_8 = 3;  // Rising Light update patch
            PlayerData.instance.charmCost_35 = 5; // Radiant Jewel update patch
            PlayerData.instance.charmCost_18 = 3; // Silent Divide update patch
            PlayerData.instance.charmCost_3 = 2;  // Bloodsong update patch
            PlayerData.instance.charmCost_38 = 2; // Dreamshield update patch
        }

        private static void BeforeSaveGameSave(SaveGameData data = null)
        {
            // Don't ruin saves
            PlayerData.instance.charmCost_21 = 4;
            PlayerData.instance.charmCost_19 = 3;
            PlayerData.instance.charmCost_15 = 2;
            PlayerData.instance.charmCost_14 = 1;
            PlayerData.instance.charmCost_8 = 2;
            PlayerData.instance.charmCost_35 = 3;
            PlayerData.instance.charmCost_18 = 2;
            PlayerData.instance.charmCost_3 = 1;
            PlayerData.instance.charmCost_38 = 3; // Dreamshield update patch
            PlayerData.instance.nailDamage = PlayerData.instance.nailSmithUpgrades * 4 + 5;
        }

        private static int Health(int amount)
        {
            float panicSpeed = 1f;

            HeroController hc = HeroController.instance;

            if (PlayerData.instance.equippedCharm_2)
            {
                int missingHealth = PlayerData.instance.maxHealth -
                                    PlayerData.instance.health;
                panicSpeed += missingHealth                     * .03f;
                hc.RUN_SPEED = ORIG_RUN_SPEED                   * panicSpeed;
                hc.RUN_SPEED_CH = ORIG_RUN_SPEED_CH             * panicSpeed;
                hc.RUN_SPEED_CH_COMBO = ORIG_RUN_SPEED_CH_COMBO * panicSpeed;
            }
            else
            {
                hc.RUN_SPEED = ORIG_RUN_SPEED;
                hc.RUN_SPEED_CH = ORIG_RUN_SPEED_CH;
                hc.RUN_SPEED_CH_COMBO = ORIG_RUN_SPEED_CH_COMBO;
            }

            return amount;
        }

        private void DoAttack()
        {
            if (_origNailTerrainCheckTime == 0) _origNailTerrainCheckTime = Mirror.GetField<HeroController, float>(HeroController.instance, "NAIL_TERRAIN_CHECK_TIME");

            if (!(HeroController.instance.vertical_input < Mathf.Epsilon) &&
                !(HeroController.instance.vertical_input < -Mathf.Epsilon &&
                  HeroController.instance.hero_state != ActorStates.idle &&
                  HeroController.instance.hero_state != ActorStates.running))
                Mirror.SetField(HeroController.instance, "NAIL_TERRAIN_CHECK_TIME", 0f);
        }

        private void AfterAttack(AttackDirection dir)
        {
            Mirror.SetField(HeroController.instance, "NAIL_TERRAIN_CHECK_TIME", _origNailTerrainCheckTime);
        }

        private static void UpdateBlueHealth(On.PlayerData.orig_UpdateBlueHealth orig, PlayerData self)
        {
            self.SetInt("healthBlue", 0);
            if (self.GetBool("equippedCharm_9"))
                self.SetInt("healthBlue", self.GetInt("healthBlue") + 4);
        }

        private string LangGet(string key, string sheetTitle, string orig)
        {
            return _langDict.TryGetValue(key, out string val) ? val : Language.Language.GetInternal(key, sheetTitle);
        }

        private static void AddGeo(On.PlayerData.orig_AddGeo orig, PlayerData self, int amount)
        {
            // Don't let Faulty Wallet hurt people with full SOUL
            if (PlayerData.instance.equippedCharm_21 &&
                (PlayerData.instance.MPCharge  < PlayerData.instance.maxMP ||
                 PlayerData.instance.MPReserve != PlayerData.instance.MPReserveMax))
            {
                int lostGeo = (PlayerData.instance.maxMP - 1 - PlayerData.instance.MPCharge)  / 3 +
                              (PlayerData.instance.MPReserveMax - PlayerData.instance.MPReserve) / 3 + 1;
                HeroController.instance.AddMPChargeSpa(lostGeo > amount ? amount * 3 : lostGeo * 3);
                orig(self, lostGeo > amount ? 0 : amount - lostGeo);
            }
            else
            {
                orig(self, amount);
            }
        }

        private void CharmUpdate(PlayerData pd, HeroController self)
        {
            HeroController hc = HeroController.instance;

            // Charm Costs
            SaveGameSave();

            GameManager.instance.StartCoroutine(ChangeSprites());

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
                hc.RUN_SPEED = ORIG_RUN_SPEED;
                hc.RUN_SPEED_CH = ORIG_RUN_SPEED_CH;
                hc.RUN_SPEED_CH_COMBO = ORIG_RUN_SPEED_CH_COMBO;
            }

            pd.isInvincible = false;

            // Reset time to normal
            Time.timeScale = 1f;
            _timefracture = 1f;


            // BURNING PRIDE CALCULATIONS
            pd.nailDamage = Settings.BaseNailDamage + pd.nailSmithUpgrades * Settings.UpgradeNailDamage;
            if (pd.equippedCharm_13) // Mark of Pride
            {
                pd.CountGameCompletion();
                pd.nailDamage += (int) (pd.completionPercentage * Settings.BurningPrideScaleFactor);
            }

            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }

        private void SceneLoadedHook(Scene arg0, LoadSceneMode lsm)
        {
            // Without this your shade doesn't go away when you die.
            if (GameManager.instance == null) return;
            GameManager.instance.StartCoroutine(SceneLoaded(arg0));
        }

        private IEnumerator SceneLoaded(Scene arg0)
        {
            yield return null;
            yield return null;

            // Stop flickering soul orb
            FsmEvent a = GameManager.instance.soulOrb_fsm.FsmEvents.FirstOrDefault(x => x.Name == "MP GAIN");
            if (a != null) a.Name = "no";

            GameManager.instance.StartCoroutine(ChangeSprites());

            CreateCanvas();

            // Empress Muzznik
            if (arg0.name != "Crossroads_04" || PlayerData.instance.killedBigFly) yield break;

            PlayerData.instance.CountGameCompletion();

            if (PlayerData.instance.completionPercentage > 80)
                _textObj.text = "You are ready. Empress Muzznik awaits you.";
            else if (PlayerData.instance.completionPercentage > 60)
                _textObj.text = "You might just stand a chance...";
            else
                _textObj.text = "You are unworthy. Come back when you are stronger.";

            _textObj.CrossFadeAlpha(1f, 0f, false);
            _textObj.CrossFadeAlpha(0f, 7f, false);
        }

        private int SoulGain(int amount)
        {
            if (!PlayerData.instance.equippedCharm_15) return 0;
            _hitNumber++;
            if (_hitNumber != 5) return 0;
            HeroController.instance.AddHealth(1);
            _hitNumber = 0;
            _SpriteFlash.flash(Color.red, 0.7f, 0.45f, 0f, 0.45f);
            return 0;
        }

        private static void StartSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            orig(self);
            var slashFsm = Mirror.GetField<NailSlash, PlayMakerFSM>(self, "slashFsm");
            float slashAngle = slashFsm.FsmVariables.FindFsmFloat("direction").Value;
            var anim = Mirror.GetField<NailSlash, tk2dSpriteAnimator>(self, "anim");
            if (slashAngle == 0f || slashAngle == 180f)
            {
                self.transform.localScale = new Vector3(self.scale.x * 0.32f, self.scale.y * 0.32f, self.scale.z);
                self.transform.SetPositionZ(9999f);
                anim.Play(self.animName);
                return;
            }

            if (Mirror.GetField<NailSlash, bool>(self, "mantis")) // burning blade
            {
                self.transform.localScale = new Vector3(self.scale.x * 1.35f, self.scale.y * 1.35f, self.scale.z);
                anim.Play(self.animName + " F");
            }
            else
            {
                self.transform.localScale = self.scale;
                anim.Play(self.animName);
            }

            if (Mirror.GetField<NailSlash, bool>(self, "fury")) anim.Play(self.animName + " F");
        }

        private static int TakeHealth(int amount)
        {
            PlayerData.instance.ghostCoins = 1; // for timefracture

            if (!PlayerData.instance.equippedCharm_6) return amount;
            PlayerData.instance.health = 0;
            return 0;
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

        private void Update()
        {
            if (_timefracture < 1f || PlayerData.instance.ghostCoins == 1)
            {
                PlayerData.instance.ghostCoins = 0;
                _timefracture = 1f;
            }

            if (_timefracture > .99f && PlayerData.instance.equippedCharm_14 &&
                !HeroController.instance.cState.isPaused)
                Time.timeScale = _timefracture;

            // Double Kin
            if (_kin == null && (PlayerData.instance.geo == 753 || PlayerData.instance.geo == 56))
            {
                _kin = GameObject.Find("Lost Kin");
                if (_kin != null) _kin.AddComponent<DoubleKin>();
            }

            // EMPRESS MUZZNIK BOSS FIGHT
            if (_gruz == null)
            {
                _gruz = GameObject.Find("Giant Fly");
                if (_gruz != null && GameManager.instance.GetSceneNameString() == "Crossroads_04") _gruz.AddComponent<Muzznik>();
            }

            _manaRegenTime += Time.deltaTime * Time.timeScale;
            if (_manaRegenTime >= Settings.SoulRegenRate && GameManager.instance.soulOrb_fsm != null)
            {
                if (_SpriteFlash == null)
                {
                    _SpriteFlash = HeroController.instance.GetComponent<SpriteFlash>();
                }
                // Mana regen
                _manaRegenTime -= Settings.SoulRegenRate;
                HeroController.instance.AddMPChargeSpa(1);
                foreach (int i in new int[] {17, 19, 34, 30, 28, 22, 25})
                {
                    if (Mirror.GetField<PlayerData, bool>(PlayerData.instance, "equippedCharm_" + i) &&
                        (i != 25 || !PlayerData.instance.brokenCharm_25))
                        HeroController.instance.AddMPChargeSpa(1);
                }

                switch (PlayerData.instance.geo)
                {
                    // Easter Egg
                    case 753:
                        HeroController.instance.AddMPChargeSpa(3);
                        int num = Random.Next(1, 6);
                        switch (num)
                        {
                            case 1:
                                _SpriteFlash.flash(Color.green, 0.6f, 0.45f, 0f, 0.45f);
                                break;
                            case 2:
                                _SpriteFlash.flash(Color.red, 0.6f, 0.45f, 0f, 0.45f);
                                break;
                            case 3:
                                _SpriteFlash.flash(Color.magenta, 0.6f, 0.45f, 0f, 0.45f);
                                break;
                            case 4:
                                _SpriteFlash.flash(Color.yellow, 0.6f, 0.45f, 0f, 0.45f);
                                break;
                            default:
                                _SpriteFlash.flash(Color.blue, 0.6f, 0.45f, 0f, 0.45f);
                                break;
                        }

                        break;
                    case 56:
                        HeroController.instance.AddMPChargeSpa(3);
                        _SpriteFlash.flash(Color.black, 1.11f, 0f, 1.11f, 0f);
                        break;
                    default:
                        if (PlayerData.instance.equippedCharm_6)
                            _SpriteFlash.flash(Color.white, 0.6f, 0.45f, 0f, 0.45f);

                        break;
                }
            }

            if (!PlayerData.instance.equippedCharm_26) return;

            _passionTime += Time.deltaTime * Time.timeScale;
            if (_passionTime < 2f) return;

            _passionTime -= 2f;
            _passionDirection = !_passionDirection;

            float posX = (_passionDirection ? -1 : 1) * Random.Next(3, 12);
            if (!_passionDirection)
            {
                new AttackHandler().SpawnBeam
                (
                    _passionDirection,
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
                    _passionDirection,
                    1f,
                    1f,
                    posX,
                    0.5f - posX / 6f
                );
            }
            if (AttackHandler.BeamAudioClip == null)
            {
                GameObject BeamPrefabGameObject = Mirror.GetField<HeroController, GameObject>(HeroController.instance, "grubberFlyBeamPrefabU");
                AudioSource BeamAudio = BeamPrefabGameObject.GetComponent<AudioSource>();
                AttackHandler.BeamAudioClip = BeamAudio.clip;
            }
            Mirror.GetField<HeroController, AudioSource>(HeroController.instance, "audioSource").PlayOneShot(AttackHandler.BeamAudioClip, 0.1f);
        }
    }
}