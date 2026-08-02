using System.Collections.Generic;
using UnityEngine;

public sealed class PixelSurvivorGame : MonoBehaviour
{
    private enum GameMode
    {
        Menu,
        Playing,
        LevelUp,
        Paused,
        Won,
        Lost
    }

    private enum EnemyKind
    {
        Drone,
        Brute,
        Wool,
        Moth,
        Mushroom,
        Witch,
        Boss
    }

    private enum ProjectileKind
    {
        Spark,
        Cinder,
        HearthNote,
        Berry,
        Needle,
        CurseSeed,
        BossOrb
    }

    private enum UpgradeType
    {
        WandDamage,
        WandCount,
        WandCooldown,
        EmberRing,
        Magnet,
        Vitality,
        Haste,
        CinderVolley,
        HearthNotes,
        BerryBasket,
        SewingNeedle,
        FireflyJar,
        Armor,
        Recovery,
        Luck,
        Area
    }

    private sealed class Enemy
    {
        public int Id;
        public GameObject Object;
        public GameObject Shadow;
        public EnemyKind Kind;
        public float Health;
        public float Radius;
        public float Speed;
        public Vector2 Velocity;
        public float RingCooldown;
        public float AbilityTimer;
        public float ShotTimer;
        public float MovePhase;
        public float OrbitAngle;
        public int AbilityPhase;
        public Vector2 ChargeTarget;
        public Vector2 FormationOffset;
        public float StrafeSign;
        public float MotionSeed;
    }

    private sealed class Projectile
    {
        public GameObject Object;
        public Vector2 Velocity;
        public float Damage;
        public float Life;
        public int Pierce;
        public ProjectileKind Kind;
        public float HitRadius;
        public bool Homing;
        public readonly HashSet<int> HitIds = new HashSet<int>();
    }

    private sealed class EnemyProjectile
    {
        public GameObject Object;
        public Vector2 Velocity;
        public float Damage;
        public float Life;
        public float Radius;
    }

    private sealed class Effect
    {
        public GameObject Object;
        public Color Color;
        public Vector3 BaseScale;
        public float Life;
        public float MaxLife;
    }

    private sealed class Gem
    {
        public GameObject Object;
        public GameObject Shadow;
        public int Value;
    }

    private sealed class Chest
    {
        public GameObject Object;
        public GameObject Shadow;
        public bool Opened;
    }

    private sealed class UpgradeChoice
    {
        public UpgradeType Type;
        public string Tag;
        public string Title;
        public string Description;

        public UpgradeChoice(UpgradeType type, string tag, string title, string description)
        {
            Type = type;
            Tag = tag;
            Title = title;
            Description = description;
        }
    }

    private static readonly Color Ink = new Color(0.02f, 0.04f, 0.07f, 1f);
    private static readonly Color Arena = new Color(0.035f, 0.09f, 0.13f, 1f);
    private static readonly Color Grid = new Color(0.11f, 0.28f, 0.31f, 0.34f);
    private static readonly Color Cyan = new Color(0.22f, 0.94f, 0.88f, 1f);
    private static readonly Color CyanDim = new Color(0.10f, 0.48f, 0.55f, 1f);
    private static readonly Color Gold = new Color(1f, 0.72f, 0.25f, 1f);
    private static readonly Color Magenta = new Color(1f, 0.24f, 0.48f, 1f);
    private static readonly Color Flame = new Color(1f, 0.38f, 0.16f, 1f);
    private static readonly Color White = new Color(0.91f, 0.98f, 0.95f, 1f);
    private static readonly Color Muted = new Color(0.50f, 0.68f, 0.71f, 1f);
    private static readonly Color CardBlue = new Color(0.035f, 0.15f, 0.20f, 0.98f);
    private static readonly Color CardHover = new Color(0.07f, 0.28f, 0.32f, 1f);

    private const int TargetLevelTime = 180;
    private const int TargetSignals = 6;
    // Vampire Survivors-style world: the camera only shows a small slice of
    // this space while the courier can keep travelling and enemies stream in
    // from outside the current view.
    private const float ArenaLeft = -38f;
    private const float ArenaRight = 38f;
    private const float ArenaBottom = -26f;
    private const float ArenaTop = 26f;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<Projectile> projectiles = new List<Projectile>();
    private readonly List<EnemyProjectile> enemyProjectiles = new List<EnemyProjectile>();
    private readonly List<Effect> effects = new List<Effect>();
    private readonly List<Gem> gems = new List<Gem>();
    private readonly List<Chest> chests = new List<Chest>();
    private readonly List<GameObject> decoration = new List<GameObject>();
    private readonly List<UpgradeChoice> upgradeChoices = new List<UpgradeChoice>();
    private readonly List<Enemy> defeatedBuffer = new List<Enemy>();
    private readonly List<Vector2> signalSpots = new List<Vector2>
    {
        new Vector2(-5.2f, 2.4f), new Vector2(5.4f, 2.3f), new Vector2(5.5f, -2.5f),
        new Vector2(-5.1f, -2.2f), new Vector2(0.4f, 2.8f), new Vector2(0.2f, -0.9f)
    };

    private GameMode mode = GameMode.Menu;
    private Camera mainCamera;
    private GameObject player;
    private GameObject playerShadow;
    private LineRenderer pulseLine;
    private GameObject[] emberRingObjects;

    private Sprite playerSprite;
    private Sprite droneSprite;
    private Sprite bruteSprite;
    private Sprite boltSprite;
    private Sprite emberBoltSprite;
    private Sprite noteBoltSprite;
    private Sprite berryBoltSprite;
    private Sprite needleSprite;
    private Sprite curseSeedSprite;
    private Sprite bossOrbSprite;
    private Sprite hitEffectSprite;
    private Sprite bossBurstSprite;
    private Sprite telegraphSprite;
    private Sprite gemSprite;
    private Sprite chestSprite;
    private Sprite ringSprite;
    private Sprite whiteSprite;
    private Sprite shadowSprite;
    private Texture2D arenaTexture;
    private GameObject arenaArt;
    private Texture2D hudPanelTexture;
    private Texture2D hudLineTexture;

    private Texture2D overlayTexture;
    private Texture2D cardTexture;
    private Texture2D cardHoverTexture;
    private Texture2D meterBackTexture;
    private Texture2D meterXpTexture;
    private Texture2D meterHealthTexture;

    private GUIStyle titleStyle;
    private GUIStyle hudStyle;
    private GUIStyle mutedStyle;
    private GUIStyle overlayTitleStyle;
    private GUIStyle overlayBodyStyle;
    private GUIStyle overlayPanelStyle;
    private GUIStyle cardStyle;
    private GUIStyle cardTagStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle cardBodyStyle;
    private GUIStyle toastStyle;
    private GUIStyle meterBackStyle;
    private GUIStyle meterXpStyle;
    private GUIStyle meterHealthStyle;
    private bool stylesBuilt;

    private Vector2 playerVelocity;
    private Vector2 spawnPoint = new Vector2(0f, -2.65f);
    private Vector2 lastAim = Vector2.up;
    private int nextEnemyId = 1;
    private int level;
    private int xp;
    private int xpToNext;
    private int kills;
    private int chestsOpened;
    private int score;
    private int wandLevel;
    private int projectileCount;
    private int projectilePierce;
    private int maxHealth;
    private float playerHealth;
    private float weaponDamage;
    private float weaponCooldown;
    private float weaponTimer;
    private float projectileSpeed;
    private float moveSpeed;
    private float magnetRange;
    private float elapsed;
    private float timeLeft;
    private float spawnTimer;
    private float chestTimer;
    private float contactCooldown;
    private float pulseEnergy;
    private float pulseCooldown;
    private float pulseTimer;
    private float pulseRadius;
    private float toastTimer;
    private string toastMessage = string.Empty;
    private float emberDamage;
    private bool hasEmberRing;
    private bool cinderVolley;
    private bool hasHearthNotes;
    private bool hasBerryBasket;
    private bool hasSewingNeedle;
    private bool hasFireflyJar;
    private int hearthNotesLevel;
    private int berryBasketLevel;
    private int sewingNeedleLevel;
    private int fireflyJarLevel;
    private int armorLevel;
    private int luckLevel;
    private float recoveryRate;
    private float areaMultiplier;
    private float hearthNotesTimer;
    private float berryBasketTimer;
    private float sewingNeedleTimer;
    private float fireflyTimer;
    private GameObject[] fireflyObjects;
    private bool bossActive;
    private bool bossSpawned;
    private float bossHealth;
    private float bossMaxHealth;
    private string bossDisplayName = string.Empty;
    private float bossWarningTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (GameObject.Find("PixelSignalNightfall") == null)
        {
            new GameObject("PixelSignalNightfall").AddComponent<PixelSurvivorGame>();
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        ConfigureCamera();
        BuildSprites();
        BuildArena();
        BuildActors();
        hudPanelTexture = CreateTexture(new Color(0.008f, 0.035f, 0.055f, 0.78f));
        hudLineTexture = CreateTexture(new Color(Cyan.r, Cyan.g, Cyan.b, 0.46f));
        ResetRun();
    }

