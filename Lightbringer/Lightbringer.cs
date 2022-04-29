using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GlobalEnums;
using JetBrains.Annotations;
using Modding;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;
using IntCompare = On.HutongGames.PlayMaker.Actions.IntCompare;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

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

        GameObject OrbPrefab;
        GameObject[] Orbs = { null, null };
        GameObject ShotCharge;
        GameObject ShotCharge2;
        GameObject BeamSweeper;
        GameObject HKBlast;
        GameObject SpikePrefab;
        GameObject SpikeCenter;
        List<GameObject> Spikes = new List<GameObject>();

        public LightbringerSettings Settings = new LightbringerSettings();
        public void OnLoadGlobal(LightbringerSettings s) => Settings = s;
        public LightbringerSettings OnSaveGlobal() => Settings;

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance = this;
            GetPrefabs(preloadedObjects);
            SetupSpells();
            RegisterCallbacks();
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

            Log("Finished loading images");
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

        private void GetPrefabs(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            var radiance = preloadedObjects["GG_Radiance"]["Boss Control/Absolute Radiance"];
            var fsm = radiance.LocateMyFSM("Attack Commands");
            var spawnAction = fsm.GetAction<SpawnObjectFromGlobalPool>("Spawn Fireball", 1);
            OrbPrefab = Object.Instantiate(spawnAction.gameObject.Value, null);
            Object.DontDestroyOnLoad(OrbPrefab);
            OrbPrefab.SetActive(false);
            ShotCharge = radiance.transform.Find("Shot Charge").gameObject;
            ShotCharge2 = radiance.transform.Find("Shot Charge 2").gameObject;
            var finalcontrol = OrbPrefab.LocateMyFSM("Final Control");
            Object.DestroyImmediate(finalcontrol);
            var herohurter = OrbPrefab.transform.Find("Hero Hurter").GetComponent<DamageHero>();
            Object.DestroyImmediate(herohurter);
            BeamSweeper = preloadedObjects["GG_Radiance"]["Boss Control/Beam Sweeper"];
            BeamSweeper.transform.SetParent(null);

            HKBlast = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/Focus Blasts/HK Prime Blast"];

            SpikePrefab = preloadedObjects["GG_Radiance"]["Boss Control/Spike Control/Far L/Radiant Spike"];
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
                ReflectionHelper.GetField<ShopItemStats, GameObject>(self, "itemSprite").GetComponent<SpriteRenderer>().sprite = Sprites[key];
        }

        private IEnumerator WaitHero(Action a)
        {
            yield return new WaitWhile(() => HeroController.instance == null);
            yield return new WaitForSeconds(0.3f);
            a?.Invoke();
        }

        private void AfterSaveGameLoad(SaveGameData data)
        {
            SaveGameSave();
            GameManager.instance.StartCoroutine(ChangeSprites());
            GameManager.instance.StartCoroutine(WaitHero(() => SetupActions()));
        }

        private IEnumerator ChangeSprites()
        {
            while (CharmIconList.Instance == null ||
                   GameManager.instance == null ||
                   HeroController.instance == null ||
                   HeroController.instance.geoCounter == null ||
                   HeroController.instance.geoCounter.geoSprite == null ||
                   Sprites.Count < 22)
                yield return null;

            foreach (int i in new int[] { 2, 3, 4, 6, 8, 11, 13, 14, 15, 16, 17, 18, 19, 20, 21, 25, 26, 35 }) CharmIconList.Instance.spriteList[i] = Sprites["Charms." + i];

            HeroController.instance.geoCounter.geoSprite.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material.mainTexture = Sprites["UI"].texture;

            CharmIconList.Instance.unbreakableStrength = Sprites["Charms.ustr"];

            GameObject.Find("/_GameCameras/HudCamera/Inventory/Charms/Collected Charms/25")
                .LocateMyFSM("charm_show_if_collected")
                .GetAction<SetSpriteRendererSprite>("Glass Attack", 2)
                .sprite.Value = Sprites["Charms.brokestr"];
            GameObject.Find("/_GameCameras/HudCamera/Inventory/Charms/Details/Detail Sprite")
                .LocateMyFSM("Update Sprite")
                .GetAction<SetSpriteRendererSprite>("Glass Attack", 2)
                .sprite.Value = Sprites["Charms.brokestr"];

            HeroController.instance.grubberFlyBeamPrefabL.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material.mainTexture = Sprites["Lances"].texture;

            HeroController.instance.gameObject.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material.mainTexture = Sprites["Knight"].texture;

            HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>()
                          .GetClipByName("Sprint")
                          .frames[0]
                          .spriteCollection.spriteDefinitions[0]
                          .material.mainTexture = Sprites["Sprint"].texture;

            GameObject.Find("/Knight/Effects/Shadow Dash Blobs")
                .GetComponent<ParticleSystemRenderer>()
                .material.mainTexture = Sprites["Void"].texture;

            foreach (Transform child in HeroController.instance.transform)
                if (child.name == "Spells")
                    foreach (Transform spellsChild in child)
                        if (spellsChild.name == "Scr Heads 2" || spellsChild.name == "Scr Base 2")
                            spellsChild.gameObject.GetComponent<tk2dSprite>()
                                .GetCurrentSpriteDef()
                                .material.mainTexture = Sprites["VoidSpells"].texture;

            var invNail = GameObject.Find("/_GameCameras/HudCamera/Inventory/Inv/Inv_Items/Nail");
            var invNailSprite = invNail.GetComponent<InvNailSprite>();

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
                panicSpeed += missingHealth * .03f;
                hc.RUN_SPEED = ORIG_RUN_SPEED * panicSpeed;
                hc.RUN_SPEED_CH = ORIG_RUN_SPEED_CH * panicSpeed;
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

        private void SetupActions()
        {
            var spellctrl = HeroController.instance.spellControl;
            spellctrl.AddAction("Focus", () => {
                if (PlayerData.instance.equippedCharm_17)
                {
                    HKBlast.transform.position = HeroController.instance.transform.position;
                    HKBlast.LocateMyFSM("Control").SetState("Blast");
                }
            });
            spellctrl.ReplaceTransition("Focus", "FOCUS COMPLETED", "Set HP Amount");

            spellctrl.ReplaceAction("Scream Burst 2", 8, () => {
                BeamSweeper.LocateMyFSM("Control").SetState("Beam Sweep R 2");
            });
            spellctrl.RemoveAction("Scream Burst 2", 3);
            spellctrl.RemoveAction("Scream Burst 2", 1);
            spellctrl.RemoveAction("Scream Burst 2", 0);

            spellctrl.ReplaceAction("Fireball 1", 3, SpawnFireball);

            spellctrl.ReplaceAction("Fireball 2", 3, SpawnOrb);
            spellctrl.ReplaceTransition("Fireball 2", "FINISHED", "Spell End");

            spellctrl.AddAction("Q2 Land", SpawnSpike);

            spellctrl.Fsm.SaveActions();
        }

        private void SetupSpells()
        {
            GameManager.instance.StartCoroutine(WaitHero(() => {
                SetupOrb();
                SetupBeam();
                SetupBlast();
                SetupSpike();
            }));
        }
        private void SetupOrb()
        {
            var orb = OrbPrefab;
            orb.layer = 17;   // PhysLayers.HERO_ATTACK

            var orbcontrol = orb.LocateMyFSM("Orb Control");

            orbcontrol.AddState("Chase Enemy");

            orbcontrol.ReplaceTransition("Init", "FIRE", "Chase Enemy");
            orbcontrol.ReplaceTransition("Init", "FINISHED", "Chase Enemy");

            orbcontrol.AddTransition("Chase Enemy", "ORBHIT", "Impact pause");
            orbcontrol.AddTransition("Chase Enemy", "DISSIPATE", "Dissipate");

            orbcontrol.RemoveState("Orbiting");
            orbcontrol.RemoveState("Chase Hero");

            orbcontrol.RemoveAction("Init", 2); // SetScale

            orbcontrol.AddAction("Chase Enemy", new Trigger2dEventLayer
            {
                trigger = PlayMakerUnity2d.Trigger2DType.OnTriggerEnter2D,
                collideLayer = 11,
                sendEvent = FsmEvent.GetFsmEvent("ORBHIT"),
                collideTag = "",
                storeCollider = new FsmGameObject()
            });
            orbcontrol.AddAction("Chase Enemy", new Trigger2dEventLayer
            {
                trigger = PlayMakerUnity2d.Trigger2DType.OnTriggerStay2D,
                collideLayer = 11,
                sendEvent = FsmEvent.GetFsmEvent("ORBHIT"),
                collideTag = "",
                storeCollider = new FsmGameObject()
            });
            orbcontrol.AddAction("Chase Enemy", new Wait
            {
                time = 3.5f,
                finishEvent = FsmEvent.GetFsmEvent("DISSIPATE")
            });

            orbcontrol.GetAction<Wait>("Impact", 7).time = 0.1f;
            orbcontrol.Fsm.SaveActions();
        }
        private void SetupBlast()
        {
            HKBlast.transform.position = HeroController.instance.transform.position;
            GameObject blast;
            var fsm = HKBlast.LocateMyFSM("Control");
            var blastAction = fsm.GetAction<ActivateGameObject>("Blast", 0);
            blast = Object.Instantiate(blastAction.gameObject.GameObject.Value);
            blast.name = "MyBlast";
            Object.DontDestroyOnLoad(blast);
            blast.transform.SetParent(HKBlast.transform);
            blast.transform.localPosition = new Vector3(0, 0, 0);
            blast.SetActive(false);
            var damager = blast.transform.Find("hero_damager");
            Object.DestroyImmediate(damager.GetComponent<DamageHero>());
            HKBlast.layer = (int)PhysLayers.HERO_ATTACK;
            blast.layer = (int)PhysLayers.HERO_ATTACK;
            damager.gameObject.layer = (int)PhysLayers.HERO_ATTACK;

            SetDamageEnemy(damager.gameObject, 20).circleDirection = true;

            blastAction.gameObject.GameObject.Value = blast;
            fsm.Fsm.SaveActions();

            var hkblastfsm = HKBlast.LocateMyFSM("Control");
            hkblastfsm.AddAction("Blast", () => {
                Vector3 scale = new Vector3(1, 1, 1);
                MaterialPropertyBlock prop = new MaterialPropertyBlock();
                if (PlayerData.instance.equippedCharm_34)
                    scale *= 3;

                HKBlast.transform.localScale = scale;

                foreach (Transform t in blast.transform)
                {
                    var render = t.GetComponent<SpriteRenderer>();
                    if (render != null)
                        render.SetPropertyBlock(prop);
                }

            });
            var idle = hkblastfsm.GetState("Idle");
            idle.Transitions = new FsmTransition[] { };
            hkblastfsm.Fsm.SaveActions();
            HKBlast.SetActive(true);
        }
        private void SetupBeam()
        {
            var BeamControl = BeamSweeper.LocateMyFSM("Control");

            var SpawnBeam = BeamControl.GetAction<SpawnObjectFromGlobalPoolOverTime>("Beam Sweep R 2", 5);
            var BeamPrefab = Object.Instantiate(SpawnBeam.gameObject.Value);
            Object.DontDestroyOnLoad(BeamPrefab);
            BeamPrefab.SetActive(false);
            Object.DestroyImmediate(BeamPrefab.GetComponent<DamageHero>());
            SpawnBeam.gameObject.Value = BeamPrefab;
            BeamSweeper.layer = (int)PhysLayers.HERO_ATTACK;
            BeamPrefab.layer = (int)PhysLayers.HERO_ATTACK;
            var MySpawnBeam = new SpawnObjects
            {
                gameObject = SpawnBeam.gameObject,
                spawnPoint = SpawnBeam.spawnPoint,
                position = new Vector3(0, 0, 0),
                rotation = new Vector3(0, 0, 0),
                frequency = 0.075f,
                initialize = (GameObject beam) =>
                {
                    var damage = SetDamageEnemy(beam, PlayerData.instance.equippedCharm_19 ? 12 : 8);
                    damage.direction = 90;
                }
            };

            BeamControl.ReplaceAction("Beam Sweep R 2", 4, () => {
                if (HeroController.instance != null)
                {
                    Vector3 position = HeroController.instance.transform.position;
                    position.y -= 10;
                    position.x -= 30;
                    BeamSweeper.transform.position = position;
                }
            });
            BeamControl.ReplaceAction("Beam Sweep R 2", 5, MySpawnBeam);

            BeamControl.GetAction<iTweenMoveBy>("Beam Sweep R 2", 6).vector = new Vector3(0, 50, 0);

            var idle = BeamControl.GetState("Idle");
            idle.Transitions = new FsmTransition[] { };
            BeamControl.Fsm.SaveActions();
            BeamSweeper.SetActive(true);
        }
        private void SetupSpike()
        {
            Object.DestroyImmediate(SpikePrefab.LocateMyFSM("Hero Saver"));
            Object.DestroyImmediate(SpikePrefab.GetComponent<DamageHero>());

            var SpikeControl = SpikePrefab.LocateMyFSM("Control");
            SpikeControl.RemoveTransition("Up", "DOWN");
            SpikeControl.RemoveTransition("Up", "SPIKES DOWN");

            SpikeControl.GetAction<Wait>("Floor Antic", 2).time = 0.4f;
            SpikeControl.AddAction("Up", new Wait { time = 0.8f, finishEvent = FsmEvent.Finished });

            SpikeControl.GetState("Downed").Transitions = new FsmTransition[] { };
            SpikeControl.GetState("Floor Antic").Transitions = new FsmTransition[] { new FsmTransition { FsmEvent = FsmEvent.Finished, ToState = "Spike Up" } };
            SpikeControl.GetState("Spike Up").Transitions = new FsmTransition[] { new FsmTransition { FsmEvent = FsmEvent.Finished, ToState = "Up" } };
            SpikeControl.GetState("Up").Transitions = new FsmTransition[] { new FsmTransition { FsmEvent = FsmEvent.Finished, ToState = "Down" } };
            SpikeControl.AddTransition("Downed", "HEROSPIKEUP", "Floor Antic");

            SpikeControl.Fsm.SaveActions();
            SpikeCenter = new GameObject { name = "HeroSpikeCenter", layer = 23 };
            SpikeCenter.SetActive(true);
            Object.DontDestroyOnLoad(SpikeCenter);
            SpikeCenter.transform.position = HeroController.instance.transform.position;
        }

        private IEnumerator SpawnFireball()
        {
            Vector3 position = HeroController.instance.transform.position + (PlayerData.instance.equippedCharm_4 ? new Vector3(0f, .6f) : new Vector3(0f, .3f));
            var fireballCast = HeroController.instance.spell1Prefab.Spawn(position).LocateMyFSM("Fireball Cast");

            fireballCast.GetAction<PlayerDataBoolTest>("Cast Right", 5).isTrue = FsmEvent.GetFsmEvent("");
            fireballCast.GetAction<PlayerDataBoolTest>("Cast Left", 2).isTrue = FsmEvent.GetFsmEvent("");

            if (PlayerData.instance.equippedCharm_11)
            {
                yield return new WaitForSeconds(0.1f);
                
                GameObject fireball = fireballCast.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).gameObject.Value;
                position = HeroController.instance.transform.position - (PlayerData.instance.equippedCharm_4 ? new Vector3(0f, .6f) : new Vector3(0f, .3f));
                var miniball = fireball.Spawn(position);

                Vector2 velocity = new Vector2(fireballCast.FsmVariables.GetFsmFloat("Fire Speed").Value, 0);
                velocity *= HeroController.instance.cState.facingRight ? 1 : -1;
                miniball.GetComponent<Rigidbody2D>().velocity = velocity;
            }
        }
        public IEnumerator SpawnOrb()
        {
            for (int i = 0; i < (PlayerData.instance.equippedCharm_11 ? 2 : 1); i++)
            {
                var position = new Vector3(HeroController.instance.transform.position.x + UnityEngine.Random.Range(-2, 2), HeroController.instance.transform.position.y + 2 + UnityEngine.Random.Range(-3, 2));
                var ShotCharge = Object.Instantiate(this.ShotCharge);
                var ShotCharge2 = Object.Instantiate(this.ShotCharge2);
                ShotCharge.transform.position = position;
                ShotCharge2.transform.position = position;
                ShotCharge.SetActive(true);
                ShotCharge2.SetActive(true);
                var Emitter = ShotCharge.GetComponent<ParticleSystem>().emission;
                var Emitter2 = ShotCharge2.GetComponent<ParticleSystem>().emission;
                Emitter.enabled = true;
                Emitter2.enabled = true;

                yield return new WaitForSeconds(0.2f);

                if (Orbs[i]?.transform?.parent?.name != "GlobalPool")
                    Object.Destroy(Orbs[i]);

                var Orb = OrbPrefab.Spawn();
                Orb.transform.position = position;
                float size = PlayerData.instance.equippedCharm_19 ? 1.7f : 1.3f;
                Orb.transform.localScale = new Vector3(size, size, 1);
                SetDamageEnemy(Orb, PlayerData.instance.equippedCharm_19 ? 25 : 15);
                Orb.AddComponent<OrbChaseObject>();
                Orb.SetActive(true);

                Orbs[i] = Orb;

                Emitter.enabled = false;
                Emitter2.enabled = false;

                yield return new WaitForSeconds(0.3f);
            }
        }
        private void SpawnSpike()
        {
            int count = 10;
            float spacing = 0.8f;
            int damage = 5;
            Vector3 scale = new Vector3(1.0f, 0.7f, 0.9f);
            Vector3 position = HeroController.instance.transform.position + 0.1f * Vector3.up;
            if (PlayerData.instance.equippedCharm_19)
            {
                count += 3;
                damage += 5;
                scale.y *= 1.5f;
                scale.x *= 1.2f;
                spacing *= 1.2f;
                position.y += 0.6f;
            }
            SpikePrefab.transform.localScale = scale;
            SetDamageEnemy(SpikePrefab, damage);
            SpikeCenter.transform.position = position;

            AddSpikeToPool(count, spacing);
            foreach (var Spike in Spikes)
                Spike.LocateMyFSM("Control").SendEvent("HEROSPIKEUP");
        }
        private bool AddSpikeToPool(int n = 10, float spacing = 0.8f)
        {
            foreach (var Spike in Spikes)
                Object.Destroy(Spike);
            Spikes.Clear();

            float x = -1 * (n * spacing / 2);
            for (int i = 0; i < n; i++)
            {
                GameObject Spike = Object.Instantiate(SpikePrefab);
                Spike.transform.SetParent(SpikeCenter.transform);
                Spike.transform.localPosition = new Vector3(x, -0.4f, 0);
                x += spacing;
                Spikes.Add(Spike);
                Spike.SetActive(true);
            }
            return true;
        }

        private void DoAttack()
        {
            if (_origNailTerrainCheckTime == 0)
                _origNailTerrainCheckTime = ReflectionHelper.GetField<HeroController, float>(HeroController.instance, "NAIL_TERRAIN_CHECK_TIME");

            if (!(HeroController.instance.vertical_input < Mathf.Epsilon) &&
                !(HeroController.instance.vertical_input < -Mathf.Epsilon &&
                  HeroController.instance.hero_state != ActorStates.idle &&
                  HeroController.instance.hero_state != ActorStates.running))
                ReflectionHelper.SetField(HeroController.instance, "NAIL_TERRAIN_CHECK_TIME", 0f);
        }

        private void AfterAttack(AttackDirection dir)
        {
            ReflectionHelper.SetField(HeroController.instance, "NAIL_TERRAIN_CHECK_TIME", _origNailTerrainCheckTime);
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

        public DamageEnemies SetDamageEnemy(GameObject go, int damage = 0)
        {
            var dmg = go.GetComponent<DamageEnemies>() ?? go.AddComponent<DamageEnemies>();
            dmg.attackType = AttackTypes.Spell;
            dmg.circleDirection = false;
            dmg.damageDealt = damage;
            dmg.direction = 90 * 3;
            dmg.ignoreInvuln = false;
            dmg.magnitudeMult = 1f;
            dmg.moveDirection = false;
            dmg.specialType = 0;

            return dmg;
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
            pd.nailDamage = Settings.NailDamage + pd.nailSmithUpgrades * Settings.NailUpgradeBonus;
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
            GameManager.instance.StartCoroutine(ChangeSprites());

            CreateCanvas();

            if (!Settings.EmpressMuzznik || arg0.name != "Crossroads_04" || PlayerData.instance.killedBigFly) yield break;

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

            if (_timefracture > .99f && PlayerData.instance.equippedCharm_14 && !HeroController.instance.cState.isPaused)
                Time.timeScale = _timefracture;

            // Double Kin
            if (Settings.DoubleKin && _kin == null)
            {
                _kin = GameObject.Find("Lost Kin");
                if (_kin != null) _kin.AddComponent<DoubleKin>();
            }

            // EMPRESS MUZZNIK BOSS FIGHT
            if (Settings.EmpressMuzznik && _gruz == null)
            {
                _gruz = GameObject.Find("Giant Fly");
                if (_gruz != null && GameManager.instance.GetSceneNameString() == "Crossroads_04") _gruz.AddComponent<Muzznik>();
            }

            _manaRegenTime += Time.deltaTime * Time.timeScale;
            if (_manaRegenTime >= Settings.SoulRegenRate && GameManager.instance.soulOrb_fsm != null)
            {
                if (_SpriteFlash == null)
                    _SpriteFlash = HeroController.instance.GetComponent<SpriteFlash>();
                // Mana regen
                _manaRegenTime -= Settings.SoulRegenRate;
                HeroController.instance.AddMPChargeSpa(1);
                foreach (int i in new int[] {17, 19, 34, 30, 28, 22, 25})
                    if (ReflectionHelper.GetField<PlayerData, bool>(PlayerData.instance, "equippedCharm_" + i))
                        if (i != 25 || !PlayerData.instance.brokenCharm_25)
                            HeroController.instance.AddMPChargeSpa(1);
            }

            #region Nailmaster's Passion

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
                GameObject BeamPrefabGameObject = ReflectionHelper.GetField<HeroController, GameObject>(HeroController.instance, "grubberFlyBeamPrefabU");
                AudioSource BeamAudio = BeamPrefabGameObject.GetComponent<AudioSource>();
                AttackHandler.BeamAudioClip = BeamAudio.clip;
            }
            ReflectionHelper.GetField<HeroController, AudioSource>(HeroController.instance, "audioSource").PlayOneShot(AttackHandler.BeamAudioClip, 0.1f);
            
            #endregion
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Radiance","Boss Control/Absolute Radiance"),
                ("GG_Radiance","Boss Control/Beam Sweeper"),
                ("GG_Radiance","Boss Control/Spike Control/Far L/Radiant Spike"),
                ("GG_Hollow_Knight","Battle Scene/Focus Blasts/HK Prime Blast"),
            };
        }
    }
}