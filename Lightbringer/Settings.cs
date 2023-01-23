using System;

namespace Lightbringer
{
    [Serializable]
    public class Settings
    {
        // Buffed bosses
        public bool EmpressMuzznik = true;
        public bool DoubleKin = false;

        // Lance attack config options
        public bool NailCollision = true;
        public int LanceDamage = 3;
        public int LanceUpgradeBonus = 3;
        public int RadiantJewelDamage = 5;
        public float FragileNightmareScaleFactor = 1 / 20f;
        public int FragileNightmareSoulCost = 7;

        // Nail attack config options
        public int NailDamage = 5;
        public int NailUpgradeBonus = 2;
        public float BurningPrideScaleFactor = 1 / 6f;

        // MP regen
        public float SoulRegenRate = 1.11f;
    }
}
