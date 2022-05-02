using UnityEngine;

namespace Lightbringer
{
    public class Muzznik : MonoBehaviour
    {
        private bool[] fight;

        private HealthManager healthManager;

        private GameObject minion;

        private GameObject[] minions;

        private void Start()
        {
            gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = Sprites.customSprites["Muzznik"].texture;
            healthManager = gameObject.GetComponent<HealthManager>();
            healthManager.hp = 1500;
            fight = new bool[12];
            minions = new GameObject[16];
            minion = GameObject.Find("Fly");
            minion.transform.SetScaleY(-1f);
            minion.GetComponent<HealthManager>().hp = 99999;
        }


        private void Update()
        {
            int gruzHp = healthManager.hp;
            if (!fight[0] && gruzHp < 1470)
            {
                fight[0] = true;
                minions[0] = minion.Spawn(gameObject.transform.position); // dud
                minions[1] = minion.Spawn(gameObject.transform.position);
                minions[1].transform.SetScaleX(1.3f);
                minions[1].transform.SetScaleY(-1.3f);
            }
            else if (!fight[1] && gruzHp < 1100)
            {
                fight[1] = true;
                minions[2] = minion.Spawn(gameObject.transform.position);
            }
            else if (!fight[2] && gruzHp < 800)
            {
                fight[2] = true;
                minions[3] = minion.Spawn(gameObject.transform.position);
                minions[3].transform.SetScaleX(.8f);
                minions[3].transform.SetScaleY(-.8f);
            }
            else if (!fight[3] && gruzHp < 600)
            {
                fight[3] = true;
                minions[4] = minion.Spawn(gameObject.transform.position);
                minions[4].transform.SetScaleX(.8f);
                minions[4].transform.SetScaleY(-.8f);
            }
            else if (!fight[4] && gruzHp < 500)
            {
                fight[4] = true;
                minions[5] = minion.Spawn(gameObject.transform.position);
            }
            else if (!fight[5] && gruzHp < 400)
            {
                fight[5] = true;
                minions[6] = minion.Spawn(gameObject.transform.position);
            }
            else if (!fight[6] && gruzHp < 300)
            {
                fight[6] = true;
                minions[7] = minion.Spawn(gameObject.transform.position);
            }
            else if (!fight[7] && gruzHp < 200)
            {
                fight[7] = true;
                minions[8] = minion.Spawn(gameObject.transform.position);
                minions[9] = minion.Spawn(gameObject.transform.position);
            }
            else if (!fight[8] && gruzHp < 100)
            {
                fight[8] = true;
                minions[10] = minion.Spawn(gameObject.transform.position);
                minions[11] = minion.Spawn(gameObject.transform.position);
            }
            else if (!fight[9] && gruzHp < 1)
            {
                fight[9] = true;
            }
        }

        private void OnDestroy()
        {
            minion.GetComponent<HealthManager>().hp = 1;
            for (int i = 0; i < 12; i++)
                if (minions[i] != null)
                    GetComponent<HealthManager>().hp = 1;
        }
    }
}