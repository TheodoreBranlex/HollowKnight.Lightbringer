using System;
using System.Collections.Generic;
using GlobalEnums;
using Modding;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using static Lightbringer.AttackHandler.BeamDirection;
using Random = System.Random;

namespace Lightbringer
{
    public class AttackHandler
    {
        public static AudioClip BeamAudioClip;

        private static GameObject GrubberFlyBeam
        {
            get => ReflectionHelper.GetField<HeroController, GameObject>(HeroController.instance, "grubberFlyBeam");
            set => ReflectionHelper.SetField(HeroController.instance, "grubberFlyBeam", value);
        }

        private bool _crit;

        public AttackHandler() {}

        public AttackHandler(float fracture) {}

        private static Random _rand => Lightbringer.random;

        public void Attack(HeroController hc, AttackDirection dir)
        {
            Settings settings = Lightbringer.instance.settings;
            PlayerData pd = PlayerData.instance;

            hc.cState.altAttack = false;
            hc.cState.attacking = true;

            #region Damage Controller

            // NAIL
            pd.nailDamage = settings.NailDamage + pd.nailSmithUpgrades * settings.NailUpgradeBonus;
            if (pd.equippedCharm_13) // Mark of Pride
            {
                pd.CountGameCompletion();
                pd.nailDamage += (int)(pd.completionPercentage * settings.BurningPrideScaleFactor);
            }

            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");

            // LANCE
            pd.beamDamage = settings.LanceDamage + pd.nailSmithUpgrades * settings.LanceUpgradeBonus;

            // Radiant Jewel (Elegy)
            if (pd.equippedCharm_35) pd.beamDamage += settings.RadiantJewelDamage;

            // Fragile Nightmare damage calculations
            if (dir == AttackDirection.normal && pd.equippedCharm_25 && pd.MPCharge > 3) // Fragile Strength > Fragile Nightmare
            {
                pd.beamDamage += pd.MPCharge / 20;
                hc.TakeMP(7);
            }

            if (pd.equippedCharm_6) // Glass Soul charm replacing Fury of Fallen
                pd.beamDamage += pd.health + pd.healthBlue - 3;

            #endregion

            _crit = CalculateCrit(hc, pd, dir);

            int lanceDamage = pd.beamDamage;

            // QUICK SLASH CHARM #REEE32
            ReflectionHelper.SetField(hc, "attackDuration", pd.equippedCharm_32 ? hc.ATTACK_DURATION_CH : hc.ATTACK_DURATION);

            // Handle audio
            if (dir == AttackDirection.normal || (dir == AttackDirection.upward && pd.equippedCharm_8))
            {
                if (BeamAudioClip == null)
                {
                    GameObject BeamPrefabGameObject = ReflectionHelper.GetField<HeroController, GameObject>(hc, "grubberFlyBeamPrefabU");
                    AudioSource BeamAudio = BeamPrefabGameObject.GetComponent<AudioSource>();
                    BeamAudioClip = BeamAudio.clip;
                }
                ReflectionHelper.GetField<HeroController, AudioSource>(hc, "audioSource").PlayOneShot(BeamAudioClip, 0.1f);
            }
            
            // Fragile Nightmare damage calculations
            if (pd.equippedCharm_25 && pd.MPCharge > settings.FragileNightmareSoulCost) // Fragile Strength > Fragile Nightmare
            {
                pd.beamDamage += (int) (pd.MPCharge * settings.FragileNightmareScaleFactor);
                hc.TakeMP(settings.FragileNightmareSoulCost);
            }

            if (pd.equippedCharm_38) hc.fsm_orbitShield.SendEvent("SLASH");

            if (hc.cState.wallSliding)
            {
                pd.nailDamage = pd.beamDamage; 
                PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
                pd.beamDamage = lanceDamage;
                ReflectionHelper.SetField(hc, "wallSlashing", true);
                SpawnBeam(!hc.cState.facingRight, 1f, 1f);
                return;
            }

            ReflectionHelper.SetField(hc, "wallSlashing", false);
            switch (dir)
            {
                #region Normal Attack

                case AttackDirection.normal:
                    pd.nailDamage = pd.beamDamage;
                    PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
                    pd.beamDamage = lanceDamage;

                    hc.normalSlashFsm.FsmVariables.GetFsmFloat("direction").Value = hc.cState.facingRight ? 0f : 180f;
                    ReflectionHelper.SetField(hc, "slashComponent", hc.normalSlash);

                    if (settings.NailCollision)
                        hc.normalSlash.StartSlash();

                    bool tShell = pd.equippedCharm_4;

                    if (pd.equippedCharm_19)
                    {
                        if (pd.MPCharge > 10)
                        {
                            if (!_crit) hc.TakeMP(10);
                            var instance = hc.spell1Prefab.Spawn(hc.transform.position + (tShell ? new Vector3(0f, .6f) : new Vector3(0f, .3f)));
                            instance.LocateMyFSM("Fireball Cast").GetAction<PlayerDataBoolTest>("Cast Right", 5).isTrue = FsmEvent.GetFsmEvent("");
                            instance.LocateMyFSM("Fireball Cast").GetAction<PlayerDataBoolTest>("Cast Left", 2).isTrue = FsmEvent.GetFsmEvent("");
                        }
                        else
                        {
                            ReflectionHelper.GetField<HeroController, AudioSource>(hc, "audioSource").PlayOneShot(hc.blockerImpact, 1f);
                        }
                        return;
                    }

                    // Grubberfly's Elegy
                    if (pd.equippedCharm_35)
                    {
                        // Longnail AND/OR Soul Catcher
                        if (pd.equippedCharm_20)
                        {
                            bool longnail = pd.equippedCharm_18;

                            if (hc.cState.facingRight || longnail)
                                SpawnBeams(Right, 1.5f, 1.5f, positionY: tShell ? new[] { .2f, .7f } : new[] { 0f, .9f });

                            if (!hc.cState.facingRight || longnail)
                                SpawnBeams(Left, 1.5f, 1.5f, positionY: tShell ? new[] { .2f, .7f } : new[] { 0f, .9f });
                        }
                        // Longnail
                        else if (pd.equippedCharm_18)
                            SpawnBeams(1.5f, 1.5f, positionY: tShell ? .2f : .1f);
                        else
                            SpawnBeam(hc.cState.facingRight, 1.5f, 1.5f, positionY: tShell ? .2f : .1f);
                    }
                    // Longnail AND Soul Catcher
                    else if (pd.equippedCharm_20 && pd.equippedCharm_18)
                        SpawnBeams(1f, 1f, positionY: tShell ? new float[] { -.2f, .7f } : new float[] { .5f, -.4f });
                    // Soul Catcher
                    else if (pd.equippedCharm_20)
                        SpawnBeams(hc.cState.facingRight, 1f, 1f, positionY: tShell ? new[] { -.2f, .7f } : new[] { .5f, -.4f });
                    // Longnail
                    else if (pd.equippedCharm_18)
                        SpawnBeams(1f, 1f);
                    else // No charms
                        SpawnBeam(hc.cState.facingRight, 1f, 1f);

                    // Handle Recoil
                    if (!pd.equippedCharm_18)
                    {
                        if (pd.equippedCharm_35)
                        {
                            if (tShell || pd.equippedCharm_20)
                            {
                                if (hc.cState.facingRight) { Recoil(Right, true); }
                                else { Recoil(Left, true); }
                            }
                            else
                            {
                                if (hc.cState.facingRight) { Recoil(Right, false); }
                                else { Recoil(Left, false); }
                            }
                        }
                        else if (tShell && pd.equippedCharm_20)
                        {
                            if (hc.cState.facingRight) { Recoil(Right, false); }
                            else { Recoil(Left, false); }
                        }
                    }

                    break;

                #endregion

                #region Upwards Attack

                case AttackDirection.upward:
                    // Timescale Charm #14 - TIME FRACTURE //
                    if (pd.equippedCharm_14 && Lightbringer.timeFracture < 2f)
                    {
                        Lightbringer.timeFracture += 0.1f;
                        Lightbringer.spriteFlash.flash(Color.white, 0.85f, 0.35f, 0f, 0.35f);
                    }

                    // Upward Attack Charm #8 - RISING LIGHT //
                    if (pd.equippedCharm_8)
                    {
                        // Fragile Nightmare damage calculations
                        if (pd.equippedCharm_25 &&
                            pd.MPCharge > 3)
                        {
                            pd.beamDamage += pd.MPCharge / 20;
                            hc.TakeMP(7);
                        }

                        pd.nailDamage = pd.beamDamage;
                        PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
                        pd.beamDamage = lanceDamage;

                        foreach (float i in new float[] {0.5f, 0.85f, 1.2f, 1.55f, .15f, -.2f, -.55f, -.9f})
                        {
                            SpawnBeam(Up, 0.6f, 0.6f, i);
                            GrubberFlyBeam.transform.Rotate(0f, 0f, -90f);
                        }
                    }

                    ReflectionHelper.SetField(hc, "slashComponent", hc.upSlash);
                    ReflectionHelper.SetField(hc, "slashFsm", hc.upSlashFsm);
                    hc.cState.upAttacking = true;
                    hc.upSlashFsm.FsmVariables.GetFsmFloat("direction").Value = 90f;
                    ReflectionHelper.GetField<HeroController, NailSlash>(hc, "slashComponent").StartSlash();
                    break;

                #endregion

                #region Downward Attack

                case AttackDirection.downward:
                    ReflectionHelper.SetField(hc, "slashComponent", hc.downSlash);
                    ReflectionHelper.SetField(hc, "slashFsm", hc.downSlashFsm);
                    hc.cState.downAttacking = true;
                    hc.downSlashFsm.FsmVariables.GetFsmFloat("direction").Value = 270f;
                    ReflectionHelper.GetField<HeroController, NailSlash>(hc, "slashComponent").StartSlash();
                    break;

                #endregion

                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }

        private static bool CalculateCrit(HeroController hc, PlayerData pd, AttackDirection dir)
        {
            if (!pd.equippedCharm_3 || dir != AttackDirection.normal) return false;
            
            int critChance = _rand.Next(1, 101);
            
            pd.CountJournalEntries();
            int critThreshold = 100 - pd.journalEntriesCompleted / 10;

            if (critChance <= Math.Min(critThreshold, 96)) return false;
            
            pd.beamDamage *= 3;
            hc.shadowRingPrefab.Spawn(hc.transform.position);
            ReflectionHelper.GetField<HeroController, AudioSource>(hc, "audioSource").PlayOneShot(hc.nailArtChargeComplete, 1f);

            return true;
        }

        #region Recoil

        private static void Recoil(BeamDirection dir, bool @long)
        {
            // The directions are flipped cause you recoil the opposite of the direction you attack
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (dir)
            {
                case Right:
                    if (@long)
                        HeroController.instance.RecoilLeftLong();
                    else
                        HeroController.instance.RecoilLeft();
                    break;
                case Left:
                    if (@long)
                        HeroController.instance.RecoilRightLong();
                    else
                        HeroController.instance.RecoilRight();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }
        #endregion

        #region SpawnBeam
        internal enum BeamDirection
        {
            Up,
            Down,
            Right,
            Left
        }

        private void SpawnBeams
        (
            bool         dir,
            float        scaleX,
            float        scaleY,
            float?       positionX     = null,
            IList<float> positionY     = null,
            bool         offset        = true,
            bool         rightNegative = true
        )
        {
            SpawnBeams(dir ? Right : Left, scaleX, scaleY, positionX, positionY, offset, rightNegative);
        }

        private void SpawnBeams
        (
            BeamDirection dir,
            float         scaleX,
            float         scaleY,
            float?        positionX     = null,
            IList<float>  positionY     = null,
            bool          offset        = true,
            bool          rightNegative = true
        )
        {
            SpawnBeam(dir, scaleX, scaleY, positionX, positionY?[0], offset, rightNegative);
            SpawnBeam(dir, scaleX, scaleY, positionX, positionY?[1], offset, rightNegative);
        }

        private void SpawnBeams
        (
            float   scaleX,
            float   scaleY,
            float?  positionX     = null,
            object  positionY     = null,
            bool    offset        = true,
            bool    rightNegative = true
        )
        {
            switch (positionY)
            {
                case float posY:
                    SpawnBeam(Left, scaleX, scaleY, positionX, posY, offset, rightNegative);
                    SpawnBeam(Right, scaleX, scaleY, positionX, posY, offset, rightNegative);
                    break;
                case float[] posYs:
                    foreach (float y in posYs)
                    {
                        SpawnBeams(scaleX, scaleY, positionX, y, offset, rightNegative);
                        SpawnBeams(scaleX, scaleY, positionX, y, offset, rightNegative);
                    }

                    break;
                case null:
                    SpawnBeam(Left, scaleX, scaleY, positionX, null, offset, rightNegative);
                    SpawnBeam(Right, scaleX, scaleY, positionX, null, offset, rightNegative);
                    break;
            }
        }

        internal void SpawnBeam
        (
            bool    dir,
            float   scaleX,
            float   scaleY,
            float?  positionX     = null,
            float?  positionY     = null,
            bool    offset        = true,
            bool    rightNegative = true
        )
        {
            SpawnBeam(dir ? Right : Left, scaleX, scaleY, positionX, positionY, offset, rightNegative);
        }

        private void SpawnBeam
        (
            BeamDirection dir,
            float         scaleX,
            float         scaleY,
            float?        positionX     = null,
            float?        positionY     = null,
            bool          offset        = true,
            bool          rightNegative = true
        )
        {
            string beamPrefab = "grubberFlyBeamPrefab";
            switch (dir)
            {
                case Up:
                    beamPrefab += "U";
                    break;
                case Down:
                    beamPrefab += "D";
                    break;
                case Right:
                    beamPrefab += "R";
                    break;
                case Left:
                    beamPrefab += "L";
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }

            beamPrefab += _crit ? "_fury" : "";

            HeroController hc = HeroController.instance;

            GrubberFlyBeam = ReflectionHelper.GetField<HeroController, GameObject>(hc, beamPrefab).Spawn(hc.transform.position);
            AudioSource BeamAudio = GrubberFlyBeam.GetComponent<AudioSource>();
            BeamAudio.enabled = false; // disable audio source for beams
            Transform t = hc.transform;

            if (positionX != null)
                GrubberFlyBeam.transform.SetPositionX((float) (positionX + (offset ? t.GetPositionX() : 0)));
            if (positionY != null)
                GrubberFlyBeam.transform.SetPositionY((float) (positionY + (offset ? t.GetPositionY() : 0)));

            GrubberFlyBeam.transform.SetScaleX((rightNegative && dir == Right ? -1 : 1) * scaleX);
            GrubberFlyBeam.transform.SetScaleY(scaleY);
        }

        #endregion
    }
}