    private void ConfigureCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5.2f;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Ink;
    }

    private void BuildSprites()
    {
        whiteSprite = CreatePixelSprite("White Pixel", new[] { "W" }, new Dictionary<char, Color> { { 'W', Color.white } }, 1f);
        shadowSprite = CreatePixelSprite(
            "Ground Shadow",
            new[] { ".......", ".SSSSS.", "SSSSSSS", ".SSSSS.", "......." },
            new Dictionary<char, Color> { { 'S', new Color(0.01f, 0.02f, 0.03f, 0.72f) } }, 16f);
        playerSprite = CreatePixelSprite(
            "Night Courier",
            new[]
            {
                ".....C......", "....CCC.....", "...CCWCC....", "..CCWWWWCC..",
                ".CCWWWWWWCC.", "CCWWWWWWWWCC", ".CCWWWWWWCC.", "..CCWWWWCC..",
                "...CCWWCC...", "....CCCC....", ".....CC.....", "............"
            },
            new Dictionary<char, Color> { { 'C', Cyan }, { 'W', White } }, 16f);

        droneSprite = CreatePixelSprite(
            "Red Drone",
            new[]
            {
                ".....M.....", "...MMMMM...", "..MMRRRMM..", ".MMRRRRRMM.",
                "MMRRRRRRRMM", "MMRRRRRRRMM", ".MMRRRRRMM.", "..MMRRRMM..",
                "...MMMMM...", ".....M.....", "..........."
            },
            new Dictionary<char, Color> { { 'M', Magenta }, { 'R', Flame } }, 16f);

        bruteSprite = CreatePixelSprite(
            "Brute Drone",
            new[]
            {
                "....G..G....", "...GGGGGG...", "..GGBBBBGG..", ".GGBBBBBBGG.",
                "GGBBBBBBBBGG", "GGBBBBBBBBGG", ".GGBBBBBBGG.", "..GGBBBBGG..",
                "...GGGGGG...", "....G..G....", "............"
            },
            new Dictionary<char, Color> { { 'G', Gold }, { 'B', new Color(0.72f, 0.18f, 0.36f, 1f) } }, 16f);

        boltSprite = CreatePixelSprite(
            "Spark Bolt",
            new[] { "..C..", ".CCC.", "CCWCC", ".CCC.", "..C.." },
            new Dictionary<char, Color> { { 'C', Cyan }, { 'W', White } }, 16f);

        emberBoltSprite = CreatePixelSprite(
            "Cinder Bolt",
            new[] { "..F..", ".FFF.", "FFGFF", ".FFF.", "..F.." },
            new Dictionary<char, Color> { { 'F', Flame }, { 'G', Gold } }, 16f);

        noteBoltSprite = CreatePixelSprite(
            "Hearth Note",
            new[] { "..C..", ".CCC.", "CCWCC", ".CCC.", "..C.." },
            new Dictionary<char, Color> { { 'C', new Color(1f, 0.55f, 0.74f, 1f) }, { 'W', White } }, 16f);
        berryBoltSprite = CreatePixelSprite(
            "Berry Toss",
            new[] { ".FF.", "FGGF", "FGGF", ".FF.", "...." },
            new Dictionary<char, Color> { { 'F', Magenta }, { 'G', Gold } }, 16f);
        needleSprite = CreatePixelSprite(
            "Sewing Needle",
            new[] { "....W", "...WW", "..WW.", ".WW..", "W...." },
            new Dictionary<char, Color> { { 'W', White } }, 16f);
        curseSeedSprite = CreatePixelSprite(
            "Curse Seed",
            new[] { "..F..", ".FGF.", "FGGFG", ".FGF.", "..F.." },
            new Dictionary<char, Color> { { 'F', new Color(0.76f, 0.32f, 0.86f, 1f) }, { 'G', Magenta } }, 16f);
        bossOrbSprite = CreatePixelSprite(
            "Boss Orb",
            new[] { "..G..", ".GFG.", "GFFF G".Replace(" ", string.Empty), ".GFG.", "..G.." },
            new Dictionary<char, Color> { { 'F', Flame }, { 'G', Gold } }, 16f);
        hitEffectSprite = CreatePixelSprite(
            "Hit Spark",
            new[] { "..W..", ".W.W.", "W...W", ".W.W.", "..W.." },
            new Dictionary<char, Color> { { 'W', White } }, 16f);
        bossBurstSprite = CreatePixelSprite(
            "Boss Burst",
            new[] { "...G...", ".G...G.", "G..F..G", "...F...", "G..F..G", ".G...G.", "...G..." },
            new Dictionary<char, Color> { { 'F', Flame }, { 'G', Gold } }, 16f);
        telegraphSprite = CreatePixelSprite(
            "Charge Telegraph",
            new[] { ".GGGGG.", "G.....G", "G.....G", "G.....G", "G.....G", "G.....G", ".GGGGG." },
            new Dictionary<char, Color> { { 'G', new Color(1f, 0.40f, 0.62f, 0.65f) } }, 16f);

        gemSprite = CreatePixelSprite(
            "Signal Shard",
            new[] { "..G..", ".GGG.", "GGYGG", ".GGG.", "..G.." },
            new Dictionary<char, Color> { { 'G', Gold }, { 'Y', White } }, 16f);

        chestSprite = CreatePixelSprite(
            "Night Chest",
            new[] { "..GGGG..", ".GYYYYG.", "GYYYYYYG", "GYYWYYYG", "GGGGGGGG", "........" },
            new Dictionary<char, Color> { { 'G', Gold }, { 'Y', new Color(0.55f, 0.26f, 0.08f, 1f) }, { 'W', White } }, 16f);

        ringSprite = CreatePixelSprite(
            "Ember Ring Orb",
            new[] { ".FF.", "FGGF", ".FF.", "...." },
            new Dictionary<char, Color> { { 'F', Flame }, { 'G', Gold } }, 16f);
    }

    private void BuildArena()
    {
        arenaTexture = Resources.Load<Texture2D>("NightfallArena");
        if (arenaTexture != null)
        {
            arenaTexture.filterMode = FilterMode.Point;
            arenaTexture.wrapMode = TextureWrapMode.Clamp;
            Sprite arenaSprite = Sprite.Create(
                arenaTexture,
                new Rect(0f, 0f, arenaTexture.width, arenaTexture.height),
                new Vector2(0.5f, 0.5f),
                64f);
            arenaArt = CreateSpriteObject("Nightfall Arena Art", arenaSprite, Vector2.zero, 1f, -31);
            arenaArt.transform.localScale = new Vector3(
                15.4f / (arenaTexture.width / 64f),
                8.7f / (arenaTexture.height / 64f),
                1f);
        }

        Color arenaOverlay = new Color(Arena.r, Arena.g, Arena.b, 0.28f);
        CreateRect("Arena Surface", Vector2.zero, new Vector2(15.4f, 8.7f), arenaOverlay, -30);
        for (float x = -7f; x <= 7f; x += 1f) decoration.Add(CreateRect("Grid Vertical", new Vector2(x, 0f), new Vector2(0.018f, 8.2f), Grid, -20));
        for (float y = -3.5f; y <= 3.5f; y += 1f) decoration.Add(CreateRect("Grid Horizontal", new Vector2(0f, y), new Vector2(14.7f, 0.018f), Grid, -20));

        Color border = new Color(0.20f, 0.72f, 0.74f, 0.82f);
        decoration.Add(CreateRect("Border Top", new Vector2(0f, ArenaTop), new Vector2(15f, 0.06f), border, -10));
        decoration.Add(CreateRect("Border Bottom", new Vector2(0f, ArenaBottom), new Vector2(15f, 0.06f), border, -10));
        decoration.Add(CreateRect("Border Left", new Vector2(ArenaLeft, 0f), new Vector2(0.06f, 8.15f), border, -10));
        decoration.Add(CreateRect("Border Right", new Vector2(ArenaRight, 0f), new Vector2(0.06f, 8.15f), border, -10));

        Vector2[] stars =
        {
            new Vector2(-6.7f, 3.2f), new Vector2(-3.5f, 3.25f), new Vector2(-1.1f, 2.3f),
            new Vector2(2.7f, 3.5f), new Vector2(6.4f, 1.2f), new Vector2(6.8f, -1.6f),
            new Vector2(3.3f, -3.4f), new Vector2(-1.8f, -3.3f), new Vector2(-6.6f, -1.1f)
        };
        foreach (Vector2 star in stars) decoration.Add(CreateRect("Signal Dust", star, new Vector2(0.08f, 0.08f), CyanDim, -5));
    }

    private void BuildActors()
    {
        player = CreateSpriteObject("Night Courier", playerSprite, spawnPoint, 0.95f, 10);
        playerShadow = CreateShadow("Courier Ground Shadow", spawnPoint + new Vector2(0.08f, -0.16f), 0.95f, 8);
        GameObject pulseObject = new GameObject("Pulse Ring");
        pulseLine = pulseObject.AddComponent<LineRenderer>();
        pulseLine.positionCount = 40;
        pulseLine.loop = true;
        pulseLine.useWorldSpace = false;
        pulseLine.startWidth = 0.045f;
        pulseLine.endWidth = 0.045f;
        pulseLine.material = new Material(Shader.Find("Sprites/Default"));
        pulseLine.startColor = Cyan;
        pulseLine.endColor = Cyan;
        pulseObject.SetActive(false);
    }

    private void BuildStyles()
    {
        overlayTexture = CreateTexture(new Color(0.012f, 0.035f, 0.055f, 0.95f));
        cardTexture = CreateTexture(CardBlue);
        cardHoverTexture = CreateTexture(CardHover);
        meterBackTexture = CreateTexture(new Color(0.02f, 0.08f, 0.10f, 0.9f));
        meterXpTexture = CreateTexture(Cyan);
        meterHealthTexture = CreateTexture(Magenta);

        overlayPanelStyle = new GUIStyle();
        overlayPanelStyle.normal.background = overlayTexture;
        cardStyle = new GUIStyle();
        cardStyle.normal.background = cardTexture;
        cardStyle.hover.background = cardHoverTexture;
        cardStyle.active.background = cardHoverTexture;
        cardStyle.padding = new RectOffset(14, 14, 14, 14);

        titleStyle = MakeStyle(20, Cyan, TextAnchor.MiddleLeft, true);
        hudStyle = MakeStyle(14, White, TextAnchor.MiddleLeft, true);
        mutedStyle = MakeStyle(12, Muted, TextAnchor.MiddleLeft, false);
        overlayTitleStyle = MakeStyle(29, Cyan, TextAnchor.MiddleCenter, true);
        overlayBodyStyle = MakeStyle(15, White, TextAnchor.MiddleCenter, false);
        cardTagStyle = MakeStyle(11, Gold, TextAnchor.UpperLeft, true);
        cardTitleStyle = MakeStyle(18, White, TextAnchor.UpperLeft, true);
        cardBodyStyle = MakeStyle(12, Muted, TextAnchor.UpperLeft, false);
        toastStyle = MakeStyle(15, Gold, TextAnchor.MiddleCenter, true);

        meterBackStyle = new GUIStyle();
        meterBackStyle.normal.background = meterBackTexture;
        meterXpStyle = new GUIStyle();
        meterXpStyle.normal.background = meterXpTexture;
        meterHealthStyle = new GUIStyle();
        meterHealthStyle.normal.background = meterHealthTexture;
    }

    private GUIStyle MakeStyle(int size, Color color, TextAnchor anchor, bool bold)
    {
        GUIStyle style = new GUIStyle()
        {
            fontSize = size,
            alignment = anchor,
            wordWrap = true,
            richText = false,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal
        };
        style.normal.textColor = color;
        return style;
    }

    private void ResetRun()
    {
        mode = GameMode.Menu;
        elapsed = 0f;
        timeLeft = TargetLevelTime;
        level = 1;
        xp = 0;
        xpToNext = 12;
        kills = 0;
        chestsOpened = 0;
        score = 0;
        wandLevel = 1;
        projectileCount = 1;
        projectilePierce = 1;
        // Give the first level-up window enough breathing room for a new player.
        // The run should teach movement and pulse timing before contact damage
        // becomes the dominant outcome.
        maxHealth = 180;
        playerHealth = maxHealth;
        weaponDamage = 14f;
        weaponCooldown = 0.72f;
        weaponTimer = 0.25f;
        projectileSpeed = 7.2f;
        moveSpeed = 4.1f;
        magnetRange = 0.95f;
        emberDamage = 10f;
        hasEmberRing = false;
        cinderVolley = false;
        hasHearthNotes = false;
        hasBerryBasket = false;
        hasSewingNeedle = false;
        hasFireflyJar = false;
        hearthNotesLevel = 0;
        berryBasketLevel = 0;
        sewingNeedleLevel = 0;
        fireflyJarLevel = 0;
        armorLevel = 0;
        luckLevel = 0;
        recoveryRate = 0f;
        areaMultiplier = 1f;
        hearthNotesTimer = 1.5f;
        berryBasketTimer = 2.8f;
        sewingNeedleTimer = 2.0f;
        fireflyTimer = 0.4f;
        bossActive = false;
        bossSpawned = false;
        bossHealth = 0f;
        bossMaxHealth = 1f;
        bossDisplayName = string.Empty;
        bossWarningTimer = 0f;
        playerVelocity = Vector2.zero;
        lastAim = Vector2.up;
        nextEnemyId = 1;
        spawnTimer = 0.35f;
        chestTimer = 24f;
        contactCooldown = 0f;
        pulseEnergy = 100f;
        pulseCooldown = 0f;
        pulseTimer = 0f;
        toastTimer = 0f;
        toastMessage = string.Empty;
        upgradeChoices.Clear();

        player.transform.position = new Vector3(spawnPoint.x, spawnPoint.y, 0f);
        player.transform.rotation = Quaternion.identity;
        SyncShadow(player, playerShadow, new Vector2(0.08f, -0.16f));
        player.SetActive(true);
        ClearHazards();
        ClearProjectiles();
        ClearEnemyProjectiles();
        ClearEffects();
        ClearGems();
        ClearChests();
        ClearRingObjects();
        ClearFireflyObjects();
        SpawnEnemy(new Vector2(-2.8f, 2.8f));
        SpawnEnemy(new Vector2(5.3f, 1.0f));
        SpawnEnemy(new Vector2(3.2f, -2.9f));
        SpawnEnemy(new Vector2(-5.3f, -1.0f));
        pulseLine.gameObject.SetActive(false);
    }

    private void StartRun()
    {
        ResetRun();
        mode = GameMode.Playing;
    }

    private void Update()
    {
        if (mode == GameMode.Menu)
        {
            AnimateDecorativeState();
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) StartRun();
            return;
        }

        if (mode == GameMode.LevelUp)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) ApplyUpgrade(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) ApplyUpgrade(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) ApplyUpgrade(2);
            return;
        }

        if (mode == GameMode.Paused)
        {
            if (Input.GetKeyDown(KeyCode.P)) mode = GameMode.Playing;
            return;
        }

        if (mode == GameMode.Won || mode == GameMode.Lost)
        {
            if (Input.GetKeyDown(KeyCode.R)) StartRun();
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            mode = GameMode.Paused;
            return;
        }

        if (Input.GetKeyDown(KeyCode.F)) Screen.fullScreen = !Screen.fullScreen;

        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        elapsed += dt;
        timeLeft = Mathf.Max(0f, TargetLevelTime - elapsed);
        pulseEnergy = Mathf.Min(100f, pulseEnergy + dt * 7.5f);
        pulseCooldown = Mathf.Max(0f, pulseCooldown - dt);
        contactCooldown = Mathf.Max(0f, contactCooldown - dt);
        toastTimer = Mathf.Max(0f, toastTimer - dt);
        bossWarningTimer = Mathf.Max(0f, bossWarningTimer - dt);
        if (recoveryRate > 0f) playerHealth = Mathf.Min(maxHealth, playerHealth + recoveryRate * dt);

        MovePlayer(dt);
        UpdateSpawnPressure(dt);
        MoveEnemies(dt);
        FireWeapon(dt);
        UpdateProjectiles(dt);
        UpdateEnemyProjectiles(dt);
        UpdateGems(dt);
        UpdateChests(dt);
        UpdateEmberRing(dt);
        UpdateFireflyJar(dt);
        UpdatePulse(dt);
        UpdateEffects(dt);
        SyncBossTelemetry();

        if (timeLeft <= 0f) Finish(GameMode.Won);
    }

    private void MovePlayer(float dt)
    {
        float horizontal = 0f;
        float vertical = 0f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) vertical += 1f;

        Vector2 input = new Vector2(horizontal, vertical);
        if (input.sqrMagnitude > 1f) input.Normalize();
        Vector2 desired = input * moveSpeed;
        // Keep the control responsive and grounded: a quick start, a firm stop,
        // and no fish-like sideways drift after the player releases the key.
        playerVelocity = Vector2.MoveTowards(playerVelocity, desired, 28f * dt);
        if (input.sqrMagnitude < 0.01f) playerVelocity = Vector2.MoveTowards(playerVelocity, Vector2.zero, 38f * dt);

        Vector3 next = player.transform.position + new Vector3(playerVelocity.x, playerVelocity.y, 0f) * dt;
        next.x = Mathf.Clamp(next.x, ArenaLeft + 0.55f, ArenaRight - 0.55f);
        next.y = Mathf.Clamp(next.y, ArenaBottom + 0.55f, ArenaTop - 0.55f);
        player.transform.position = next;
        SyncShadow(player, playerShadow, new Vector2(0.08f, -0.16f));
        if (input.sqrMagnitude > 0.01f && playerVelocity.sqrMagnitude > 0.01f)
        {
            lastAim = playerVelocity.normalized;
        }

        if (contactCooldown > 0f) player.SetActive(Mathf.FloorToInt(contactCooldown * 14f) % 2 == 0);
        else player.SetActive(true);
    }

    private void UpdateSpawnPressure(float dt)
    {
        if (!bossSpawned && elapsed >= 42f) SpawnBoss();

        spawnTimer -= dt;
        if (spawnTimer <= 0f && enemies.Count < 86)
        {
            SpawnEnemy(Vector2.zero);
            if (elapsed > 64f && Random.value < 0.34f && enemies.Count < 86) SpawnEnemy(Vector2.zero);
            if (elapsed > 112f && Random.value < 0.22f && enemies.Count < 86) SpawnEnemy(Vector2.zero);
            spawnTimer = Mathf.Max(0.24f, 0.80f - elapsed * 0.0028f);
        }

        chestTimer -= dt;
        if (chestTimer <= 0f && chests.Count < 2)
        {
            SpawnChest(Vector2.zero);
            chestTimer = 38f;
        }
    }

    private void SpawnEnemy(Vector2 suggestedPosition)
    {
        Vector2 position = suggestedPosition;
        if (suggestedPosition == Vector2.zero)
        {
            Vector2 playerPosition = player != null ? (Vector2)player.transform.position : spawnPoint;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(10.5f, 13.5f);
            position = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            position.x = Mathf.Clamp(position.x, ArenaLeft + 0.8f, ArenaRight - 0.8f);
            position.y = Mathf.Clamp(position.y, ArenaBottom + 0.8f, ArenaTop - 0.8f);
        }

        EnemyKind kind = ChooseEnemyKind();
        float healthScale = 1f + elapsed * 0.005f;
        float radius;
        float speed;
        float health;
        float scale;
        switch (kind)
        {
            case EnemyKind.Brute:
                radius = 0.68f;
                speed = 0.72f + elapsed * 0.001f;
                health = 78f;
                scale = 1.02f;
                break;
            case EnemyKind.Wool:
                radius = 0.58f;
                speed = 0.70f + elapsed * 0.001f;
                health = 34f;
                scale = 0.90f;
                break;
            case EnemyKind.Moth:
                radius = 0.44f;
                speed = 1.18f + elapsed * 0.002f;
                health = 24f;
                scale = 0.78f;
                break;
            case EnemyKind.Mushroom:
                radius = 0.50f;
                speed = 1.38f + elapsed * 0.0015f;
                health = 30f;
                scale = 0.84f;
                break;
            case EnemyKind.Witch:
                radius = 0.52f;
                speed = 0.62f + elapsed * 0.0008f;
                health = 48f;
                scale = 0.88f;
                break;
            default:
                radius = 0.46f;
                speed = 1.08f + elapsed * 0.002f;
                health = 18f;
                scale = 0.78f;
                break;
        }
        Enemy enemy = new Enemy
        {
            Id = nextEnemyId++,
            Kind = kind,
            Radius = radius,
            Speed = speed,
            Health = health * healthScale,
            Velocity = Vector2.zero,
            AbilityTimer = Random.Range(1.6f, 4.4f),
            ShotTimer = Random.Range(1.4f, 2.8f),
            MovePhase = Random.Range(0f, Mathf.PI * 2f),
            OrbitAngle = Random.Range(0f, Mathf.PI * 2f),
            FormationOffset = Vector2.zero,
            StrafeSign = Random.value < 0.5f ? -1f : 1f,
            MotionSeed = Random.Range(0f, Mathf.PI * 2f)
        };
        enemy.FormationOffset = Random.insideUnitCircle * 0.8f;
        enemy.Object = CreateSpriteObject(EnemyObjectName(kind), EnemySprite(kind), position, scale, 9);
        enemy.Shadow = CreateShadow(EnemyObjectName(kind) + " Ground Shadow", position + new Vector2(0.08f, -0.16f), scale, 8);
        enemies.Add(enemy);
    }

    private EnemyKind ChooseEnemyKind()
    {
        float roll = Random.value;
        if (elapsed < 14f) return EnemyKind.Drone;
        if (elapsed < 28f) return roll < 0.24f ? EnemyKind.Wool : EnemyKind.Drone;
        if (elapsed < 45f)
        {
            if (roll < 0.16f) return EnemyKind.Brute;
            if (roll < 0.42f) return EnemyKind.Wool;
            if (roll < 0.68f) return EnemyKind.Mushroom;
            return EnemyKind.Drone;
        }
        if (elapsed < 82f)
        {
            if (roll < 0.12f) return EnemyKind.Brute;
            if (roll < 0.30f) return EnemyKind.Witch;
            if (roll < 0.52f) return EnemyKind.Moth;
            if (roll < 0.72f) return EnemyKind.Mushroom;
            return EnemyKind.Wool;
        }
        if (roll < 0.16f) return EnemyKind.Brute;
        if (roll < 0.35f) return EnemyKind.Witch;
        if (roll < 0.56f) return EnemyKind.Moth;
        if (roll < 0.76f) return EnemyKind.Mushroom;
        return EnemyKind.Wool;
    }

    private string EnemyObjectName(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Brute: return "Brute Drone";
            case EnemyKind.Wool: return "Wool Sprite";
            case EnemyKind.Moth: return "Lantern Moth";
            case EnemyKind.Mushroom: return "Mushroom Thief";
            case EnemyKind.Witch: return "Hedge Witch";
            case EnemyKind.Boss: return "Mallow Warden";
            default: return "Red Drone";
        }
    }

    private Sprite EnemySprite(EnemyKind kind)
    {
        return kind == EnemyKind.Brute || kind == EnemyKind.Boss ? bruteSprite : droneSprite;
    }

    private void SpawnBoss()
    {
        bossSpawned = true;
        bossActive = true;
        bossDisplayName = "MALLOW WARDEN";
        bossMaxHealth = 520f + elapsed * 2.2f;
        bossHealth = bossMaxHealth;
        bossWarningTimer = 4.2f;
        Vector2 playerPosition = player != null ? (Vector2)player.transform.position : spawnPoint;
        float bossAngle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 position = playerPosition + new Vector2(Mathf.Cos(bossAngle), Mathf.Sin(bossAngle)) * 12.5f;
        position.x = Mathf.Clamp(position.x, ArenaLeft + 1.2f, ArenaRight - 1.2f);
        position.y = Mathf.Clamp(position.y, ArenaBottom + 1.2f, ArenaTop - 1.2f);
        Enemy boss = new Enemy
        {
            Id = nextEnemyId++,
            Kind = EnemyKind.Boss,
            Radius = 1.08f,
            Speed = 0.60f,
            Health = bossMaxHealth,
            Velocity = Vector2.zero,
            AbilityTimer = 1.2f,
            ShotTimer = 2.0f,
            MovePhase = 0.0f,
            OrbitAngle = 0.0f,
            FormationOffset = Vector2.zero,
            StrafeSign = 1f,
            MotionSeed = 0.6f
        };
        boss.Object = CreateSpriteObject("Mallow Warden", bossSpriteFallback(), position, 1.42f, 10);
        boss.Shadow = CreateShadow("Mallow Warden Ground Shadow", position + new Vector2(0.08f, -0.20f), 1.42f, 8);
        enemies.Add(boss);
        SpawnEffect("Boss Burst", position, bossBurstSprite, 2.2f, new Color(1f, 0.50f, 0.72f, 0.92f), 0.75f);
        toastMessage = "MALLOW WARDEN APPROACHES  //  HOLD THE LIGHT";
        toastTimer = 4.2f;
    }

    private Sprite bossSpriteFallback()
    {
        return bruteSprite;
    }

    private void MoveEnemies(float dt)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy.Health <= 0f || enemy.Object == null) continue;
            Vector2 playerPosition = player.transform.position;
            Vector2 enemyPosition = enemy.Object.transform.position;
            Vector2 toPlayer = playerPosition - enemyPosition;
            float distance = toPlayer.magnitude;
            Vector2 direction = distance > 0.001f ? toPlayer / distance : Vector2.zero;
            Vector2 desiredDirection = direction;
            float desiredSpeed = enemy.Speed;
            float acceleration = 5.5f;
            Vector2 tangent = new Vector2(-direction.y, direction.x) * enemy.StrafeSign;
            float motionWave = Mathf.Sin(enemy.MovePhase * 1.65f + enemy.MotionSeed + elapsed * 0.55f);

            enemy.AbilityTimer -= dt;
            enemy.ShotTimer -= dt;
            enemy.MovePhase += dt;

            switch (enemy.Kind)
            {
                case EnemyKind.Drone:
                    // The basic mote now zig-zags through the arena instead of
                    // locking onto the courier on a straight line.
                    desiredDirection = (direction * 0.93f + tangent * motionWave * 0.42f).normalized;
                    desiredSpeed = enemy.Speed * (1.02f + Mathf.Abs(motionWave) * 0.12f);
                    break;
                case EnemyKind.Wool:
                    float woolOrbit = enemy.MovePhase * 0.72f + enemy.MotionSeed;
                    Vector2 rotatingFormation = new Vector2(Mathf.Cos(woolOrbit), Mathf.Sin(woolOrbit * 1.17f)) * 0.48f;
                    Vector2 woolTarget = playerPosition + (enemy.FormationOffset + rotatingFormation) * (0.75f + Mathf.Sin(elapsed * 1.4f + enemy.Id) * 0.18f);
                    Vector2 woolToTarget = woolTarget - enemyPosition;
                    desiredDirection = woolToTarget.sqrMagnitude > 0.001f
                        ? (woolToTarget.normalized + tangent * motionWave * 0.32f).normalized
                        : direction;
                    desiredSpeed = enemy.Speed * (0.92f + Mathf.Abs(motionWave) * 0.20f);
                    break;
                case EnemyKind.Moth:
                    if (enemy.AbilityPhase == 0 && enemy.AbilityTimer <= 0f)
                    {
                        enemy.AbilityPhase = 1;
                        enemy.AbilityTimer = 0.52f;
                        enemy.ChargeTarget = playerPosition;
                        SpawnEffect("Charge Telegraph", enemyPosition, telegraphSprite, 1.0f, new Color(1f, 0.55f, 0.76f, 0.76f), 0.54f);
                    }
                    if (enemy.AbilityPhase == 1)
                    {
                        desiredDirection = Vector2.zero;
                        desiredSpeed = 0f;
                        if (enemy.AbilityTimer <= 0f)
                        {
                            enemy.AbilityPhase = 2;
                            enemy.AbilityTimer = 0.82f;
                        }
                    }
                    else if (enemy.AbilityPhase == 2)
                    {
                        Vector2 dive = enemy.ChargeTarget - enemyPosition;
                        desiredDirection = dive.sqrMagnitude > 0.001f ? dive.normalized : direction;
                        desiredSpeed = enemy.Speed * 3.6f;
                        if (enemy.AbilityTimer <= 0f)
                        {
                            enemy.AbilityPhase = 0;
                            enemy.AbilityTimer = Random.Range(2.2f, 4.0f);
                        }
                    }
                    else
                    {
                        desiredDirection = (direction * 0.45f + tangent * Mathf.Sin(elapsed * 2.8f + enemy.Id) * 0.85f).normalized;
                        desiredSpeed = enemy.Speed * (0.95f + Mathf.Abs(motionWave) * 0.30f);
                    }
                    break;
                case EnemyKind.Mushroom:
                    if (enemy.AbilityPhase == 0 && enemy.AbilityTimer <= 0f)
                    {
                        enemy.AbilityPhase = 3;
                        enemy.AbilityTimer = 0.30f;
                        SpawnEffect("Charge Telegraph", enemyPosition, telegraphSprite, 0.78f, new Color(0.65f, 1f, 0.78f, 0.62f), 0.32f);
                    }
                    if (enemy.AbilityPhase == 3)
                    {
                        desiredDirection = (direction + tangent * 0.35f).normalized;
                        desiredSpeed = enemy.Speed * 2.85f;
                        acceleration = 11f;
                        if (enemy.AbilityTimer <= 0f)
                        {
                            enemy.AbilityPhase = 0;
                            enemy.AbilityTimer = Random.Range(2.1f, 4.0f);
                        }
                    }
                    else
                    {
                        Vector2 wobble = tangent * Mathf.Sin(elapsed * 4.4f + enemy.MovePhase) * 0.88f;
                        desiredDirection = (direction + wobble).normalized;
                        desiredSpeed = enemy.Speed * (0.94f + Mathf.Abs(Mathf.Sin(elapsed * 2.1f + enemy.Id)) * 0.24f);
                    }
                    break;
                case EnemyKind.Witch:
                    float rangeError = distance - 3.45f;
                    float rangeCorrection = Mathf.Clamp(rangeError * 0.75f, -0.92f, 0.92f);
                    float witchStrafe = 0.62f + Mathf.Sin(enemy.MovePhase * 1.35f + enemy.Id) * 0.24f;
                    desiredDirection = (direction * rangeCorrection + tangent * witchStrafe).normalized;
                    desiredSpeed = enemy.Speed * (1.0f + Mathf.Abs(Mathf.Sin(enemy.MovePhase * 1.2f)) * 0.28f);
                    if (enemy.ShotTimer <= 0f)
                    {
                        FireEnemyProjectile(enemy, direction, false);
                        enemy.ShotTimer = Mathf.Max(1.15f, 2.25f - elapsed * 0.002f);
                    }
                    break;
                case EnemyKind.Brute:
                    if (enemy.AbilityPhase == 0 && enemy.AbilityTimer <= 0f)
                    {
                        enemy.AbilityPhase = 1;
                        enemy.AbilityTimer = 0.68f;
                        enemy.ChargeTarget = playerPosition;
                        SpawnEffect("Charge Telegraph", enemyPosition, telegraphSprite, 1.18f, new Color(1f, 0.58f, 0.45f, 0.84f), 0.68f);
                    }
                    if (enemy.AbilityPhase == 1)
                    {
                        desiredDirection = Vector2.zero;
                        desiredSpeed = 0f;
                        if (enemy.AbilityTimer <= 0f)
                        {
                            enemy.AbilityPhase = 2;
                            enemy.AbilityTimer = 0.88f;
                        }
                    }
                    else if (enemy.AbilityPhase == 2)
                    {
                        Vector2 charge = enemy.ChargeTarget - enemyPosition;
                        desiredDirection = charge.sqrMagnitude > 0.001f ? charge.normalized : direction;
                        desiredSpeed = enemy.Speed * 3.7f;
                        acceleration = 14f;
                        if (enemy.AbilityTimer <= 0f)
                        {
                            enemy.AbilityPhase = 0;
                            enemy.AbilityTimer = Random.Range(2.6f, 4.2f);
                        }
                    }
                    else
                    {
                        desiredDirection = (direction * 0.86f + tangent * (0.24f + motionWave * 0.16f)).normalized;
                        desiredSpeed = enemy.Speed * (0.86f + Mathf.Abs(motionWave) * 0.18f);
                    }
                    break;
                case EnemyKind.Boss:
                    float bossOrbit = enemy.MovePhase * 0.82f + enemy.MotionSeed;
                    Vector2 bossFigureEight = playerPosition + new Vector2(
                        Mathf.Cos(bossOrbit) * 3.0f,
                        Mathf.Sin(bossOrbit * 2f) * 1.75f);
                    Vector2 bossToTarget = bossFigureEight - enemyPosition;
                    desiredDirection = bossToTarget.sqrMagnitude > 0.001f
                        ? bossToTarget.normalized
                        : (direction * 0.30f + tangent * 0.76f).normalized;
                    desiredSpeed = enemy.Speed * (1.0f + Mathf.Sin(enemy.MovePhase * 1.7f) * 0.24f);
                    acceleration = 6.5f;
                    if (enemy.ShotTimer <= 0f)
                    {
                        SpawnBossPattern(enemy);
                        enemy.ShotTimer = Mathf.Max(1.35f, 2.65f - elapsed * 0.003f);
                    }
                    break;
            }

            if (desiredDirection.sqrMagnitude > 1f) desiredDirection.Normalize();
            enemy.Velocity = Vector2.MoveTowards(enemy.Velocity, desiredDirection * desiredSpeed, acceleration * dt);
            enemy.Object.transform.position += new Vector3(enemy.Velocity.x, enemy.Velocity.y, 0f) * dt;
            Vector3 clampedPosition = enemy.Object.transform.position;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, ArenaLeft - 1.1f, ArenaRight + 1.1f);
            clampedPosition.y = Mathf.Clamp(clampedPosition.y, ArenaBottom - 1.1f, ArenaTop + 1.1f);
            enemy.Object.transform.position = clampedPosition;
            SyncShadow(enemy.Object, enemy.Shadow, new Vector2(0.08f, -0.16f));
            if (enemy.Velocity.sqrMagnitude > 0.02f)
            {
                float velocityAngle = Mathf.Atan2(enemy.Velocity.y, enemy.Velocity.x) * Mathf.Rad2Deg - 90f;
                float facingWeight = enemy.Kind == EnemyKind.Witch ? 0.22f : enemy.Kind == EnemyKind.Boss ? 0.38f : 0.64f;
                float targetRotation = Mathf.LerpAngle(0f, velocityAngle, facingWeight);
                targetRotation += Mathf.Sin(enemy.MovePhase * 2.1f + enemy.MotionSeed) * (2.5f + Mathf.Abs(motionWave) * 3.5f);
                float turnSpeed = enemy.Kind == EnemyKind.Moth ? 520f : enemy.Kind == EnemyKind.Boss ? 260f : 380f;
                float currentRotation = enemy.Object.transform.eulerAngles.z;
                float nextRotation = Mathf.MoveTowardsAngle(currentRotation, targetRotation, turnSpeed * dt);
                enemy.Object.transform.rotation = Quaternion.Euler(0f, 0f, nextRotation);
            }
            enemy.RingCooldown = Mathf.Max(0f, enemy.RingCooldown - dt);

            if (contactCooldown <= 0f && Vector2.Distance(player.transform.position, enemy.Object.transform.position) < enemy.Radius + 0.38f)
            {
                playerHealth -= ContactDamage(enemy.Kind) * Mathf.Max(0.35f, 1f - armorLevel * 0.12f);
                contactCooldown = 1.15f;
                // A hit interrupts the step but never teleports the player.
                // Push the enemy away instead, so the player can recover in place.
                playerVelocity = Vector2.zero;
                SpawnEffect("Hit Spark", playerPosition, hitEffectSprite, enemy.Kind == EnemyKind.Boss ? 0.92f : 0.48f, new Color(1f, 0.70f, 0.82f, 0.94f), 0.34f);
                Vector2 enemyAway = (Vector2)enemy.Object.transform.position - (Vector2)player.transform.position;
                if (enemyAway.sqrMagnitude < 0.001f) enemyAway = Vector2.up;
                enemyAway.Normalize();
                enemy.Object.transform.position = (Vector2)player.transform.position + enemyAway * (enemy.Radius + 0.55f);
                enemy.Velocity = enemyAway * Mathf.Max(enemy.Speed * 0.9f, enemy.Kind == EnemyKind.Boss ? 2.8f : 2.2f);
                if (enemy.AbilityPhase == 2) enemy.AbilityPhase = 0;
                if (playerHealth <= 0f)
                {
                    Finish(GameMode.Lost);
                    return;
                }
            }
        }
    }

    private float ContactDamage(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Brute: return 14f;
            case EnemyKind.Boss: return 20f;
            case EnemyKind.Witch: return 6f;
            case EnemyKind.Moth: return 7f;
            case EnemyKind.Mushroom: return 6f;
            case EnemyKind.Wool: return 6f;
            default: return 4f;
        }
    }

    private void FireWeapon(float dt)
    {
        weaponTimer -= dt;
        if (weaponTimer <= 0f)
        {
            weaponTimer = weaponCooldown;
            Enemy nearest = FindNearestEnemy();
            if (nearest != null)
            {
                Vector2 direction = ((Vector2)nearest.Object.transform.position - (Vector2)player.transform.position).normalized;
                if (direction.sqrMagnitude < 0.001f) direction = lastAim;
                lastAim = direction;

                int count = projectileCount + (cinderVolley ? 1 : 0);
                float spread = count <= 1 ? 0f : 18f;
                for (int i = 0; i < count; i++)
                {
                    float offset = count == 1 ? 0f : Mathf.Lerp(-spread, spread, i / (float)(count - 1));
                    Vector2 shotDirection = Quaternion.Euler(0f, 0f, offset) * direction;
                    ProjectileKind kind = cinderVolley ? ProjectileKind.Cinder : ProjectileKind.Spark;
                    SpawnPlayerProjectile(
                        kind,
                        cinderVolley ? "Cinder Bolt" : "Spark Bolt",
                        cinderVolley ? emberBoltSprite : boltSprite,
                        shotDirection,
                        projectileSpeed,
                        weaponDamage,
                        2.4f,
                        projectilePierce,
                        0.42f,
                        false);
                }
            }
        }

        hearthNotesTimer -= dt;
        if (hasHearthNotes && hearthNotesTimer <= 0f)
        {
            int count = 3 + Mathf.Min(hearthNotesLevel, 3);
            float damage = 8f + hearthNotesLevel * 4f;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f + elapsed * 0.7f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnPlayerProjectile(ProjectileKind.HearthNote, "Hearth Note", noteBoltSprite, direction, 4.5f, damage, 1.55f, 1, 0.30f, false);
            }
            hearthNotesTimer = Mathf.Max(1.45f, 3.35f - hearthNotesLevel * 0.28f);
        }

        berryBasketTimer -= dt;
        if (hasBerryBasket && berryBasketTimer <= 0f)
        {
            Enemy nearest = FindNearestEnemy();
            if (nearest != null)
            {
                Vector2 direction = ((Vector2)nearest.Object.transform.position - (Vector2)player.transform.position).normalized;
                SpawnPlayerProjectile(ProjectileKind.Berry, "Berry Toss", berryBoltSprite, direction, 4.6f, 20f + berryBasketLevel * 9f, 2.9f, 1, 0.46f, true);
            }
            berryBasketTimer = Mathf.Max(1.65f, 4.0f - berryBasketLevel * 0.35f);
        }

        sewingNeedleTimer -= dt;
        if (hasSewingNeedle && sewingNeedleTimer <= 0f)
        {
            Enemy nearest = FindNearestEnemy();
            if (nearest != null)
            {
                Vector2 direction = ((Vector2)nearest.Object.transform.position - (Vector2)player.transform.position).normalized;
                SpawnPlayerProjectile(ProjectileKind.Needle, "Sewing Needle", needleSprite, direction, 11.5f, 34f + sewingNeedleLevel * 12f, 1.65f, 3 + sewingNeedleLevel, 0.27f, false);
            }
            sewingNeedleTimer = Mathf.Max(1.25f, 2.7f - sewingNeedleLevel * 0.22f);
        }
    }

    private void SpawnPlayerProjectile(ProjectileKind kind, string objectName, Sprite sprite, Vector2 direction, float speed, float damage, float life, int pierce, float scale, bool homing)
    {
        if (direction.sqrMagnitude < 0.001f) direction = lastAim;
        direction.Normalize();
        GameObject projectileObject = CreateSpriteObject(objectName, sprite, player.transform.position, scale, 16);
        projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        projectiles.Add(new Projectile
        {
            Object = projectileObject,
            Velocity = direction * speed,
            Damage = damage,
            Life = life,
            Pierce = pierce,
            Kind = kind,
            HitRadius = kind == ProjectileKind.Needle ? 0.13f : kind == ProjectileKind.Berry ? 0.25f : 0.20f,
            Homing = homing
        });
    }

    private Enemy FindNearestEnemy()
    {
        Enemy nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Health <= 0f || enemy.Object == null) continue;
            float distance = Vector2.Distance(player.transform.position, enemy.Object.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
        }
        return nearest;
    }

    private void FireEnemyProjectile(Enemy enemy, Vector2 direction, bool boss)
    {
        if (enemy == null || enemy.Object == null) return;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;
        direction.Normalize();
        ProjectileKind kind = boss ? ProjectileKind.BossOrb : ProjectileKind.CurseSeed;
        string objectName = boss ? "Boss Orb" : "Curse Seed";
        Sprite sprite = boss ? bossOrbSprite : curseSeedSprite;
        float speed = boss ? 2.9f : 3.35f;
        float damage = boss ? 13f : 9f;
        GameObject projectileObject = CreateSpriteObject(objectName, sprite, enemy.Object.transform.position, boss ? 0.48f : 0.32f, 17);
        projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        enemyProjectiles.Add(new EnemyProjectile
        {
            Object = projectileObject,
            Velocity = direction * speed,
            Damage = damage,
            Life = boss ? 4.4f : 3.8f,
            Radius = boss ? 0.30f : 0.20f
        });
    }

    private void SpawnBossPattern(Enemy boss)
    {
        if (boss == null || boss.Object == null) return;
        Vector2 bossPosition = boss.Object.transform.position;
        Vector2 toPlayer = ((Vector2)player.transform.position - bossPosition).normalized;
        int count = 8;
        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f + elapsed * 0.42f;
            FireEnemyProjectile(boss, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), true);
        }
        FireEnemyProjectile(boss, toPlayer, true);
        SpawnEffect("Boss Burst", bossPosition, bossBurstSprite, 1.25f, new Color(1f, 0.48f, 0.68f, 0.84f), 0.38f);
    }

    private void UpdateEnemyProjectiles(float dt)
    {
        for (int i = enemyProjectiles.Count - 1; i >= 0; i--)
        {
            EnemyProjectile projectile = enemyProjectiles[i];
            if (projectile.Object == null || (projectile.Life -= dt) <= 0f)
            {
                RemoveEnemyProjectileAt(i);
                continue;
            }
            projectile.Object.transform.position += new Vector3(projectile.Velocity.x, projectile.Velocity.y, 0f) * dt;
            if (contactCooldown <= 0f && Vector2.Distance(player.transform.position, projectile.Object.transform.position) < projectile.Radius + 0.28f)
            {
                playerHealth -= projectile.Damage * Mathf.Max(0.35f, 1f - armorLevel * 0.12f);
                contactCooldown = 0.9f;
                playerVelocity = Vector2.zero;
                SpawnEffect("Hit Spark", player.transform.position, hitEffectSprite, 0.44f, new Color(0.82f, 0.52f, 1f, 0.92f), 0.34f);
                RemoveEnemyProjectileAt(i);
                if (playerHealth <= 0f)
                {
                    Finish(GameMode.Lost);
                    return;
                }
            }
        }
    }

    private void UpdateProjectiles(float dt)
    {
        defeatedBuffer.Clear();
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            Projectile projectile = projectiles[i];
            projectile.Life -= dt;
            if (projectile.Life <= 0f || projectile.Object == null)
            {
                RemoveProjectileAt(i);
                continue;
            }

            if (projectile.Homing)
            {
                Enemy target = FindNearestEnemy();
                if (target != null && target.Object != null)
                {
                    Vector2 toTarget = ((Vector2)target.Object.transform.position - (Vector2)projectile.Object.transform.position).normalized;
                    float speed = projectile.Velocity.magnitude;
                    projectile.Velocity = Vector2.Lerp(projectile.Velocity.normalized, toTarget, Mathf.Clamp01(dt * 3.4f)) * speed;
                    projectile.Object.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(projectile.Velocity.y, projectile.Velocity.x) * Mathf.Rad2Deg - 90f);
                }
            }
            projectile.Object.transform.position += new Vector3(projectile.Velocity.x, projectile.Velocity.y, 0f) * dt;
            bool consumed = false;
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Health <= 0f || enemy.Object == null || projectile.HitIds.Contains(enemy.Id)) continue;
                if (Vector2.Distance(projectile.Object.transform.position, enemy.Object.transform.position) > enemy.Radius + projectile.HitRadius) continue;

                projectile.HitIds.Add(enemy.Id);
                projectile.Pierce -= 1;
                enemy.Health -= projectile.Damage;
                SpawnEffect("Hit Spark", enemy.Object.transform.position, hitEffectSprite, enemy.Kind == EnemyKind.Boss ? 0.60f : 0.30f, projectile.Kind == ProjectileKind.Needle ? new Color(1f, 0.92f, 0.70f, 0.96f) : new Color(1f, 0.72f, 0.86f, 0.90f), 0.28f);
                if (enemy.Health <= 0f && !defeatedBuffer.Contains(enemy)) defeatedBuffer.Add(enemy);
                if (projectile.Pierce <= 0)
                {
                    consumed = true;
                    break;
                }
            }

            if (consumed) RemoveProjectileAt(i);
        }

        foreach (Enemy defeated in defeatedBuffer)
        {
            if (enemies.Contains(defeated)) DefeatEnemy(defeated);
        }
    }

    private void DefeatEnemy(Enemy enemy)
    {
        Vector2 position = enemy.Object == null ? Vector2.zero : enemy.Object.transform.position;
        if (enemy.Object != null) Destroy(enemy.Object);
        if (enemy.Shadow != null) Destroy(enemy.Shadow);
        enemies.Remove(enemy);
        if (enemy.Kind == EnemyKind.Boss)
        {
            bossActive = false;
            bossHealth = 0f;
            score += 1500;
            kills += 5;
            SpawnEffect("Boss Burst", position, bossBurstSprite, 2.8f, new Color(1f, 0.70f, 0.84f, 1f), 0.9f);
            for (int i = 0; i < 6; i++)
            {
                Vector2 drop = position + Random.insideUnitCircle * 0.75f;
                SpawnGem(drop, 5);
            }
            SpawnChest(position);
            toastMessage = "MALLOW WARDEN FELLED  //  RELAY SURGE +1500";
            toastTimer = 4.0f;
            return;
        }

        int value = enemy.Kind == EnemyKind.Brute || enemy.Kind == EnemyKind.Witch ? 4 : enemy.Kind == EnemyKind.Moth ? 2 : 1;
        SpawnGem(position, value);
        kills++;
        score += enemy.Kind == EnemyKind.Brute ? 30 : enemy.Kind == EnemyKind.Witch ? 24 : enemy.Kind == EnemyKind.Moth ? 18 : 10;
        float chestChance = 0.025f + luckLevel * 0.018f;
        if (enemy.Kind == EnemyKind.Brute || enemy.Kind == EnemyKind.Witch || Random.value < chestChance) SpawnChest(position);
    }

    private void SpawnGem(Vector2 position, int value)
    {
        GameObject gemObject = CreateSpriteObject("Signal Shard", gemSprite, position, value > 1 ? 0.72f : 0.52f, 7);
        GameObject gemShadow = CreateShadow("Shard Ground Shadow", position + new Vector2(0.05f, -0.10f), value > 1 ? 0.72f : 0.52f, 6);
        gems.Add(new Gem { Object = gemObject, Shadow = gemShadow, Value = value });
    }

    private void UpdateGems(float dt)
    {
        for (int i = gems.Count - 1; i >= 0; i--)
        {
            Gem gem = gems[i];
            if (gem.Object == null)
            {
                gems.RemoveAt(i);
                continue;
            }

            float distance = Vector2.Distance(player.transform.position, gem.Object.transform.position);
            if (distance < magnetRange + 1.0f)
            {
                Vector2 direction = ((Vector2)player.transform.position - (Vector2)gem.Object.transform.position).normalized;
                gem.Object.transform.position += new Vector3(direction.x, direction.y, 0f) * (5.8f + magnetRange) * dt;
            }
            SyncShadow(gem.Object, gem.Shadow, new Vector2(0.05f, -0.10f));

            if (distance < 0.38f)
            {
                AddXp(gem.Value);
                score += gem.Value;
                Destroy(gem.Object);
                if (gem.Shadow != null) Destroy(gem.Shadow);
                gems.RemoveAt(i);
            }
        }
    }

    private void AddXp(int amount)
    {
        xp += amount;
        if (mode == GameMode.Playing) CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        if (xp < xpToNext) return;
        xp -= xpToNext;
        level++;
        xpToNext = Mathf.RoundToInt(xpToNext * 1.22f) + 3;
        BuildUpgradeChoices();
        mode = GameMode.LevelUp;
    }

    private void BuildUpgradeChoices()
    {
        upgradeChoices.Clear();
        List<UpgradeChoice> pool = new List<UpgradeChoice>
        {
            new UpgradeChoice(UpgradeType.WandDamage, "WEAPON", "THICKER ARC", "+4 Spark Wand damage. Wand level rises."),
            new UpgradeChoice(UpgradeType.WandCount, "WEAPON", "SPLIT ARC", "+1 automatic bolt per volley."),
            new UpgradeChoice(UpgradeType.WandCooldown, "WEAPON", "QUICK HAND", "Attack cooldown reduced by 10%."),
            new UpgradeChoice(UpgradeType.Magnet, "PASSIVE", "GRAVITY THREAD", "+0.8 pickup radius for signal shards."),
            new UpgradeChoice(UpgradeType.Vitality, "PASSIVE", "SECOND WIND", "+25 max health and restore 25 health."),
            new UpgradeChoice(UpgradeType.Haste, "PASSIVE", "LIGHT FEET", "+0.55 movement speed."),
            new UpgradeChoice(UpgradeType.Armor, "PASSIVE", "SOFT ARMOR", "Reduce contact and projectile damage by 12%."),
            new UpgradeChoice(UpgradeType.Recovery, "PASSIVE", "RECOVERY TEA", "Regenerate 1.4 hearts every second."),
            new UpgradeChoice(UpgradeType.Luck, "PASSIVE", "LUCKY THREAD", "More elite enemies drop story chests."),
            new UpgradeChoice(UpgradeType.Area, "PASSIVE", "WIDE COMFORT", "Pulse and orbiting weapon radius +18%.")
        };

        pool.Add(new UpgradeChoice(
            UpgradeType.HearthNotes,
            "WEAPON",
            hasHearthNotes ? "HEARTH NOTES +" + (hearthNotesLevel + 1) : "HEARTH NOTES",
            hasHearthNotes ? "Notes fire faster and gain another warm projectile." : "Every few seconds, send a radial ring of warm notes."));
        pool.Add(new UpgradeChoice(
            UpgradeType.BerryBasket,
            "WEAPON",
            hasBerryBasket ? "BERRY BASKET +" + (berryBasketLevel + 1) : "BERRY BASKET",
            hasBerryBasket ? "Homing berry damage and cadence improve." : "Launch a homing berry at the nearest foe."));
        pool.Add(new UpgradeChoice(
            UpgradeType.SewingNeedle,
            "WEAPON",
            hasSewingNeedle ? "NEEDLE KIT +" + (sewingNeedleLevel + 1) : "SEWING NEEDLE",
            hasSewingNeedle ? "Fast piercing needle gains damage and pierce." : "Fire a fast piercing needle through the swarm."));
        pool.Add(new UpgradeChoice(
            UpgradeType.FireflyJar,
            "WEAPON",
            hasFireflyJar ? "FIREFLY JAR +" + (fireflyJarLevel + 1) : "FIREFLY JAR",
            hasFireflyJar ? "Add another orbiting firefly and increase its damage." : "Summon orbiting fireflies that hurt nearby foes."));

        if (!hasEmberRing)
        {
            UpgradeChoice ring = new UpgradeChoice(UpgradeType.EmberRing, "PASSIVE", "EMBER RING", "Two orbiting sparks damage enemies nearby.");
            upgradeChoices.Add(ring);
            pool.RemoveAll(choice => choice.Type == UpgradeType.EmberRing);
        }

        if (hasEmberRing && wandLevel >= 3 && !cinderVolley)
        {
            UpgradeChoice evolution = new UpgradeChoice(UpgradeType.CinderVolley, "EVOLUTION", "CINDER VOLLEY", "Spark Wand + Ember Ring evolve into a piercing spread.");
            upgradeChoices.Add(evolution);
        }

        while (upgradeChoices.Count < 3 && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            upgradeChoices.Add(pool[index]);
            pool.RemoveAt(index);
        }

        while (upgradeChoices.Count < 3)
        {
            upgradeChoices.Add(new UpgradeChoice(UpgradeType.WandDamage, "WEAPON", "THICKER ARC", "+4 Spark Wand damage."));
        }
    }

    private void ApplyUpgrade(int index)
    {
        if (mode != GameMode.LevelUp || index < 0 || index >= upgradeChoices.Count) return;
        UpgradeChoice choice = upgradeChoices[index];
        switch (choice.Type)
        {
            case UpgradeType.WandDamage:
                weaponDamage += 4f;
                wandLevel++;
                break;
            case UpgradeType.WandCount:
                projectileCount++;
                wandLevel++;
                break;
            case UpgradeType.WandCooldown:
                weaponCooldown = Mathf.Max(0.22f, weaponCooldown * 0.90f);
                wandLevel++;
                break;
            case UpgradeType.EmberRing:
                hasEmberRing = true;
                emberDamage += 2f;
                BuildRingObjects();
                break;
            case UpgradeType.Magnet:
                magnetRange += 0.8f;
                break;
            case UpgradeType.Vitality:
                maxHealth += 25;
                playerHealth = Mathf.Min(maxHealth, playerHealth + 25f);
                break;
            case UpgradeType.Haste:
                moveSpeed += 0.55f;
                break;
            case UpgradeType.CinderVolley:
                cinderVolley = true;
                weaponDamage += 10f;
                projectileCount++;
                projectilePierce++;
                weaponCooldown = Mathf.Max(0.18f, weaponCooldown * 0.75f);
                BuildRingObjects();
                break;
            case UpgradeType.HearthNotes:
                hasHearthNotes = true;
                hearthNotesLevel++;
                break;
            case UpgradeType.BerryBasket:
                hasBerryBasket = true;
                berryBasketLevel++;
                break;
            case UpgradeType.SewingNeedle:
                hasSewingNeedle = true;
                sewingNeedleLevel++;
                break;
            case UpgradeType.FireflyJar:
                hasFireflyJar = true;
                fireflyJarLevel++;
                BuildFireflyObjects();
                break;
            case UpgradeType.Armor:
                armorLevel++;
                break;
            case UpgradeType.Recovery:
                recoveryRate += 1.4f;
                break;
            case UpgradeType.Luck:
                luckLevel++;
                break;
            case UpgradeType.Area:
                areaMultiplier += 0.18f;
                break;
        }

        toastMessage = choice.Title + " INSTALLED";
        toastTimer = 2.4f;
        upgradeChoices.Clear();
        mode = GameMode.Playing;
        CheckForLevelUp();
    }

    private void BuildRingObjects()
    {
        ClearRingObjects();
        if (!hasEmberRing) return;
        int count = cinderVolley ? 3 : 2;
        emberRingObjects = new GameObject[count];
        for (int i = 0; i < count; i++) emberRingObjects[i] = CreateSpriteObject("Ember Ring", ringSprite, player.transform.position, 0.58f, 15);
    }

    private void UpdateEmberRing(float dt)
    {
        if (!hasEmberRing || emberRingObjects == null) return;
        List<Enemy> ringDefeated = new List<Enemy>();
        float radius = (cinderVolley ? 1.45f : 1.22f) * areaMultiplier;
        for (int i = 0; i < emberRingObjects.Length; i++)
        {
            float angle = elapsed * 1.7f + i / (float)emberRingObjects.Length * Mathf.PI * 2f;
            emberRingObjects[i].transform.position = player.transform.position + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Health <= 0f || enemy.Object == null || enemy.RingCooldown > 0f) continue;
                if (Vector2.Distance(emberRingObjects[i].transform.position, enemy.Object.transform.position) > enemy.Radius + 0.22f) continue;
                enemy.Health -= emberDamage;
                enemy.RingCooldown = 0.55f;
                if (enemy.Health <= 0f && !ringDefeated.Contains(enemy)) ringDefeated.Add(enemy);
            }
        }
        foreach (Enemy defeated in ringDefeated) if (enemies.Contains(defeated)) DefeatEnemy(defeated);
    }

    private void BuildFireflyObjects()
    {
        ClearFireflyObjects();
        if (!hasFireflyJar) return;
        int count = 2 + Mathf.Min(fireflyJarLevel, 4);
        fireflyObjects = new GameObject[count];
        for (int i = 0; i < count; i++) fireflyObjects[i] = CreateSpriteObject("Firefly Jar", ringSprite, player.transform.position, 0.42f, 15);
    }

    private void UpdateFireflyJar(float dt)
    {
        if (!hasFireflyJar || fireflyObjects == null) return;
        fireflyTimer -= dt;
        float radius = 1.75f * areaMultiplier;
        for (int i = 0; i < fireflyObjects.Length; i++)
        {
            float angle = elapsed * (1.2f + fireflyJarLevel * 0.08f) + i / (float)fireflyObjects.Length * Mathf.PI * 2f;
            fireflyObjects[i].transform.position = player.transform.position + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        if (fireflyTimer > 0f) return;
        fireflyTimer = Mathf.Max(0.30f, 0.68f - fireflyJarLevel * 0.06f);
        List<Enemy> defeated = new List<Enemy>();
        float damage = 6f + fireflyJarLevel * 3f;
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Health <= 0f || enemy.Object == null) continue;
            if (Vector2.Distance(player.transform.position, enemy.Object.transform.position) > radius + enemy.Radius * 0.42f) continue;
            enemy.Health -= damage;
            SpawnEffect("Hit Spark", enemy.Object.transform.position, hitEffectSprite, 0.24f, new Color(0.70f, 1f, 0.88f, 0.84f), 0.24f);
            if (enemy.Health <= 0f && !defeated.Contains(enemy)) defeated.Add(enemy);
        }
        foreach (Enemy enemy in defeated) if (enemies.Contains(enemy)) DefeatEnemy(enemy);
    }

    private void SpawnChest(Vector2 suggestedPosition)
    {
        Vector2 position = suggestedPosition;
        if (position == Vector2.zero || Vector2.Distance(position, player.transform.position) < 2.7f)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(4.5f, 8.5f);
            position = (Vector2)player.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            position.x = Mathf.Clamp(position.x, ArenaLeft + 1f, ArenaRight - 1f);
            position.y = Mathf.Clamp(position.y, ArenaBottom + 1f, ArenaTop - 1f);
        }
        GameObject chestObject = CreateSpriteObject("Night Chest", chestSprite, position, 0.82f, 8);
        GameObject chestShadow = CreateShadow("Chest Ground Shadow", position + new Vector2(0.08f, -0.16f), 0.82f, 7);
        chests.Add(new Chest { Object = chestObject, Shadow = chestShadow, Opened = false });
    }

    private void UpdateChests(float dt)
    {
        for (int i = chests.Count - 1; i >= 0; i--)
        {
            Chest chest = chests[i];
            if (chest.Object == null)
            {
                chests.RemoveAt(i);
                continue;
            }
            if (!chest.Opened && Vector2.Distance(player.transform.position, chest.Object.transform.position) < 0.76f)
            {
                chest.Opened = true;
                chestsOpened++;
                score += 250;
                AddXp(7);
                toastMessage = "CHEST OPENED  +250 SCORE  +7 XP";
                toastTimer = 3.2f;
                chest.Object.transform.localScale = Vector3.one * 0.58f;
                chest.Object.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.42f);
            }
        }
    }

    private void EmitPulse()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || pulseCooldown > 0f || pulseEnergy < 30f) return;
        pulseEnergy -= 30f;
        pulseCooldown = 1.25f;
        pulseTimer = 0.42f;
        pulseRadius = 0.35f;
        SpawnEffect("Boss Burst", player.transform.position, bossBurstSprite, 0.88f * areaMultiplier, new Color(0.72f, 0.92f, 1f, 0.82f), 0.42f);
        defeatedBuffer.Clear();
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Object != null && Vector2.Distance(player.transform.position, enemy.Object.transform.position) <= 2.55f * areaMultiplier)
            {
                enemy.Health -= 24f * areaMultiplier;
                if (enemy.Health <= 0f && !defeatedBuffer.Contains(enemy)) defeatedBuffer.Add(enemy);
            }
        }
        foreach (Enemy defeated in defeatedBuffer) if (enemies.Contains(defeated)) DefeatEnemy(defeated);
    }

    private void UpdatePulse(float dt)
    {
        EmitPulse();
        if (pulseTimer <= 0f)
        {
            pulseLine.gameObject.SetActive(false);
            return;
        }
        pulseTimer -= dt;
        pulseRadius += 8.6f * areaMultiplier * dt;
        pulseLine.gameObject.SetActive(true);
        pulseLine.transform.position = player.transform.position;
        for (int i = 0; i < pulseLine.positionCount; i++)
        {
            float angle = i / (float)pulseLine.positionCount * Mathf.PI * 2f;
            pulseLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * pulseRadius, Mathf.Sin(angle) * pulseRadius, 0f));
        }
        Color color = new Color(Cyan.r, Cyan.g, Cyan.b, Mathf.Clamp01(pulseTimer / 0.42f));
        pulseLine.startColor = color;
        pulseLine.endColor = color;
    }

    private void SpawnEffect(string objectName, Vector2 position, Sprite sprite, float scale, Color color, float life)
    {
        if (effects.Count >= 120 || sprite == null) return;
        GameObject effectObject = CreateSpriteObject(objectName, sprite, position, scale, 24);
        SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.color = color;
        effects.Add(new Effect
        {
            Object = effectObject,
            Color = color,
            BaseScale = effectObject.transform.localScale,
            Life = life,
            MaxLife = life
        });
    }

    private void UpdateEffects(float dt)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            Effect effect = effects[i];
            if (effect.Object == null || (effect.Life -= dt) <= 0f)
            {
                if (effect.Object != null) Destroy(effect.Object);
                effects.RemoveAt(i);
                continue;
            }
            float normalizedLife = Mathf.Clamp01(effect.Life / Mathf.Max(0.01f, effect.MaxLife));
            effect.Object.transform.localScale = effect.BaseScale * (1f + (1f - normalizedLife) * 0.75f);
            SpriteRenderer renderer = effect.Object.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = effect.Color;
                color.a *= normalizedLife;
                renderer.color = color;
            }
        }
    }

    private void SyncBossTelemetry()
    {
        Enemy boss = null;
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Kind == EnemyKind.Boss)
            {
                boss = enemy;
                break;
            }
        }
        bossActive = boss != null && boss.Health > 0f;
        if (boss != null)
        {
            bossHealth = Mathf.Max(0f, boss.Health);
            bossMaxHealth = Mathf.Max(1f, bossMaxHealth);
        }
    }

    private void AnimateDecorativeState()
    {
        if (player == null) return;
        player.transform.position = new Vector3(spawnPoint.x, spawnPoint.y + Mathf.Sin(Time.time * 2f) * 0.08f, 0f);
        SyncShadow(player, playerShadow, new Vector2(0.08f, -0.16f));
    }

    private void Finish(GameMode result)
    {
        mode = result;
        playerVelocity = Vector2.zero;
        pulseLine.gameObject.SetActive(false);
        player.SetActive(true);
    }

    private void RemoveProjectileAt(int index)
    {
        if (projectiles[index].Object != null) Destroy(projectiles[index].Object);
        projectiles.RemoveAt(index);
    }

    private void RemoveEnemyProjectileAt(int index)
    {
        if (enemyProjectiles[index].Object != null) Destroy(enemyProjectiles[index].Object);
        enemyProjectiles.RemoveAt(index);
    }

    private void ClearHazards()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Object != null) Destroy(enemy.Object);
            if (enemy.Shadow != null) Destroy(enemy.Shadow);
        }
        enemies.Clear();
    }

    private void ClearProjectiles()
    {
        foreach (Projectile projectile in projectiles) if (projectile.Object != null) Destroy(projectile.Object);
        projectiles.Clear();
    }

    private void ClearEnemyProjectiles()
    {
        foreach (EnemyProjectile projectile in enemyProjectiles) if (projectile.Object != null) Destroy(projectile.Object);
        enemyProjectiles.Clear();
    }

    private void ClearEffects()
    {
        foreach (Effect effect in effects) if (effect.Object != null) Destroy(effect.Object);
        effects.Clear();
    }

    private void ClearGems()
    {
        foreach (Gem gem in gems)
        {
            if (gem.Object != null) Destroy(gem.Object);
            if (gem.Shadow != null) Destroy(gem.Shadow);
        }
        gems.Clear();
    }

    private void ClearChests()
    {
        foreach (Chest chest in chests)
        {
            if (chest.Object != null) Destroy(chest.Object);
            if (chest.Shadow != null) Destroy(chest.Shadow);
        }
        chests.Clear();
    }

    private void ClearRingObjects()
    {
        if (emberRingObjects == null) return;
        foreach (GameObject ringObject in emberRingObjects) if (ringObject != null) Destroy(ringObject);
        emberRingObjects = null;
    }

    private void ClearFireflyObjects()
    {
        if (fireflyObjects == null) return;
        foreach (GameObject firefly in fireflyObjects) if (firefly != null) Destroy(firefly);
        fireflyObjects = null;
    }

    private GameObject CreateSpriteObject(string objectName, Sprite sprite, Vector2 position, float scale, int sortingOrder)
    {
        GameObject objectInstance = new GameObject(objectName);
        SpriteRenderer renderer = objectInstance.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        objectInstance.transform.position = new Vector3(position.x, position.y, 0f);
        objectInstance.transform.localScale = Vector3.one * scale;
        return objectInstance;
    }

    private GameObject CreateShadow(string objectName, Vector2 position, float scale, int sortingOrder)
    {
        GameObject shadow = CreateSpriteObject(objectName, shadowSprite, position, scale, sortingOrder);
        SpriteRenderer renderer = shadow.GetComponent<SpriteRenderer>();
        renderer.color = new Color(0.005f, 0.012f, 0.018f, 0.62f);
        shadow.transform.localScale = new Vector3(scale * 1.18f, scale * 0.58f, 1f);
        return shadow;
    }

    private void SyncShadow(GameObject actor, GameObject shadow, Vector2 offset)
    {
        if (actor == null || shadow == null) return;
        shadow.transform.position = actor.transform.position + new Vector3(offset.x, offset.y, 0.08f);
        shadow.transform.rotation = Quaternion.identity;
    }

    private GameObject CreateRect(string objectName, Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        GameObject objectInstance = new GameObject(objectName);
        SpriteRenderer renderer = objectInstance.AddComponent<SpriteRenderer>();
        renderer.sprite = whiteSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        objectInstance.transform.position = new Vector3(position.x, position.y, 0f);
        objectInstance.transform.localScale = new Vector3(size.x, size.y, 1f);
        return objectInstance;
    }

    private Sprite CreatePixelSprite(string spriteName, string[] rows, Dictionary<char, Color> palette, float pixelsPerUnit)
    {
        int width = rows[0].Length;
        int height = rows.Length;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = spriteName + " Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int row = 0; row < height; row++)
        {
            string source = rows[height - 1 - row];
            for (int column = 0; column < width; column++)
            {
                Color pixel;
                if (!palette.TryGetValue(source[column], out pixel)) pixel = Color.clear;
                texture.SetPixel(column, row, pixel);
            }
        }
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    private Texture2D CreateTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }

    private void OnGUI()
    {
        float scale = Mathf.Min(Screen.width / 960f, Screen.height / 600f);
        float offsetX = (Screen.width - 960f * scale) * 0.5f;
        float offsetY = (Screen.height - 600f * scale) * 0.5f;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

        if (hudPanelTexture != null) GUI.DrawTexture(new Rect(18f, 10f, 924f, 154f), hudPanelTexture);
        if (hudLineTexture != null) GUI.DrawTexture(new Rect(18f, 160f, 924f, 2f), hudLineTexture);

        DrawUiLabel(new Rect(30f, 18f, 470f, 32f), "PIXEL SIGNAL // NIGHTFALL", Cyan, 22, true);
        DrawUiLabel(new Rect(30f, 48f, 470f, 22f), "RELAY FIELD  /  SURVIVE THE SWARM", Muted, 11, false);
        DrawUiLabel(new Rect(720f, 20f, 210f, 28f), string.Format("TIME  {0:00}:{1:00}", Mathf.FloorToInt(elapsed / 60f), Mathf.FloorToInt(elapsed % 60f)), White, 16, true, TextAnchor.MiddleRight);

        DrawUiLabel(new Rect(30f, 78f, 260f, 22f), string.Format("LEVEL {0:00}   KILLS {1:000}   CHESTS {2}", level, kills, chestsOpened), White, 14, true);
        DrawUiBox(new Rect(30f, 103f, 410f, 12f), new Color(0.02f, 0.08f, 0.10f, 0.92f));
        DrawUiBox(new Rect(30f, 103f, 410f * Mathf.Clamp01(xp / (float)Mathf.Max(1, xpToNext)), 12f), Cyan);
        DrawUiLabel(new Rect(30f, 118f, 410f, 20f), string.Format("XP {0}/{1}", xp, xpToNext), Muted, 11);
        DrawUiBox(new Rect(30f, 142f, 220f, 10f), new Color(0.02f, 0.08f, 0.10f, 0.92f));
        DrawUiBox(new Rect(30f, 142f, 220f * Mathf.Clamp01(playerHealth / Mathf.Max(1f, maxHealth)), 10f), Magenta);
        DrawUiLabel(new Rect(260f, 136f, 200f, 24f), string.Format("LIFE {0:000}/{1:000}", Mathf.CeilToInt(playerHealth), maxHealth), Muted, 11);

        string loadout = cinderVolley ? "CINDER VOLLEY // EVOLVED" : string.Format("SPARK WAND LV.{0}", wandLevel);
        if (hasEmberRing) loadout += "  + EMBER RING";
        DrawUiLabel(new Rect(500f, 78f, 420f, 24f), loadout, White, 14, true);
        DrawUiLabel(new Rect(500f, 108f, 420f, 24f), string.Format("DAMAGE {0:00}   BOLTS {1}   PULSE {2:000}%", Mathf.RoundToInt(weaponDamage), projectileCount, Mathf.RoundToInt(pulseEnergy)), Muted, 11);
        DrawUiLabel(new Rect(500f, 136f, 420f, 24f), string.Format("SCORE {0:00000}   MAGNET {1:0.0}", score, magnetRange), Muted, 11);
        DrawUiLabel(new Rect(30f, 560f, 900f, 24f), "WASD / ARROWS MOVE     SPACE PULSE     P PAUSE     F FULLSCREEN", Muted, 11, false, TextAnchor.MiddleCenter);

        if (toastTimer > 0f) DrawUiLabel(new Rect(210f, 520f, 540f, 28f), toastMessage, Gold, 15, true, TextAnchor.MiddleCenter);
        if (mode == GameMode.LevelUp) DrawLevelUpOverlay();
        else if (mode == GameMode.Menu) DrawStateOverlay("PIXEL SIGNAL", "COLLECT SHARDS, BUILD A LOADOUT, SURVIVE THE NIGHT.", "PRESS ENTER TO DEPLOY");
        else if (mode == GameMode.Paused) DrawStateOverlay("PAUSED", "THE SWARM IS FROZEN. YOUR RUN IS SAFE.", "PRESS P TO RESUME");
        else if (mode == GameMode.Won) DrawStateOverlay("NIGHT SURVIVED", string.Format("180 SECONDS CLEARED.\nFINAL SCORE {0:00000}  //  KILLS {1:000}.", score, kills), "PRESS R TO RUN IT AGAIN");
        else if (mode == GameMode.Lost) DrawStateOverlay("RUN ENDED", string.Format("SURVIVED {0:00}:{1:00}.\nLEVEL {2:00}  //  KILLS {3:000}.", Mathf.FloorToInt(elapsed / 60f), Mathf.FloorToInt(elapsed % 60f), level, kills), "PRESS R TO REDEPLOY");

        GUI.matrix = previousMatrix;
    }

    private void DrawStateOverlay(string heading, string body, string prompt)
    {
        Rect panel = new Rect(170f, 170f, 620f, 260f);
        DrawUiBox(panel, new Color(0.012f, 0.035f, 0.055f, 0.95f));
        DrawUiLabel(new Rect(205f, 204f, 550f, 46f), heading, Cyan, 24, true, TextAnchor.MiddleCenter);
        DrawUiLabel(new Rect(205f, 270f, 550f, 60f), body, White, 15, false, TextAnchor.MiddleCenter);
        DrawUiLabel(new Rect(205f, 360f, 550f, 28f), prompt, White, 14, true, TextAnchor.MiddleCenter);
        DrawUiLabel(new Rect(205f, 398f, 550f, 22f), "AUTO-ATTACK  //  SHARDS  //  UPGRADES  //  EVOLUTION", Muted, 10, false, TextAnchor.MiddleCenter);
    }

    private void DrawLevelUpOverlay()
    {
        DrawUiBox(new Rect(75f, 118f, 810f, 410f), new Color(0.012f, 0.035f, 0.055f, 0.96f));
        DrawUiLabel(new Rect(115f, 145f, 730f, 40f), "LEVEL UP // CHOOSE ONE", Cyan, 24, true, TextAnchor.MiddleCenter);
        DrawUiLabel(new Rect(115f, 185f, 730f, 24f), "THE NIGHT PAUSES WHILE YOU BUILD YOUR LOADOUT", Muted, 11, false, TextAnchor.MiddleCenter);

        for (int i = 0; i < 3; i++)
        {
            Rect card = new Rect(105f + i * 255f, 230f, 230f, 235f);
            if (DrawUiButton(card, new Color(0.035f, 0.15f, 0.20f, 0.98f))) ApplyUpgrade(i);
            UpgradeChoice choice = upgradeChoices[i];
            DrawUiLabel(new Rect(card.x + 16f, card.y + 16f, card.width - 32f, 22f), string.Format("{0}  //  {1}", i + 1, choice.Tag), Gold, 11, true);
            DrawUiLabel(new Rect(card.x + 16f, card.y + 55f, card.width - 32f, 54f), choice.Title, White, 16, true);
            DrawUiLabel(new Rect(card.x + 16f, card.y + 120f, card.width - 32f, 86f), choice.Description, Muted, 12);
        }
        DrawUiLabel(new Rect(115f, 482f, 730f, 24f), "CLICK A CARD OR PRESS 1 / 2 / 3", White, 14, true, TextAnchor.MiddleCenter);
    }

    private void DrawUiLabel(Rect rect, string text, Color color, int fontSize = 14, bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        Color previousColor = GUI.color;
        GUIStyle label = GUI.skin.label;
        int previousFontSize = label.fontSize;
        FontStyle previousFontStyle = label.fontStyle;
        TextAnchor previousAnchor = label.alignment;
        bool previousWordWrap = label.wordWrap;
        GUI.color = color;
        label.fontSize = fontSize;
        label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        label.alignment = anchor;
        label.wordWrap = true;
        GUI.Label(rect, text);
        label.fontSize = previousFontSize;
        label.fontStyle = previousFontStyle;
        label.alignment = previousAnchor;
        label.wordWrap = previousWordWrap;
        GUI.color = previousColor;
    }

    private void DrawUiBox(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;
    }

    private bool DrawUiButton(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        bool clicked = GUI.Button(rect, GUIContent.none);
        GUI.color = previousColor;
        return clicked;
    }
}
