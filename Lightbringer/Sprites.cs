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
        internal static Dictionary<string, Sprite> customSprites;
        internal static Dictionary<string, Sprite> originalSprites;

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
                    var tex = new Texture2D(2, 2);

                    tex.LoadImage(buffer, true);

                    // Create sprite from texture
                    // Substring is to cut off the Lightbringer. and the .png
                    customSprites.Add(res.Substring(23, res.Length - 27), Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
                }
            }
        }

        internal static IEnumerator Change(bool restore = false)
        {
            while (CharmIconList.Instance == null ||
                   GameManager.instance == null ||
                   HeroController.instance == null ||
                   HeroController.instance.geoCounter == null ||
                   HeroController.instance.geoCounter.geoSprite == null ||
                   customSprites.Count < 28)
                yield return null;

            if (originalSprites == null)
                Save();

            var sprites = restore ? originalSprites : customSprites;

            foreach (int i in new int[] { 2, 3, 4, 6, 8, 11, 13, 14, 15, 16, 17, 18, 19, 20, 21, 25, 26, 35 })
                CharmIconList.Instance.spriteList[i] = sprites["Charms." + i];

            HeroController.instance.geoCounter.geoSprite.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material.mainTexture = sprites["UI"].texture;

            CharmIconList.Instance.unbreakableStrength = sprites["Charms.ustr"];

            GameObject.Find("/_GameCameras/HudCamera/Inventory/Charms/Collected Charms/25")
                .LocateMyFSM("charm_show_if_collected")
                .GetAction<SetSpriteRendererSprite>("Glass Attack", 2)
                .sprite.Value = sprites["Charms.brokestr"];
            GameObject.Find("/_GameCameras/HudCamera/Inventory/Charms/Details/Detail Sprite")
                .LocateMyFSM("Update Sprite")
                .GetAction<SetSpriteRendererSprite>("Glass Attack", 2)
                .sprite.Value = sprites["Charms.brokestr"];

            HeroController.instance.grubberFlyBeamPrefabL.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material.mainTexture = sprites["Lances"].texture;

            HeroController.instance.gameObject.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material.mainTexture = sprites["Knight"].texture;

            HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>()
                          .GetClipByName("Sprint")
                          .frames[0]
                          .spriteCollection.spriteDefinitions[0]
                          .material.mainTexture = sprites["Sprint"].texture;

            GameObject.Find("/Knight/Effects/Shadow Dash Blobs")
                .GetComponent<ParticleSystemRenderer>()
                .material.mainTexture = sprites["Void"].texture;

            foreach (Transform child in HeroController.instance.transform)
                if (child.name == "Spells")
                    foreach (Transform spellsChild in child)
                        if (spellsChild.name == "Scr Heads 2" || spellsChild.name == "Scr Base 2")
                            spellsChild.gameObject.GetComponent<tk2dSprite>()
                                .GetCurrentSpriteDef()
                                .material.mainTexture = sprites["VoidSpells"].texture;

            var invNail = GameObject.Find("/_GameCameras/HudCamera/Inventory/Inv/Inv_Items/Nail");
            var invNailSprite = invNail.GetComponent<InvNailSprite>();

            invNailSprite.level1 = sprites[restore ? "Nail1" : "LanceInv"];
            invNailSprite.level2 = sprites[restore ? "Nail2" : "LanceInv"];
            invNailSprite.level3 = sprites[restore ? "Nail3" : "LanceInv"];
            invNailSprite.level4 = sprites[restore ? "Nail4" : "LanceInv"];
            invNailSprite.level5 = sprites[restore ? "Nail5" : "LanceInv"];
        }

        private static void Save()
        {
            originalSprites = new Dictionary<string, Sprite>();

            foreach (int i in new int[] { 2, 3, 4, 6, 8, 11, 13, 14, 15, 16, 17, 18, 19, 20, 21, 25, 26, 35 })
                originalSprites["Charms." + i] = CharmIconList.Instance.spriteList[i];

            Texture2D UI = HeroController.instance.geoCounter.geoSprite.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture as Texture2D;
            originalSprites["UI"] = Sprite.Create(UI, new Rect(0, 0, UI.width, UI.height), new Vector2(0.5f, 0.5f));

            originalSprites["Charms.ustr"] = CharmIconList.Instance.unbreakableStrength;

            originalSprites["Charms.brokestr"] = GameObject.Find("/_GameCameras/HudCamera/Inventory/Charms/Collected Charms/25")
                .LocateMyFSM("charm_show_if_collected")
                .GetAction<SetSpriteRendererSprite>("Glass Attack", 2)
                .sprite.Value as Sprite;
            originalSprites["Charms.brokestr"] = GameObject.Find("/_GameCameras/HudCamera/Inventory/Charms/Details/Detail Sprite")
                .LocateMyFSM("Update Sprite")
                .GetAction<SetSpriteRendererSprite>("Glass Attack", 2)
                .sprite.Value as Sprite;

            Texture2D Lances = HeroController.instance.grubberFlyBeamPrefabL.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material.mainTexture as Texture2D;
            originalSprites["Lances"] = Sprite.Create(Lances, new Rect(0, 0, Lances.width, Lances.height), new Vector2(0.5f, 0.5f));

            Texture2D Knight = HeroController.instance.gameObject.GetComponent<tk2dSprite>()
                          .GetCurrentSpriteDef()
                          .material.mainTexture as Texture2D;
            originalSprites["Knight"] = Sprite.Create(Knight, new Rect(0, 0, Knight.width, Knight.height), new Vector2(0.5f, 0.5f));

            Texture2D Sprint = HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>()
                          .GetClipByName("Sprint")
                          .frames[0]
                          .spriteCollection.spriteDefinitions[0]
                          .material.mainTexture as Texture2D;
            originalSprites["Sprint"] = Sprite.Create(Sprint, new Rect(0, 0, Sprint.width, Sprint.height), new Vector2(0.5f, 0.5f));

            Texture2D Void = GameObject.Find("/Knight/Effects/Shadow Dash Blobs")
                .GetComponent<ParticleSystemRenderer>()
                .material.mainTexture as Texture2D;
            originalSprites["Void"] = Sprite.Create(Void, new Rect(0, 0, Void.width, Void.height), new Vector2(0.5f, 0.5f));

            Texture2D VoidSpells = GameObject.Find("/Knight/Spells/Scr Base 2")
                .GetComponent<tk2dSprite>()
                .GetCurrentSpriteDef()
                .material.mainTexture as Texture2D;
            originalSprites["VoidSpells"] = Sprite.Create(VoidSpells, new Rect(0, 0, VoidSpells.width, VoidSpells.height), new Vector2(0.5f, 0.5f));

            var invNail = GameObject.Find("/_GameCameras/HudCamera/Inventory/Inv/Inv_Items/Nail");
            var invNailSprite = invNail.GetComponent<InvNailSprite>();

            originalSprites["Nail1"] = invNailSprite.level1;
            originalSprites["Nail2"] = invNailSprite.level2;
            originalSprites["Nail3"] = invNailSprite.level3;
            originalSprites["Nail4"] = invNailSprite.level4;
            originalSprites["Nail5"] = invNailSprite.level5;
        }
    }
}
