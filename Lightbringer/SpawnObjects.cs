using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace Lightbringer
{
    public class SpawnObjects : FsmStateAction
    {
        public override void Reset()
        {
            gameObject = null;
            spawnPoint = null;
            position = new FsmVector3 { UseVariable = true };
            rotation = new FsmVector3 { UseVariable = true };
            frequency = null;
        }
        public override void OnUpdate()
        {
            timer += Time.deltaTime;
            if (timer >= frequency.Value)
            {
                timer = 0f;
                GameObject value = gameObject.Value;
                if (value != null)
                {
                    Vector3 a = Vector3.zero;
                    Vector3 euler = Vector3.up;
                    if (spawnPoint.Value != null)
                    {
                        a = spawnPoint.Value.transform.position;
                        if (!position.IsNone)
                            a += position.Value;
                        euler = rotation.IsNone ? spawnPoint.Value.transform.eulerAngles : rotation.Value;
                    }
                    else
                    {
                        if (!position.IsNone)
                            a = position.Value;
                        if (!rotation.IsNone)
                            euler = rotation.Value;
                    }
                    if (gameObject != null)
                    {
                        GameObject gameObject = this.gameObject.Value.Spawn(a, Quaternion.Euler(euler));
                        initialize?.Invoke(gameObject);
                        gameObject.SetActive(true);
                    }
                }
            }
        }

        [RequiredField] public FsmGameObject gameObject;

        public FsmGameObject spawnPoint;

        public FsmVector3 position;

        public FsmVector3 rotation;

        public FsmFloat frequency;

        public Action<GameObject> initialize;

        private float timer;
    }
}
