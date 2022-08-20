using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace Lightbringer
{
    public partial class Lightbringer
    {
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

    internal static class ChildOfLight
    {
        static Coroutine task;
        static GameObject orbPrefab;
        static GameObject[] orbs = { null, null };
        static GameObject shotCharge;
        static GameObject shotCharge2;
        static GameObject beamSweeper;
        static GameObject blastPrefab;
        static GameObject spikePrefab;
        static GameObject spikeCenter;
        static List<GameObject> spikes = new List<GameObject>();

        internal static void Launch()
        {
            if (task != null)
                GameManager.instance.StopCoroutine(task);
            task = GameManager.instance.StartCoroutine(Enable());
        }

        internal static void Cancel()
        {
            if (task != null)
                GameManager.instance.StopCoroutine(task);
            if (HeroController.instance != null)
                Disable();
        }

        private static IEnumerator Enable()
        {
            while (HeroController.instance == null)
                yield return null;

            var spellControl = HeroController.instance.spellControl;
            spellControl.AddAction("Focus", () => {
                if (PlayerData.instance.equippedCharm_17)
                {
                    blastPrefab.transform.position = HeroController.instance.transform.position;
                    blastPrefab.LocateMyFSM("Control").SetState("Blast");
                }
            });
            spellControl.ReplaceTransition("Focus", "FOCUS COMPLETED", "Set HP Amount");

            spellControl.ReplaceAction("Scream Burst 2", 8, () => {
                beamSweeper.LocateMyFSM("Control").SetState("Beam Sweep R 2");
            });
            spellControl.RemoveAction("Scream Burst 2", 3);
            spellControl.RemoveAction("Scream Burst 2", 1);
            spellControl.RemoveAction("Scream Burst 2", 0);

            spellControl.ReplaceAction("Fireball 1", 3, SpawnFireball);

            spellControl.ReplaceAction("Fireball 2", 3, SpawnOrb);
            spellControl.ReplaceTransition("Fireball 2", "FINISHED", "Spell End");

            spellControl.AddAction("Q2 Land", SpawnSpike);
        }

        private static void Disable()
        {
            var spellControl = HeroController.instance.spellControl;
            spellControl.Fsm.Reinitialize();
            spellControl.ReplaceTransition("Focus", "FOCUS COMPLETED", "Spore Cloud");
            spellControl.ReplaceTransition("Fireball 2", "FINISHED", "Fireball Recoil");
        }

        internal static void Setup(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            var radiance = preloadedObjects["GG_Radiance"]["Boss Control/Absolute Radiance"];
            var radianceAttacks = radiance.LocateMyFSM("Attack Commands");
            var spawnAction = radianceAttacks.GetAction<SpawnObjectFromGlobalPool>("Spawn Fireball", 1);
            orbPrefab = Object.Instantiate(spawnAction.gameObject.Value, null);
            shotCharge = radiance.transform.Find("Shot Charge").gameObject;
            shotCharge2 = radiance.transform.Find("Shot Charge 2").gameObject;
            SetupOrb();

            beamSweeper = preloadedObjects["GG_Radiance"]["Boss Control/Beam Sweeper"];
            beamSweeper.transform.SetParent(null);
            SetupBeam();

            blastPrefab = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/Focus Blasts/HK Prime Blast"];
            SetupBlast();

            spikePrefab = preloadedObjects["GG_Radiance"]["Boss Control/Spike Control/Far L/Radiant Spike"];
            SetupSpike();
        }

        private static void SetupOrb()
        {
            Object.DestroyImmediate(orbPrefab.LocateMyFSM("Final Control"));
            Object.DestroyImmediate(orbPrefab.transform.Find("Hero Hurter").GetComponent<DamageHero>());

            orbPrefab.layer = (int)PhysLayers.HERO_ATTACK;

            var orbControl = orbPrefab.LocateMyFSM("Orb Control");

            orbControl.AddState("Chase Enemy");

            orbControl.ReplaceTransition("Init", "FIRE", "Chase Enemy");
            orbControl.ReplaceTransition("Init", "FINISHED", "Chase Enemy");

            orbControl.AddTransition("Chase Enemy", "ORBHIT", "Impact pause");
            orbControl.AddTransition("Chase Enemy", "DISSIPATE", "Dissipate");

            orbControl.RemoveState("Orbiting");
            orbControl.RemoveState("Chase Hero");

            orbControl.RemoveAction("Init", 2); // SetScale

            orbControl.AddAction("Chase Enemy", new Trigger2dEventLayer
            {
                trigger = PlayMakerUnity2d.Trigger2DType.OnTriggerEnter2D,
                collideLayer = 11,
                sendEvent = FsmEvent.GetFsmEvent("ORBHIT"),
                collideTag = "",
                storeCollider = new FsmGameObject()
            });
            orbControl.AddAction("Chase Enemy", new Trigger2dEventLayer
            {
                trigger = PlayMakerUnity2d.Trigger2DType.OnTriggerStay2D,
                collideLayer = 11,
                sendEvent = FsmEvent.GetFsmEvent("ORBHIT"),
                collideTag = "",
                storeCollider = new FsmGameObject()
            });
            orbControl.AddAction("Chase Enemy", new Wait
            {
                time = 3.5f,
                finishEvent = FsmEvent.GetFsmEvent("DISSIPATE")
            });

            orbControl.GetAction<Wait>("Impact", 7).time = 0.1f;

            orbControl.Fsm.SaveActions();
            Object.DontDestroyOnLoad(orbPrefab);
            orbPrefab.SetActive(false);
        }

        private static void SetupBlast()
        {
            GameObject blast;
            var blastControl = blastPrefab.LocateMyFSM("Control");
            var blastAction = blastControl.GetAction<ActivateGameObject>("Blast", 0);
            blast = Object.Instantiate(blastAction.gameObject.GameObject.Value);
            blast.name = "MyBlast";
            Object.DontDestroyOnLoad(blast);
            blast.transform.SetParent(blastPrefab.transform);
            blast.transform.localPosition = new Vector3(0, 0, 0);
            blast.SetActive(false);
            var damager = blast.transform.Find("hero_damager");
            Object.DestroyImmediate(damager.GetComponent<DamageHero>());
            blastPrefab.layer = (int)PhysLayers.HERO_ATTACK;
            blast.layer = (int)PhysLayers.HERO_ATTACK;
            damager.gameObject.layer = (int)PhysLayers.HERO_ATTACK;

            SetDamageEnemy(damager.gameObject, 20).circleDirection = true;

            blastAction.gameObject.GameObject.Value = blast;
            blastControl.Fsm.SaveActions();

            var blastFsm = blastPrefab.LocateMyFSM("Control");
            blastFsm.AddAction("Blast", () => {
                Vector3 scale = new Vector3(1, 1, 1);
                MaterialPropertyBlock prop = new MaterialPropertyBlock();
                if (PlayerData.instance.equippedCharm_34)
                    scale *= 3;
                blastPrefab.transform.localScale = scale;
                foreach (Transform transform in blast.transform)
                {
                    var render = transform.GetComponent<SpriteRenderer>();
                    if (render != null)
                        render.SetPropertyBlock(prop);
                }
            });
            blastFsm.GetState("Idle").Transitions = new FsmTransition[] { };

            blastFsm.Fsm.SaveActions();
            blastPrefab.SetActive(true);
        }

        private static void SetupBeam()
        {
            var beamControl = beamSweeper.LocateMyFSM("Control");

            var spawnBeam = beamControl.GetAction<SpawnObjectFromGlobalPoolOverTime>("Beam Sweep R 2", 5);
            var beamPrefab = Object.Instantiate(spawnBeam.gameObject.Value);
            Object.DontDestroyOnLoad(beamPrefab);
            beamPrefab.SetActive(false);
            Object.DestroyImmediate(beamPrefab.GetComponent<DamageHero>());
            spawnBeam.gameObject.Value = beamPrefab;
            beamSweeper.layer = (int)PhysLayers.HERO_ATTACK;
            beamPrefab.layer = (int)PhysLayers.HERO_ATTACK;
            var customSpawnBeam = new SpawnObjects
            {
                gameObject = spawnBeam.gameObject,
                spawnPoint = spawnBeam.spawnPoint,
                position = new Vector3(0, 0, 0),
                rotation = new Vector3(0, 0, 0),
                frequency = 0.075f,
                initialize = (GameObject beam) =>
                {
                    var damage = SetDamageEnemy(beam, PlayerData.instance.equippedCharm_19 ? 12 : 8);
                    damage.direction = 90;
                }
            };

            beamControl.ReplaceAction("Beam Sweep R 2", 4, () => {
                if (HeroController.instance != null)
                {
                    Vector3 position = HeroController.instance.transform.position;
                    position.y -= 10;
                    position.x -= 30;
                    beamSweeper.transform.position = position;
                }
            });
            beamControl.ReplaceAction("Beam Sweep R 2", 5, customSpawnBeam);

            beamControl.GetAction<iTweenMoveBy>("Beam Sweep R 2", 6).vector = new Vector3(0, 50, 0);

            var idle = beamControl.GetState("Idle");
            idle.Transitions = new FsmTransition[] { };

            beamControl.Fsm.SaveActions();
            beamSweeper.SetActive(true);
        }

        private static void SetupSpike()
        {
            Object.DestroyImmediate(spikePrefab.LocateMyFSM("Hero Saver"));
            Object.DestroyImmediate(spikePrefab.GetComponent<DamageHero>());

            var spikeControl = spikePrefab.LocateMyFSM("Control");
            spikeControl.RemoveTransition("Up", "DOWN");
            spikeControl.RemoveTransition("Up", "SPIKES DOWN");

            spikeControl.GetAction<Wait>("Floor Antic", 2).time = 0.4f;
            spikeControl.AddAction("Up", new Wait { time = 0.8f, finishEvent = FsmEvent.Finished });

            spikeControl.GetState("Downed").Transitions = new FsmTransition[] { };
            spikeControl.GetState("Floor Antic").Transitions = new FsmTransition[] { new FsmTransition { FsmEvent = FsmEvent.Finished, ToState = "Spike Up" } };
            spikeControl.GetState("Spike Up").Transitions = new FsmTransition[] { new FsmTransition { FsmEvent = FsmEvent.Finished, ToState = "Up" } };
            spikeControl.GetState("Up").Transitions = new FsmTransition[] { new FsmTransition { FsmEvent = FsmEvent.Finished, ToState = "Down" } };
            spikeControl.AddTransition("Downed", "HEROSPIKEUP", "Floor Antic");

            spikeControl.Fsm.SaveActions();

            spikeCenter = new GameObject { name = "HeroSpikeCenter", layer = 23 };
            Object.DontDestroyOnLoad(spikeCenter);
        }

        private static IEnumerator SpawnFireball()
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

        public static IEnumerator SpawnOrb()
        {
            for (int i = 0; i < (PlayerData.instance.equippedCharm_11 ? 2 : 1); i++)
            {
                var position = new Vector3(HeroController.instance.transform.position.x + UnityEngine.Random.Range(-2, 2), HeroController.instance.transform.position.y + 2 + UnityEngine.Random.Range(-3, 2));
                var shotCharge = Object.Instantiate(ChildOfLight.shotCharge);
                var shotCharge2 = Object.Instantiate(ChildOfLight.shotCharge2);
                shotCharge.transform.position = position;
                shotCharge2.transform.position = position;
                shotCharge.SetActive(true);
                shotCharge2.SetActive(true);
                var emitter = shotCharge.GetComponent<ParticleSystem>().emission;
                var emitter2 = shotCharge2.GetComponent<ParticleSystem>().emission;
                emitter.enabled = true;
                emitter2.enabled = true;

                yield return new WaitForSeconds(0.2f);

                if (orbs[i] && orbs[i].transform?.parent?.name != "GlobalPool")
                    Object.Destroy(orbs[i]);

                var orb = orbPrefab.Spawn();
                orb.transform.position = position;
                float size = PlayerData.instance.equippedCharm_19 ? 1.7f : 1.3f;
                orb.transform.localScale = new Vector3(size, size, 1);
                SetDamageEnemy(orb, PlayerData.instance.equippedCharm_19 ? 25 : 15);
                orb.AddComponent<OrbChase>();
                orb.SetActive(true);

                orbs[i] = orb;

                emitter.enabled = false;
                emitter2.enabled = false;

                yield return new WaitForSeconds(0.3f);
            }
        }

        private static void SpawnSpike()
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
            spikePrefab.transform.localScale = scale;
            SetDamageEnemy(spikePrefab, damage);
            spikeCenter.transform.position = position;

            AddSpikeToPool(count, spacing);
            foreach (var spike in spikes)
                spike.LocateMyFSM("Control").SendEvent("HEROSPIKEUP");
        }

        private static bool AddSpikeToPool(int n = 10, float spacing = 0.8f)
        {
            foreach (var spike in spikes)
                Object.Destroy(spike);
            spikes.Clear();

            float x = -1 * (n * spacing / 2);
            for (int i = 0; i < n; i++)
            {
                GameObject spike = Object.Instantiate(spikePrefab);
                spike.transform.SetParent(spikeCenter.transform);
                spike.transform.localPosition = new Vector3(x, -0.4f, 0);
                x += spacing;
                spikes.Add(spike);
                spike.SetActive(true);
            }
            return true;
        }

        private static DamageEnemies SetDamageEnemy(GameObject go, int value = 0)
        {
            var damage = go.GetComponent<DamageEnemies>() ?? go.AddComponent<DamageEnemies>();
            damage.attackType = AttackTypes.Spell;
            damage.circleDirection = false;
            damage.damageDealt = value;
            damage.direction = 90 * 3;
            damage.ignoreInvuln = false;
            damage.magnitudeMult = 1f;
            damage.moveDirection = false;
            damage.specialType = 0;

            return damage;
        }
    }
}
