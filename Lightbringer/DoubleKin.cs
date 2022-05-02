using UnityEngine;

namespace Lightbringer
{
    public class DoubleKin : MonoBehaviour
    {
        private HealthManager healthManager;
        private float         invincibleTime = Time.deltaTime;
        private bool[]        fight;
        private GameObject    kinTwo;

        private void Start()
        {
            healthManager = gameObject.GetComponent<HealthManager>();
            fight = new bool[12];
        }

        private void Update()
        {
            int kinHp = healthManager.hp;
            if (!fight[0] && kinHp < 400)
            {
                fight[0] = true;
                HeroController.instance.playerData.isInvincible = true; // temporary invincibility iFrames
                Lightbringer.spriteFlash.flash(Color.black, 0.6f, 0.15f, 0f, 0.55f);
                fight[5] = true; // iFrames
                kinTwo = Instantiate(gameObject);
                kinTwo.GetComponent<HealthManager>().hp = 99999;
            }
            else if (fight[5]) // iFrames
            {
                invincibleTime += Time.deltaTime;
                if (!(invincibleTime >= 5.5f)) return;
                HeroController.instance.playerData.isInvincible = false;
                fight[5] = false;
            }
            else if (!fight[1] && kinHp < 1)
            {
                fight[1] = true;
                kinTwo.GetComponent<HealthManager>().hp = 1;
            }
        }
    }
}