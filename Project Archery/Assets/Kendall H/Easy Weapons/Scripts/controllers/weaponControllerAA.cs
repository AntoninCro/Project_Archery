using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyWeapon
{

    public class weaponControllerAA : MonoBehaviour
    {


        public bool enchantAlwaysVisable, isThrowable;

        public List<MeshFilter> enchantmentFilters = new List<MeshFilter>();

        // Start is called before the first frame update
        private void Start()
        {
            if (!enchantAlwaysVisable)
            {
                StopEnchantment();
            }
        }



        public virtual void UseWeapon()
        {
            //do something

            //enable hit colliders
            //enable swing partical effects
            //sound
            //etc

        }

        public virtual void StopWeapon()
        {
            //do something
        }


        public virtual void UseEnchantment()
        {
            if (enchantmentFilters != null && !isThrowable)
            {
                foreach (var filter in enchantmentFilters)
                {
                    filter.gameObject.SetActive(true);
                }
            }

            if (isThrowable)
            {
                //yeet that thang
            }

            //have some effect

        }

        public virtual void StopEnchantment()
        {
            if (enchantmentFilters != null)
            {
                foreach (var filter in enchantmentFilters)
                {
                    filter.gameObject.SetActive(false);
                }
            }

            //end some effect

        }


    }
}

