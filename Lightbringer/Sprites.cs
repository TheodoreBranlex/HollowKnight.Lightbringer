using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker.Actions;

namespace Lightbringer
{
    public static class Sprites
    {
        internal static Dictionary<string, Sprite> originalSprites, customSprites;
        static Coroutine changeSprites;

        internal static void Load()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            customSprites = new Dictionary<string, Sprite>();

            foreach (string res in asm.GetManifestResourceNames())
            {
                if (!res.EndsWith(".png") && !res.EndsWith(".tex")) continue;

                using (Stream stream = asm.GetManifestResourceStream(res))
                {
                    if (stream == null) continue;

                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    stream.Dispose();

                    // Create texture from bytes
                    var texture = new Texture2D(2, 2);

                    texture.LoadImage(buffer, true);

                    // Substring is to cut off the Lightbringer. and the .png
                    customSprites.Add(res.Substring(23, res.Length - 27), CreateSprite(texture));
                }
            }
        }

        internal static void Enable(bool custom)
        {
            if (changeSprites != null)
                GameManager.instance.StopCoroutine(changeSprites);
            changeSprites = GameManager.instance.StartCoroutine(Change(custom));
        }

        private static IEnumerator Change(bool custom)
        {
            while (CharmIconList.Instance == null || HeroController.instance?.geoCounter?.geoSprite == null)
                yield return null;

            if (originalSprites == null)
                Cache();

            var sprites = custom ? customSprites : originalSprites;

            foreach (int i in new int[] { 2, 3, 4, 6, 8, 11, 13, 14, 15, 16, 17, 18, 19, 20, 21, 25, 26, 35 })
                CharmIconList.Instance.spriteList[i] = sprites["Charms." + i];

            var ui = HeroController.instance.geoCounter.geoSprite.GetComponent<tk2dSprite>();
            ui.GetCurrentSpriteDef().material.mainTexture = sprites["UI"].texture;

            CharmIconList.Instance.unbreakableStrength = sprites["Charms.ustr"];

            var strength = GameObject.Find("/_GameCameras/HudCamera/Inventory/Charms/Collected Charms/25");
            var brokenStrength = strength.LocateMyFSM("charm_show_if_collected").GetAction<SetSpriteRendererSprite>("Glass Attack", 2);
            brokenStrength.sprite.Value = sprites["Charms.brokestr"];

            var detailedCharm = GameObject.Find("/_GameCameras/HudCamera/Inventory/Charms/Details/Detail Sprite");
            var detailedBrokenStrength = detailedCharm.LocateMyFSM("Update Sprite").GetAction<SetSpriteRendererSprite>("Glass Attack", 2);
            detailedBrokenStrength.sprite.Value = sprites["Charms.brokestr"];

            var lances = HeroController.instance.grubberFlyBeamPrefabL.GetComponent<tk2dSprite>();
            lances.GetCurrentSpriteDef().material.mainTexture = sprites["Lances"].texture;

            var knight = HeroController.instance.gameObject.GetComponent<tk2dSprite>();
            knight.GetCurrentSpriteDef().material.mainTexture = sprites["Knight"].texture;

            var sprint = HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>();
            sprint.GetClipByName("Sprint").frames[0].spriteCollection.spriteDefinitions[0].material.mainTexture = sprites["Sprint"].texture;

            var voidParticle = GameObject.Find("/Knight/Effects/Shadow Dash Blobs");
            voidParticle.GetComponent<ParticleSystemRenderer>().material.mainTexture = sprites["VoidParticle"].texture;

            var spells1 = GameObject.Find("/Knight/Spells/Scr Heads 2").GetComponent<tk2dSprite>();
            var spells2 = GameObject.Find("/Knight/Spells/Scr Base 2").GetComponent<tk2dSprite>();
            spells1.GetCurrentSpriteDef().material.mainTexture = sprites["VoidSpells"].texture;
            spells2.GetCurrentSpriteDef().material.mainTexture = sprites["VoidSpells"].texture;

            var invNail = GameObject.Find("/_GameCameras/HudCamera/Inventory/Inv/Inv_Items/Nail").GetComponent<InvNailSprite>();
            invNail.level1 = sprites[custom ? "LanceInv" : "Nail1"];
            invNail.level2 = sprites[custom ? "LanceInv" : "Nail2"];
            invNail.level3 = sprites[custom ? "LanceInv" : "Nail3"];
            invNail.level4 = sprites[custom ? "LanceInv" : "Nail4"];
            invNail.level5 = sprites[custom ? "LanceInv" : "Nail5"];
        }

        private static void Cache()
        {
            originalSprites = new Dictionary<string, Sprite>();

            foreach (int i in new int[] { 2, 3, 4, 6, 8, 11, 13, 14, 15, 16, 17, 18, 19, 20, 21, 25, 26, 35 })
                originalSprites["Charms." + i] = CharmIconList.Instance.spriteList[i];

            var ui = HeroController.instance.geoCounter.geoSprite.GetComponent<tk2dSprite>();
            originalSprites["UI"] = CreateSprite(ui.GetCurrentSpriteDef().material.mainTexture as Texture2D);

            originalSprites["Charms.ustr"] = CharmIconList.Instance.unbreakableStrength;

            var strength = GameObject.Find("/_GameCameras/HudCamera/Inventory/Charms/Collected Charms/25");
            var brokenStrength = strength.LocateMyFSM("charm_show_if_collected").GetAction<SetSpriteRendererSprite>("Glass Attack", 2);
            originalSprites["Charms.brokestr"] = brokenStrength.sprite.Value as Sprite;

            var lances = HeroController.instance.grubberFlyBeamPrefabL.GetComponent<tk2dSprite>();
            originalSprites["Lances"] = CreateSprite(lances.GetCurrentSpriteDef().material.mainTexture as Texture2D);

            var knight = HeroController.instance.gameObject.GetComponent<tk2dSprite>();
            originalSprites["Knight"] = CreateSprite(knight.GetCurrentSpriteDef().material.mainTexture as Texture2D);

            var sprint = HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>();
            originalSprites["Sprint"] = CreateSprite(sprint.GetClipByName("Sprint").frames[0].spriteCollection.spriteDefinitions[0].material.mainTexture as Texture2D);

            var voidParticle = GameObject.Find("/Knight/Effects/Shadow Dash Blobs");
            originalSprites["VoidParticle"] = CreateSprite(voidParticle.GetComponent<ParticleSystemRenderer>().material.mainTexture as Texture2D);

            var spells = GameObject.Find("/Knight/Spells/Scr Heads 2").GetComponent<tk2dSprite>();
            originalSprites["VoidSpells"] = CreateSprite(spells.GetCurrentSpriteDef().material.mainTexture as Texture2D);

            var invNail = GameObject.Find("/_GameCameras/HudCamera/Inventory/Inv/Inv_Items/Nail").GetComponent<InvNailSprite>();
            originalSprites["Nail1"] = invNail.level1;
            originalSprites["Nail2"] = invNail.level2;
            originalSprites["Nail3"] = invNail.level3;
            originalSprites["Nail4"] = invNail.level4;
            originalSprites["Nail5"] = invNail.level5;
        }

        private static Sprite CreateSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
