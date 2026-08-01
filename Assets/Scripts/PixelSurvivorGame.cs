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
        Brute
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
        CinderVolley
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
    }

    private sealed class Projectile
    {
        public GameObject Object;
        public Vector2 Velocity;
        public float Damage;
        public float Life;
        public int Pierce;
        public readonly HashSet<int> HitIds = new HashSet<int>();
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
    private const float ArenaLeft = -7.35f;
    private const float ArenaRight = 7.35f;
    private const float ArenaBottom = -4.05f;
    private const float ArenaTop = 4.05f;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<Projectile> projectiles = new List<Projectile>();
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
        maxHealth = 120;
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
        ClearGems();
        ClearChests();
        ClearRingObjects();
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

        MovePlayer(dt);
        UpdateSpawnPressure(dt);
        MoveEnemies(dt);
        FireWeapon(dt);
        UpdateProjectiles(dt);
        UpdateGems(dt);
        UpdateChests(dt);
        UpdateEmberRing(dt);
        UpdatePulse(dt);

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
        spawnTimer -= dt;
        if (spawnTimer <= 0f && enemies.Count < 86)
        {
            SpawnEnemy(Vector2.zero);
            if (elapsed > 72f && Random.value < 0.30f && enemies.Count < 86) SpawnEnemy(Vector2.zero);
            spawnTimer = Mathf.Max(0.22f, 0.78f - elapsed * 0.0027f);
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
            int edge = Random.Range(0, 4);
            if (edge == 0) position = new Vector2(ArenaLeft - 0.9f, Random.Range(ArenaBottom, ArenaTop));
            else if (edge == 1) position = new Vector2(ArenaRight + 0.9f, Random.Range(ArenaBottom, ArenaTop));
            else if (edge == 2) position = new Vector2(Random.Range(ArenaLeft, ArenaRight), ArenaTop + 0.9f);
            else position = new Vector2(Random.Range(ArenaLeft, ArenaRight), ArenaBottom - 0.9f);
        }

        bool brute = elapsed > 38f && Random.value < Mathf.Min(0.22f, elapsed / 700f);
        float healthScale = 1f + elapsed * 0.005f;
        Enemy enemy = new Enemy
        {
            Id = nextEnemyId++,
            Kind = brute ? EnemyKind.Brute : EnemyKind.Drone,
            Radius = brute ? 0.65f : 0.46f,
            Speed = brute ? 0.75f + elapsed * 0.001f : 1.15f + elapsed * 0.002f,
            Health = (brute ? 55f : 18f) * healthScale,
            Velocity = Vector2.zero
        };
        Sprite sprite = brute ? bruteSprite : droneSprite;
        float scale = brute ? 0.98f : 0.78f;
        enemy.Object = CreateSpriteObject(brute ? "Brute Drone" : "Red Drone", sprite, position, scale, 9);
        enemy.Shadow = CreateShadow("Drone Ground Shadow", position + new Vector2(0.08f, -0.16f), scale, 8);
        enemies.Add(enemy);
    }

    private void MoveEnemies(float dt)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy.Health <= 0f || enemy.Object == null) continue;
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)enemy.Object.transform.position;
            Vector2 direction = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.zero;
            enemy.Velocity = Vector2.MoveTowards(enemy.Velocity, direction * enemy.Speed, 5.5f * dt);
            enemy.Object.transform.position += new Vector3(enemy.Velocity.x, enemy.Velocity.y, 0f) * dt;
            SyncShadow(enemy.Object, enemy.Shadow, new Vector2(0.08f, -0.16f));
            enemy.Object.transform.Rotate(0f, 0f, (enemy.Kind == EnemyKind.Brute ? -28f : 64f) * dt);
            enemy.RingCooldown = Mathf.Max(0f, enemy.RingCooldown - dt);

            if (contactCooldown <= 0f && Vector2.Distance(player.transform.position, enemy.Object.transform.position) < enemy.Radius + 0.38f)
            {
                playerHealth -= enemy.Kind == EnemyKind.Brute ? 12f : 6f;
                contactCooldown = 1.15f;
                // A hit interrupts the step but never teleports the player.
                // Push the enemy away instead, so the player can recover in place.
                playerVelocity = Vector2.zero;
                Vector2 enemyAway = (Vector2)enemy.Object.transform.position - (Vector2)player.transform.position;
                if (enemyAway.sqrMagnitude < 0.001f) enemyAway = Vector2.up;
                enemyAway.Normalize();
                enemy.Object.transform.position = (Vector2)player.transform.position + enemyAway * (enemy.Radius + 0.55f);
                enemy.Velocity = enemyAway * Mathf.Max(enemy.Speed * 0.9f, 2.2f);
                if (playerHealth <= 0f) Finish(GameMode.Lost);
            }
        }
    }

    private void FireWeapon(float dt)
    {
        weaponTimer -= dt;
        if (weaponTimer > 0f) return;
        weaponTimer = weaponCooldown;

        Enemy nearest = FindNearestEnemy();
        if (nearest == null) return;
        Vector2 direction = ((Vector2)nearest.Object.transform.position - (Vector2)player.transform.position).normalized;
        if (direction.sqrMagnitude < 0.001f) direction = lastAim;
        lastAim = direction;

        int count = projectileCount + (cinderVolley ? 1 : 0);
        float spread = count <= 1 ? 0f : 18f;
        for (int i = 0; i < count; i++)
        {
            float offset = count == 1 ? 0f : Mathf.Lerp(-spread, spread, i / (float)(count - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, offset) * direction;
            Sprite sprite = cinderVolley ? emberBoltSprite : boltSprite;
            GameObject projectileObject = CreateSpriteObject("Auto Bolt", sprite, player.transform.position, 0.42f, 16);
            projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg - 90f);
            projectiles.Add(new Projectile
            {
                Object = projectileObject,
                Velocity = shotDirection * projectileSpeed,
                Damage = weaponDamage,
                Life = 2.4f,
                Pierce = projectilePierce
            });
        }
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

            projectile.Object.transform.position += new Vector3(projectile.Velocity.x, projectile.Velocity.y, 0f) * dt;
            bool consumed = false;
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Health <= 0f || enemy.Object == null || projectile.HitIds.Contains(enemy.Id)) continue;
                if (Vector2.Distance(projectile.Object.transform.position, enemy.Object.transform.position) > enemy.Radius + 0.20f) continue;

                projectile.HitIds.Add(enemy.Id);
                projectile.Pierce -= 1;
                enemy.Health -= projectile.Damage;
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
        int value = enemy.Kind == EnemyKind.Brute ? 4 : 1;
        SpawnGem(position, value);
        kills++;
        score += enemy.Kind == EnemyKind.Brute ? 30 : 10;
        if (enemy.Kind == EnemyKind.Brute || Random.value < 0.025f) SpawnChest(position);
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
        };

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
        float radius = cinderVolley ? 1.45f : 1.22f;
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

    private void SpawnChest(Vector2 suggestedPosition)
    {
        Vector2 position = suggestedPosition;
        if (position == Vector2.zero || Vector2.Distance(position, player.transform.position) < 2.7f)
        {
            position = new Vector2(Random.Range(ArenaLeft + 1f, ArenaRight - 1f), Random.Range(ArenaBottom + 1f, ArenaTop - 1f));
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
        defeatedBuffer.Clear();
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Object != null && Vector2.Distance(player.transform.position, enemy.Object.transform.position) <= 2.55f)
            {
                enemy.Health -= 24f;
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
        pulseRadius += 8.6f * dt;
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
