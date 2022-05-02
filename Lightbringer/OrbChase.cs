using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lightbringer
{
    public class OrbChase : MonoBehaviour
    {
        public HealthManager target;
        public float accelerationForce;
        public float speedMax;
        private Rigidbody2D rb2d;

        private void Awake()
        {
            rb2d = GetComponent<Rigidbody2D>();
            speedMax = Random.Range(20, 30);
            accelerationForce = Random.Range(40, 60);
        }
        private void Start()
        {
            rb2d.bodyType = 0;
            target = FindTarget();
        }

        private static bool ActiveEnemy(HealthManager hm)
        {
            if (hm && hm.gameObject.activeSelf && !hm.IsInvincible && !hm.isDead && hm.hp > 0)
                return true;
            return false;
        }
        private HealthManager FindTarget()
        {
            List<HealthManager> targets = FindObjectsOfType<HealthManager>().ToList();

            float min = Mathf.Infinity;
            HealthManager best = null;
            foreach (var t in targets)
            {
                if (!ActiveEnemy(t))
                    continue;

                Vector2 difference = t.transform.position - gameObject.transform.position;
                float distance = difference.sqrMagnitude;

                if (distance < min)
                {
                    min = distance;
                    best = t;
                }
            }
            return best;
        }

        private void FixedUpdate()
        {
            SetChase(rb2d, target, accelerationForce, speedMax);
        }
        public static void SetChase(Rigidbody2D rb2d, HealthManager target, float accelerationForce, float speedMax)
        {
            if (target == null || rb2d == null)
                return;

            Vector2 vector = new Vector2(target.transform.position.x - rb2d.transform.position.x, target.transform.position.y - rb2d.transform.position.y);
            vector = Vector2.ClampMagnitude(vector, 1f);
            vector = new Vector2(vector.x * accelerationForce, vector.y * accelerationForce);
            rb2d.AddForce(vector);
            Vector2 vector2 = rb2d.velocity;
            vector2 = Vector2.ClampMagnitude(vector2, speedMax);
            rb2d.velocity = vector2;
        }
    }
}
