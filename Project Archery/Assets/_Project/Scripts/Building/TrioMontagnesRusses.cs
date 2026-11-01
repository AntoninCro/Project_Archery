using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le trio <b>Montagnes russes</b> (Chaîne de forçats + Tremplin), version <b>Grappin</b> (1er novembre au soir ;
    /// <c>Docs/Refonte_TowerDefense.md</c>, 12.29) : un circuit fermé, un ovale de bois rouge et or à 5 m au-dessus de la voie, où
    /// les wagons tournent sans arrêt. Sous chaque wagon, un grappin aimanté se balance d'un bord à l'autre de la voie et accroche les
    /// ennemis qu'il frôle (« Accroché ! ») ; ils pendent sous le wagon jusqu'au virage du côté de la forêt, qui les jette vers la
    /// forêt (« Au vol ! »).
    /// <list type="bullet">
    /// <item>1 : deux wagons, trois ennemis par grappin ;</item>
    /// <item>2 : trois wagons, quatre ennemis ; la grappe qui pend fouette au passage ceux qui sont dessous (dégâts, renversés) ;</item>
    /// <item>3, <b>le grand huit infernal</b> : en plus, toutes les 25 s, un train de six wagons file à toute vitesse sur la ligne
    /// droite qui va vers la forêt, accroche tout, monte le tremplin et saute dans le vide : il s'écrase sur le groupe le plus serré,
    /// l'atterrissage tue.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le premier essai du joueur (1er novembre) : la version d'avant (un wagon au ras de la voie, un looping) ne prenait « qu'un
    /// ennemi sur 30 » (il ramassait sur 8 m de rails, à 1,6 m d'une seule ligne, en 0,7 s), ses passagers étaient bugués, et elle
    /// avançait les ennemis vers le joueur. Le grappin balaie toute la largeur de la voie, les ennemis pendent (ils ne sont plus assis
    /// dans un wagon qui se retourne), et seule la ligne droite qui va vers la forêt ramasse : le circuit est posé dans un sens ou dans
    /// l'autre, la ligne droite qui ramasse est celle qui va vers la forêt (revu toutes les 2 s : le trio a pu être déplacé ou tourné).
    /// Le circuit est calculé ici (<see cref="OvalPoint"/>) avec les cotes de son modèle (<c>Tools/allure_tours/trios.py</c>, MR_…) :
    /// changer l'un, c'est changer l'autre. Un accroché est tenu (<see cref="Enemy.Hold"/>), puis lâché. Les boss et les volants ne
    /// sont jamais accrochés.
    /// </remarks>
    public class TrioMontagnesRusses : TowerTrio
    {
        // Les cotes du circuit, en repère local (MR_W, MR_H, MR_L, MR_RAMP, MR_HOOK_HIGH du modèle).
        const float k_HalfWidth = 1.2f;     // les deux lignes droites (x = ±), et le rayon des virages
        const float k_Height = 5f;          // le dessus des rails
        const float k_HalfLength = 4f;      // la demi-longueur des lignes droites
        const float k_RampLength = 4.5f;    // le tremplin du niveau 3 : sa longueur, sa montée
        const float k_RampRise = 2.5f;
        const float k_HookHigh = 3.6f;      // le grappin remonté, au-dessus du sol
        const float k_HookLow = 1.6f;       // le grappin descendu, qui ramasse

        // Un accroché pend sous le grappin : ses pieds à tant de mètres sous lui.
        const float k_Hang = 2f;

        static float Perimeter => 4f * k_HalfLength + 2f * Mathf.PI * k_HalfWidth;

        [Header("Le circuit")]
        [Tooltip("Le wagon du modèle (repère au ras des rails, au milieu ; ses enfants « Grappin » et « Chaîne ») : chaque wagon en est " +
                 "une copie ; il est caché une fois le trio construit.")]
        [SerializeField]
        Transform m_WagonTemplate;

        [Tooltip("Le matériau de la chaîne du grappin (dessinée en jeu).")]
        [SerializeField]
        Material m_ChainMaterial;

        [Tooltip("Vitesse (m/s) des wagons sur le circuit.")]
        [SerializeField]
        float m_TrackSpeed = 6.5f;

        [Tooltip("Wagons sur le circuit, par niveau.")]
        [SerializeField]
        int[] m_WagonCount = { 2, 3, 3 };

        [Header("Le grappin")]
        [Tooltip("Il accroche les ennemis à moins de tant de mètres (à plat) de lui, sur la ligne droite qui va vers la forêt ; tant " +
                 "par grappin, par niveau.")]
        [SerializeField]
        float m_HookRadius = 1.8f;

        [SerializeField]
        int[] m_HookSeats = { 3, 4, 4 };

        [Tooltip("Dégâts en accrochant, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_PickDamagePerLevel = { 15f, 20f, 25f };

        [Tooltip("Il se balance de tant de mètres de chaque côté de l'axe du circuit, en tant de secondes (un aller-retour).")]
        [SerializeField]
        float m_SwingWidth = 3.2f;

        [SerializeField]
        float m_SwingPeriod = 1.3f;

        [Tooltip("Vitesse (m/s) du treuil qui descend et remonte le grappin.")]
        [SerializeField]
        float m_ReelSpeed = 4f;

        [Tooltip("Au virage du côté de la forêt, les accrochés sont jetés vers la forêt à tant de m/s, et vers le haut.")]
        [SerializeField]
        float m_FlingSpeed = 9f;

        [SerializeField]
        float m_FlingLift = 4.5f;

        [Header("La grappe (niveau 2)")]
        [Tooltip("Deux accrochés au moins : ils fouettent les ennemis à tant de mètres (à plat), une fois par seconde chacun.")]
        [SerializeField]
        float m_WhipRadius = 2f;

        [SerializeField]
        float m_WhipDamage = 20f;

        [Header("Le grand huit infernal (niveau 3)")]
        [Tooltip("Temps (s) entre deux grands huit ; il faut au moins six ennemis près du circuit.")]
        [SerializeField]
        float m_GreatInterval = 25f;

        [SerializeField]
        int m_InfernalWagons = 6;

        [SerializeField]
        float m_InfernalSpeed = 13f;

        [Tooltip("Écart (m) entre deux wagons du train infernal.")]
        [SerializeField]
        float m_WagonGap = 1.7f;

        [Tooltip("Le saut : sa durée (s), jusqu'où (m) il cherche un groupe, le rayon (m) et les dégâts de l'écrasement.")]
        [SerializeField]
        float m_JumpTime = 1.8f;

        [SerializeField]
        float m_JumpRange = 30f;

        [SerializeField]
        float m_CrashRadius = 4f;

        [SerializeField]
        float m_CrashDamage = 80f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_DepartClip;

        [SerializeField]
        AudioClip m_PickClip;

        [SerializeField]
        AudioClip m_CrashClip;

        [SerializeField]
        AudioClip m_FlingClip;

        class Car
        {
            public Transform Body;
            public Transform Grapple;
            public LineRenderer Chain;
            public float Distance;
            public float Offset;
            public float Phase;
            public float Reel = k_HookHigh;
            public float SwingX;
            public float NextWhip;
            public readonly List<Enemy> Riders = new List<Enemy>();
            public bool Flying;
            public Vector3 From;
            public Vector3 Velocity;
            public float FlightTime;
        }

        class Infernal
        {
            public readonly List<Car> Cars = new List<Car>();
            public float Distance;
            public int Sign;
            public bool Jumped;
            public float JumpStart;
            public Vector3 Target;
        }

        static readonly Color k_Gold = new Color(1f, 0.82f, 0.35f, 1f);
        static readonly Color k_Red = new Color(0.9f, 0.3f, 0.25f, 1f);

        readonly List<Car> m_Cars = new List<Car>();
        readonly Dictionary<Enemy, float> m_Whipped = new Dictionary<Enemy, float>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        Infernal m_Infernal;
        int m_Sign = 1;
        float m_NextSign;
        float m_NextGreat;

        protected override void OnDestroy()
        {
            foreach (var car in m_Cars)
                DestroyCar(car);
            m_Cars.Clear();
            if (m_Infernal != null)
            {
                foreach (var car in m_Infernal.Cars)
                    DestroyCar(car);
                m_Infernal = null;
            }

            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt || m_WagonTemplate == null)
                return;

            var level = Mathf.Max(1, Level);
            var wanted = Mathf.Max(1, ValueFor(m_WagonCount, level, 2));
            if (m_Cars.Count != wanted)
                SpawnCars(wanted);

            if (Time.time >= m_NextSign)
            {
                m_NextSign = Time.time + 2f;
                m_Sign = Vector3.Dot(transform.forward, ForestwardAt(transform.position)) >= 0f ? 1 : -1;
            }

            // Pendant une amélioration, le circuit s'arrête (ses accrochés restent pendus).
            var running = !IsUnderConstruction;
            foreach (var car in m_Cars)
                UpdateCar(car, level, running);
            UpdateInfernal(level, running);

            if (running && level >= 3 && m_Infernal == null && Time.time >= m_NextGreat && EnemiesNear(transform.position, 14f).Count >= 6)
            {
                m_NextGreat = Time.time + m_GreatInterval / OverchargeRate;
                StartInfernal();
            }
        }

        // ---------------------------------------------------------------- le circuit

        // Le point de l'ovale à la distance s (repère local), et son sens : la ligne droite x = +W (vers +z), le virage du bout +z, la
        // ligne droite x = -W (vers -z), le virage du bout -z.
        static Vector3 OvalPoint(float s, out Vector3 tangent)
        {
            const float w = k_HalfWidth;
            const float l = k_HalfLength;
            s = Mathf.Repeat(s, Perimeter);
            if (s < 2f * l)
            {
                tangent = Vector3.forward;
                return new Vector3(w, k_Height, -l + s);
            }

            if (s < 2f * l + Mathf.PI * w)
            {
                var a = (s - 2f * l) / w;
                tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                return new Vector3(w * Mathf.Cos(a), k_Height, l + w * Mathf.Sin(a));
            }

            if (s < 4f * l + Mathf.PI * w)
            {
                tangent = Vector3.back;
                return new Vector3(-w, k_Height, l - (s - (2f * l + Mathf.PI * w)));
            }

            var b = (s - (4f * l + Mathf.PI * w)) / w;
            tangent = new Vector3(Mathf.Sin(b), 0f, -Mathf.Cos(b));
            return new Vector3(-w * Mathf.Cos(b), k_Height, -l - w * Mathf.Sin(b));
        }

        // La ligne droite qui ramasse : celle qui va vers la forêt ; le virage qui jette : celui du bout, côté forêt (son milieu).
        float PickStart => m_Sign > 0 ? 0f : 2f * k_HalfLength + Mathf.PI * k_HalfWidth;

        float ReleaseAt => m_Sign > 0 ? 2f * k_HalfLength + 0.5f * Mathf.PI * k_HalfWidth : 4f * k_HalfLength + 1.5f * Mathf.PI * k_HalfWidth;

        bool OnPickLeg(float s) => s >= PickStart && s <= PickStart + 2f * k_HalfLength;

        // Passé ce point du circuit pendant ce pas (le circuit boucle).
        static bool Crossed(float from, float to, float point) => to >= from ? from < point && point <= to : point > from || point <= to;

        void SpawnCars(int count)
        {
            foreach (var car in m_Cars)
                DestroyCar(car);
            m_Cars.Clear();
            for (var i = 0; i < count; i++)
            {
                var car = NewCar("Wagon du grappin");
                car.Distance = i * Perimeter / count;
                car.Phase = i * 2.1f;
                m_Cars.Add(car);
            }

            m_WagonTemplate.gameObject.SetActive(false);
        }

        // Une copie du wagon du modèle : sans colliders, sa chaîne fixe cachée (une chaîne dessinée suit le grappin).
        Car NewCar(string name)
        {
            var body = Instantiate(m_WagonTemplate.gameObject).transform;
            body.name = name;
            body.localScale = m_WagonTemplate.lossyScale;
            body.gameObject.SetActive(true);
            foreach (var collider in body.GetComponentsInChildren<Collider>())
                Destroy(collider);
            var fixedChain = body.Find("Chaîne");
            if (fixedChain != null)
                fixedChain.gameObject.SetActive(false);

            var chain = new GameObject("Chaîne du grappin").AddComponent<LineRenderer>();
            chain.transform.SetParent(body, false);
            chain.useWorldSpace = true;
            chain.positionCount = 2;
            chain.widthMultiplier = 0.07f;
            chain.sharedMaterial = m_ChainMaterial;
            chain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return new Car { Body = body, Grapple = body.Find("Grappin"), Chain = chain, SwingX = k_HalfWidth };
        }

        static void DestroyCar(Car car)
        {
            foreach (var rider in car.Riders)
            {
                if (rider != null && rider.IsHeld)
                    rider.Release(Vector3.zero);
            }

            car.Riders.Clear();
            if (car.Body != null)
                Destroy(car.Body.gameObject);
        }

        void UpdateCar(Car car, int level, bool running)
        {
            if (car.Body == null)
                return;

            var before = car.Distance;
            if (running)
                car.Distance = Mathf.Repeat(car.Distance + m_TrackSpeed * OverchargeRate * Time.deltaTime, Perimeter);
            var local = OvalPoint(car.Distance, out var tangent);
            if (running && Crossed(before, car.Distance, ReleaseAt))
                Fling(car);

            var picking = running && OnPickLeg(car.Distance) && car.Riders.Count < ValueFor(m_HookSeats, level, 3);
            var hook = PlaceCar(car, local, tangent, picking, m_SwingPeriod);
            if (picking)
                Grab(car, hook, level, m_HookRadius, ValueFor(m_HookSeats, level, 3));
            Hang(car, hook);
            if (running && level >= 2 && car.Riders.Count >= 2)
                Whip(car, hook);
        }

        // Le wagon à sa place sur le circuit ; son grappin : descendu et balancé d'un bord à l'autre de la voie quand il ramasse,
        // remonté sous le wagon sinon ; la chaîne entre les deux. Renvoie le grappin (repère du monde).
        Vector3 PlaceCar(Car car, Vector3 local, Vector3 tangent, bool picking, float period)
        {
            car.Body.SetPositionAndRotation(transform.TransformPoint(local), transform.rotation * Quaternion.LookRotation(tangent));
            car.Reel = Mathf.MoveTowards(car.Reel, picking ? k_HookLow : k_HookHigh, m_ReelSpeed * Time.deltaTime);
            var swing = picking ? m_SwingWidth * Mathf.Sin(2f * Mathf.PI * Time.time / Mathf.Max(0.1f, period) + car.Phase) : local.x;
            car.SwingX = Mathf.MoveTowards(car.SwingX, swing, 20f * Time.deltaTime);
            var hook = transform.TransformPoint(new Vector3(car.SwingX, local.y - (k_Height - car.Reel), local.z));
            if (car.Grapple != null)
                car.Grapple.SetPositionAndRotation(hook, car.Body.rotation);
            if (car.Chain != null && car.Chain.enabled)
            {
                car.Chain.SetPosition(0, car.Body.TransformPoint(new Vector3(0f, -0.1f, 0f)));
                car.Chain.SetPosition(1, hook + Vector3.up * 0.39f);
            }

            return hook;
        }

        // Le grappin accroche les ennemis au sol qu'il frôle, tant qu'il a de la place.
        void Grab(Car car, Vector3 hook, int level, float radius, int seats)
        {
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(hook, radius));
            foreach (var enemy in m_Scratch)
            {
                if (car.Riders.Count >= seats)
                    break;
                if (enemy.IsBoss || !enemy.CanBeLaunched || enemy.IsAirborne ||
                    !Hurt(enemy, ValueFor(m_PickDamagePerLevel, level, 15f), enemy.Center, Vector3.up) || !enemy.Hold(this))
                    continue;

                car.Riders.Add(enemy);
                FloatingText.Spawn(enemy.Center + Vector3.up, "Accroché !", k_Gold, 1.1f, 1f);
                Sfx.Play(m_PickClip, hook, 0.8f, Random.Range(0.9f, 1.15f));
            }
        }

        // Les accrochés pendent sous le grappin, en grappe, et se balancent ; jamais sous le sol.
        void Hang(Car car, Vector3 hook)
        {
            for (var i = car.Riders.Count - 1; i >= 0; i--)
            {
                var rider = car.Riders[i];
                if (rider == null || !rider.IsAlive || !rider.IsHeld)
                    car.Riders.RemoveAt(i);
            }

            var count = car.Riders.Count;
            var facing = car.Body.forward;
            facing.y = 0f;
            var yaw = facing.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(facing) : car.Body.rotation;
            for (var i = 0; i < count; i++)
            {
                var rider = car.Riders[i];
                rider.KeepHeld();
                var angle = i * 2f * Mathf.PI / count + Time.time * 1.5f;
                var ring = count > 1 ? 0.45f : 0f;
                var feet = hook + new Vector3(Mathf.Cos(angle) * ring, -k_Hang, Mathf.Sin(angle) * ring);
                feet.y = Mathf.Max(feet.y, transform.position.y + 0.05f);
                var sway = Quaternion.Euler(Mathf.Sin(Time.time * 3f + i) * 15f, 0f, Mathf.Sin(Time.time * 2.3f + i * 1.7f) * 20f);
                rider.transform.SetPositionAndRotation(feet, yaw * sway);
            }
        }

        // Au virage du côté de la forêt, les accrochés sont jetés vers la forêt, en tournoyant.
        void Fling(Car car)
        {
            if (car.Riders.Count == 0)
                return;

            var forest = ForestwardAt(car.Body.position);
            var velocity = forest * m_FlingSpeed + Vector3.up * m_FlingLift;
            foreach (var rider in car.Riders)
            {
                if (rider == null || !rider.IsHeld)
                    continue;
                rider.Release(velocity + Random.insideUnitSphere * 1.2f);
                rider.Tumble(new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized * 420f);
                FloatingText.Spawn(rider.Center + Vector3.up, "Au vol !", k_Red, 1.1f, 1f);
            }

            car.Riders.Clear();
            Sfx.Play(m_FlingClip, car.Body.position, 0.8f, Random.Range(0.9f, 1.1f));
        }

        // Niveau 2 : la grappe qui pend fouette ceux qui sont dessous (une fois par seconde chacun).
        void Whip(Car car, Vector3 hook)
        {
            if (Time.time < car.NextWhip)
                return;
            car.NextWhip = Time.time + 0.25f;

            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(hook, m_WhipRadius));
            foreach (var enemy in m_Scratch)
            {
                if (car.Riders.Contains(enemy) || enemy.IsAirborne || (m_Whipped.TryGetValue(enemy, out var next) && Time.time < next))
                    continue;

                m_Whipped[enemy] = Time.time + 1f;
                if (Hurt(enemy, m_WhipDamage, enemy.Center, enemy.transform.position - hook))
                    KnockDown(enemy, hook, 3f, 3f);
                FloatingText.Spawn(enemy.Center + Vector3.up, "Fouetté !", k_Red, 1f, 0.9f);
            }

            if (m_Whipped.Count > 64)
                ForgetDead(m_Whipped, m_Scratch);
        }

        // ---------------------------------------------------------------- le grand huit infernal (niveau 3)

        // Le chemin du train infernal (repère local) : la ligne droite qui va vers la forêt, puis le tremplin au bout.
        static Vector3 InfernalPoint(int sign, float d, out Vector3 tangent)
        {
            var x = sign * k_HalfWidth;
            if (d <= 2f * k_HalfLength)
            {
                tangent = new Vector3(0f, 0f, sign);
                return new Vector3(x, k_Height, sign * (-k_HalfLength + d));
            }

            var ramp = new Vector3(0f, k_RampRise, sign * k_RampLength);
            var t = Mathf.Clamp01((d - 2f * k_HalfLength) / ramp.magnitude);
            tangent = ramp.normalized;
            return new Vector3(x, k_Height, sign * k_HalfLength) + ramp * t;
        }

        static float InfernalEnd => 2f * k_HalfLength + Mathf.Sqrt(k_RampLength * k_RampLength + k_RampRise * k_RampRise);

        void StartInfernal()
        {
            var train = new Infernal { Sign = m_Sign };
            for (var i = 0; i < Mathf.Max(1, m_InfernalWagons); i++)
            {
                var car = NewCar("Wagon du grand huit");
                car.Offset = -i * m_WagonGap;
                car.Phase = i * 1.3f;
                car.Body.gameObject.SetActive(false);
                train.Cars.Add(car);
            }

            m_Infernal = train;
            HeadMessages.Show("Le grand huit infernal !", k_Red, 2.5f);
            Sfx.Play(m_DepartClip, transform.position + Vector3.up * k_Height, 1f, 0.8f);
        }

        void UpdateInfernal(int level, bool running)
        {
            var train = m_Infernal;
            if (train == null)
                return;

            if (train.Jumped)
            {
                if (UpdateJump(train))
                    return;
                foreach (var car in train.Cars)
                    DestroyCar(car);
                m_Infernal = null;
                return;
            }

            if (running)
                train.Distance += m_InfernalSpeed * Time.deltaTime;
            foreach (var car in train.Cars)
            {
                var d = train.Distance + car.Offset;
                if (car.Body == null)
                    continue;
                // Pas encore parti (derrière le premier) : caché.
                car.Body.gameObject.SetActive(d >= 0f);
                if (d < 0f)
                    continue;

                var local = InfernalPoint(train.Sign, d, out var tangent);
                var picking = running && d <= 2f * k_HalfLength;
                var hook = PlaceCar(car, local, tangent, picking, m_SwingPeriod * 0.55f);
                if (picking)
                    Grab(car, hook, level, m_HookRadius * 1.3f, ValueFor(m_HookSeats, level, 4));
                Hang(car, hook);
            }

            if (train.Distance >= InfernalEnd)
                Jump(train);
        }

        // Au bout du tremplin, tout le train part dans le vide vers le groupe le plus serré ; ses accrochés avec lui.
        void Jump(Infernal train)
        {
            train.Jumped = true;
            train.JumpStart = Time.time;
            var head = train.Cars.Count > 0 && train.Cars[0].Body != null ? train.Cars[0].Body.position : transform.position;
            train.Target = DensestGroup(head, m_JumpRange);
            train.Target.y = GroundY(train.Target, 1.5f);
            foreach (var car in train.Cars)
            {
                if (car.Body == null || !car.Body.gameObject.activeSelf)
                    continue;

                // Chaque wagon vise un peu à côté : le train s'écrase en tas.
                var target = train.Target + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                var time = m_JumpTime * Random.Range(0.9f, 1.1f);
                car.Flying = true;
                car.From = car.Body.position;
                car.FlightTime = time;
                car.Velocity = (target - car.From) / time - 0.5f * Physics.gravity * time;
                if (car.Chain != null)
                    car.Chain.enabled = false;
                foreach (var rider in car.Riders)
                {
                    if (rider != null && rider.IsHeld)
                        rider.Release(car.Velocity, true, 1f);
                }

                car.Riders.Clear();
            }

            FloatingText.Spawn(head + Vector3.up * 2f, "Dans le vide !", k_Red, 2f, 1.5f);
        }

        // Les wagons volent en cloche ; à l'arrivée, l'écrasement. Faux quand tout est retombé.
        bool UpdateJump(Infernal train)
        {
            var t = Time.time - train.JumpStart;
            var flying = false;
            foreach (var car in train.Cars)
            {
                if (car.Body == null || !car.Flying)
                    continue;

                if (t < car.FlightTime)
                {
                    flying = true;
                    car.Body.position = car.From + car.Velocity * t + 0.5f * Physics.gravity * t * t;
                    car.Body.Rotate(new Vector3(160f, 40f, 90f) * Time.deltaTime, Space.Self);
                    continue;
                }

                car.Flying = false;
                Crash(car.Body.position, car == train.Cars[0]);
                Destroy(car.Body.gameObject);
            }

            return flying;
        }

        void Crash(Vector3 point, bool first)
        {
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(point, m_CrashRadius));
            foreach (var enemy in m_Scratch)
            {
                if (Hurt(enemy, m_CrashDamage / 3f, point, enemy.transform.position - point))
                    KnockDown(enemy, point, 4f, 5f);
            }

            Burst(point + Vector3.up * 0.3f, new Color(0.45f, 0.3f, 0.2f, 1f), 30, 7f, 0.4f, 0.8f, 1.2f, false);
            if (!first)
                return;
            Explosion(point + Vector3.up * 0.5f, m_CrashRadius);
            Sfx.Play(m_CrashClip, point, 1f, 0.7f);
            HeadMessages.Show("CRASH !", k_Red, 1.5f);
        }

        // Le groupe le plus serré à portée de ce point : l'ennemi au sol qui a le plus de voisins à 3 m.
        static Vector3 DensestGroup(Vector3 around, float range)
        {
            var best = around;
            var bestCount = -1;
            var all = GroundEnemies();
            foreach (var enemy in all)
            {
                var flat = enemy.transform.position - around;
                flat.y = 0f;
                if (flat.sqrMagnitude > range * range)
                    continue;

                var count = 0;
                foreach (var other in all)
                {
                    var gap = other.transform.position - enemy.transform.position;
                    gap.y = 0f;
                    if (gap.sqrMagnitude < 9f)
                        count++;
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    best = enemy.transform.position;
                }
            }

            return best;
        }
    }
}
