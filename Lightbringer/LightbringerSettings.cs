using System;

namespace Lightbringer
{
    [Serializable]
    public class LightbringerSettings
    {
        // Lance attack config options
        public int BaseBeamDamage = 3;
        public int UpgradeBeamDamage = 3;
        public int RadiantJewelDamage = 5;
        public float FragileNightmareScaleFactor = 1 / 20f;
        public int FragileNightmareSoulCost = 7;

        // Nail attack config options
        public int BaseNailDamage = 1;
        public int UpgradeNailDamage = 2;
        public float BurningPrideScaleFactor = 1 / 6f;

        // MP regen
        public float SoulRegenRate = 1.11f;
    }
}
