using UnityEngine;
using UnityEngine.Animations.Rigging;


namespace EasyWeapon
{

    public class bowWeaponControllerAA : weaponControllerAA
    {
        [SerializeField] bool isTesting;

        [Range(0f, 1f)]
        public float bowDrawPercent;
        [SerializeField] MultiPositionConstraint drawNoDraw;
        public Color stringColor;
        [SerializeField] Material inputMaterial;
        [SerializeField] Transform BowTopEnd, BowDrawPoint, BowBotEnd, RigParent;
        [SerializeField] LineRenderer bowStringLinePre;

        LineRenderer bowStringLine;

        float previousDraw;

        [HideInInspector][SerializeField] bool started;

        private void Awake()
        {
            if (started == true && isTesting == false)
            {
                Begin();
            }
        }

        public void Start()
        {
            if (isTesting)
            {
                Begin();
            }
        }


        public void Begin()
        {

            bowStringLine = Instantiate(bowStringLinePre);
            SetStringMaterial(inputMaterial);

            previousDraw = 0f;
            started = true;
        }



        public override void UseWeapon()
        {
            base.UseWeapon();

            //bow specific stuff

        }

        public override void StopWeapon()
        {
            base.StopWeapon();

            //bow specific stuff

        }

        public override void UseEnchantment()
        {
            base.UseEnchantment();

            //bow specific stuff

        }
        public override void StopEnchantment()
        {
            base.StopEnchantment();

            //bow specific stuff

        }




        // Update is called once per frame
        void Update()
        {



            if (started)
            {
                bowStringLine.transform.position = Vector3.zero;
                bowStringLine.SetPosition(0, BowTopEnd.position);
                bowStringLine.SetPosition(1, BowDrawPoint.position);
                bowStringLine.SetPosition(2, BowBotEnd.position);


                if (previousDraw != bowDrawPercent)
                {
                    var sources = drawNoDraw.data.sourceObjects;

                    sources.SetWeight(0, 1f - bowDrawPercent);
                    sources.SetWeight(1, bowDrawPercent);
                    drawNoDraw.data.sourceObjects = sources;

                    previousDraw = bowDrawPercent;
                }

            }

        }


        public void SetStringMaterial(Material stringMat)
        {
            if (bowStringLine != null)
            {
                bowStringLine.material = stringMat;
                bowStringLine.material.SetColor("_Color", stringColor);
            }

        }

        public void rotateToStartPosition()
        {
            RigParent.localEulerAngles = new Vector3(90, 0, 0);
        }

        public void NockArrow(GameObject inputArrow)
        {
            //instatiate input arrow at BowDrawPoint as a child
            //then make it have no child when released
        }
        public void DeleteArrow()
        {

        }

        private void OnDestroy()
        {
            if (bowStringLine != null)
            {
                Destroy(bowStringLine.gameObject);

            }
        }
        private void OnDisable()
        {
            if (bowStringLine != null)
            {
                bowStringLine.gameObject.SetActive(false);

            }
        }
        private void OnEnable()
        {
            if (bowStringLine != null)
            {
                bowStringLine.gameObject.SetActive(true);

            }

        }

    }
}

