using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Presentation replacement for the original prototype.
/// It keeps PixelSurvivorGame as the gameplay simulation, suppresses its developer HUD,
/// reskins every runtime-generated object, and draws a compact fantasy-life UI.
/// Authored Nightfall Meadow assets are preferred at runtime, with the procedural
/// pixel kit retained as a safe fallback for a clean first import. No third-party
/// game assets are used.
/// </summary>
[DefaultExecutionOrder(10000)]
public sealed class CuteNightfallPresentation : MonoBehaviour
{
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float ReferenceWidth = 960f;
    private static readonly Vector2 PresentationSpawnPoint = new Vector2(0f, -2.65f);
    // Match the authored 480x270 / 16:9 composition so the HUD scales cleanly
    // at 960x540, 1280x720 and 1920x1080 without editor letterboxing.
    private const float ReferenceHeight = 540f;
    private const float WorldHalfWidth = 40f;
    private const float WorldHalfHeight = 28f;

    private Component simulation;
    private MethodInfo simulationUpdate;
    private MethodInfo applyUpgrade;
    private readonly Dictionary<string, FieldInfo> fieldCache = new Dictionary<string, FieldInfo>();
    private bool attached;
    private bool reflectionFailed;
    private float skinRefresh;

    private Sprite heroSprite;
    private Sprite slimeSprite;
    private Sprite hornSprite;
    private Sprite woolSprite;
    private Sprite mothSprite;
    private Sprite mushroomSprite;
    private Sprite witchSprite;
    private Sprite bossSprite;
    private Sprite sparkSprite;
    private Sprite emberSprite;
    private Sprite noteSprite;
    private Sprite berrySprite;
    private Sprite needleSprite;
    private Sprite curseSeedSprite;
    private Sprite bossOrbSprite;
    private Sprite hitSparkSprite;
    private Sprite bossBurstSprite;
    private Sprite telegraphSprite;
    private Sprite shardSprite;
    private Sprite chestSprite;
    private Sprite orbitSprite;
    private Sprite portraitSprite;
    private Sprite wandIcon;
    private Sprite ringIcon;
    private Sprite magnetIcon;
    private Sprite heartIcon;
    private Sprite bootIcon;
    private Sprite pulseIcon;
    private Sprite notesIcon;
    private Sprite berryIcon;
    private Sprite needleIcon;
    private Sprite fireflyIcon;
    private Sprite[] heroFrames;
    private float heroWalkTime;
    private Vector3 lastHeroPosition;
    private bool heroPositionKnown;
    private bool heroFacingLeft;

    private Transform farDepthLayer;
    private Transform midDepthLayer;
    private Transform nearDepthLayer;
    private GameObject playerLanternGlow;
    private Sprite moonGlowSprite;
    private Sprite lanternGlowSprite;
    private readonly Dictionary<GameObject, Vector3> actorBaseScales = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<GameObject, PolygonActorRig> polygonActorRigs = new Dictionary<GameObject, PolygonActorRig>();
    private readonly Dictionary<GameObject, Vector3> polygonLastPositions = new Dictionary<GameObject, Vector3>();

    private sealed class PolygonActorRig
    {
        public GameObject Root;
        public GameObject DepthPlate;
        public GameObject Silhouette;
        public GameObject Facet;
        public GameObject Accent;
        public readonly List<GameObject> Details = new List<GameObject>();
        public LineRenderer Outline;
        public float Phase;
        public bool Winged;
    }

    private Texture2D authoredBackground;
    private Texture2D authoredSpriteSheet;
    private Texture2D authoredUiAtlas;
    private Texture2D slotPanelTexture;
    private Texture2D cardPanelTexture;
    private Texture2D cardMintTexture;
    private Texture2D cardCoralTexture;
    private Sprite actorShadowSprite;
    private readonly Dictionary<GameObject, GameObject> actorShadowObjects = new Dictionary<GameObject, GameObject>();

    private Texture2D woodPanel;
    private Texture2D parchmentPanel;
    private Texture2D parchmentHover;
    private Texture2D darkPanel;
    private Texture2D barBack;
    private Texture2D healthFill;
    private Texture2D xpFill;
    private Texture2D pulseFill;
    private Texture2D veil;

    private GUIStyle titleStyle;
    private GUIStyle headingStyle;
    private GUIStyle bodyStyle;
    private GUIStyle tinyStyle;
    private GUIStyle centeredStyle;
    private GUIStyle buttonStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle cardBodyStyle;
    private GUIStyle parchmentPanelStyle;
    private GUIStyle darkPanelStyle;
    private GUIStyle timerPanelStyle;
    private GUIStyle slotPanelStyle;
    private GUIStyle cardNormalStyle;
    private GUIStyle cardMintStyle;
    private GUIStyle cardCoralStyle;
    private bool stylesBuilt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (GameObject.Find("Cute Nightfall Presentation") == null)
        {
            new GameObject("Cute Nightfall Presentation").AddComponent<CuteNightfallPresentation>();
        }
    }

    private IEnumerator Start()
    {
        BuildMaterials();
        BuildSprites();
        BuildFairytaleField();

        for (int frame = 0; frame < 30 && !attached; frame++)
        {
            TryAttach();
            if (!attached) yield return null;
        }
    }

    private void TryAttach()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this) continue;
            Type type = behaviour.GetType();
            if (type.Name != "PixelSurvivorGame") continue;

            simulation = behaviour;
            simulationUpdate = type.GetMethod("Update", InstancePrivate);
            applyUpgrade = type.GetMethod("ApplyUpgrade", InstancePrivate);
            behaviour.enabled = false;
            attached = simulationUpdate != null;
            ApplySkinToWorld(true);
            break;
        }
    }

    private void Update()
    {
        if (!attached)
        {
            TryAttach();
            return;
        }

        try
        {
            simulationUpdate.Invoke(simulation, null);
        }
        catch (TargetInvocationException exception)
        {
            if (!reflectionFailed)
            {
                reflectionFailed = true;
                Debug.LogException(exception.InnerException ?? exception);
            }
        }

        UpdateTwoPointFiveSpace();
        UpdatePolygonActorMotion();
        skinRefresh -= Time.unscaledDeltaTime;
        if (skinRefresh <= 0f)
        {
            skinRefresh = 0.12f;
            ApplySkinToWorld(false);
        }
    }

    private void BuildMaterials()
    {
        authoredUiAtlas = Resources.Load<Texture2D>("NightfallMeadow/ui_atlas_512");
        if (authoredUiAtlas != null) authoredUiAtlas.filterMode = FilterMode.Point;

        woodPanel = CutePixelKit.PanelTexture(
            CutePixelKit.Hex("A86D62"), CutePixelKit.MascotOutline, CutePixelKit.Hex("F6C6B6"), 16, 3);
        Texture2D rawParchmentPanel = CutePixelKit.CropAtlasTexture(
            authoredUiAtlas, "Authored Parchment Panel", 15, 35, 229, 65);
        parchmentPanel = CutePixelKit.FlattenPanelInterior(
            rawParchmentPanel,
            "Clean Authored Parchment Panel",
            10,
            CutePixelKit.Hex("F6E8BD")) ?? CutePixelKit.PanelTexture(
                CutePixelKit.Hex("F6E8BD"), CutePixelKit.Hex("5D3E39"), CutePixelKit.Hex("FFF7D9"), 16, 3);
        parchmentHover = CutePixelKit.CropAtlasTexture(
            authoredUiAtlas, "Authored Hover Panel", 134, 116, 106, 31) ?? CutePixelKit.PanelTexture(
                CutePixelKit.Hex("FFF3CB"), CutePixelKit.Hex("9C5B4A"), Color.white, 16, 3);
        darkPanel = CutePixelKit.PanelTexture(
            new Color(0.16f, 0.115f, 0.22f, 0.95f), CutePixelKit.MascotPink, CutePixelKit.Hex("765A78"), 16, 3);
        slotPanelTexture = CutePixelKit.CropAtlasTexture(
            authoredUiAtlas, "Authored Equipment Slot", 15, 178, 51, 49) ?? darkPanel;
        Texture2D rawCardPanel = CutePixelKit.CropAtlasTexture(
            authoredUiAtlas, "Authored Upgrade Card", 15, 260, 148, 148);
        Texture2D rawCardMint = CutePixelKit.CropAtlasTexture(
            authoredUiAtlas, "Authored Rare Upgrade Card", 177, 260, 148, 148);
        Texture2D rawCardCoral = CutePixelKit.CropAtlasTexture(
            authoredUiAtlas, "Authored Evolution Upgrade Card", 339, 260, 148, 148);
        cardPanelTexture = CutePixelKit.FlattenPanelInterior(
            rawCardPanel,
            "Clean Authored Upgrade Card",
            16,
            CutePixelKit.Hex("F6E8BD")) ?? parchmentPanel;
        cardMintTexture = CutePixelKit.FlattenPanelInterior(
            rawCardMint,
            "Clean Authored Rare Upgrade Card",
            16,
            CutePixelKit.Hex("E4F0D7")) ?? parchmentHover;
        cardCoralTexture = CutePixelKit.FlattenPanelInterior(
            rawCardCoral,
            "Clean Authored Evolution Upgrade Card",
            16,
            CutePixelKit.Hex("F7D8D4")) ?? parchmentHover;
        barBack = CutePixelKit.SolidTexture(new Color(0.12f, 0.085f, 0.18f, 0.94f), "Bar Back");
        healthFill = CutePixelKit.SolidTexture(CutePixelKit.MascotBlush, "Health Fill");
        xpFill = CutePixelKit.SolidTexture(CutePixelKit.MascotMint, "XP Fill");
        pulseFill = CutePixelKit.SolidTexture(CutePixelKit.MascotLilac, "Pulse Fill");
        veil = CutePixelKit.SolidTexture(new Color(0.10f, 0.065f, 0.16f, 0.78f), "Overlay Veil");
    }

    private void BuildSprites()
    {
        actorShadowSprite = CutePixelKit.CreateSprite(
            "Shared Grounding Shadow",
            new[]
            {
                "...............",
                "....SSSSSSS....",
                ".SSSSSSSSSSSSS.",
                "SSSSSSSSSSSSSSS",
                ".SSSSSSSSSSSSS.",
                "....SSSSSSS....",
                "..............."
            },
            new Dictionary<char, Color> { { 'S', new Color(0.035f, 0.025f, 0.055f, 0.62f) } },
            16f);

        authoredSpriteSheet = Resources.Load<Texture2D>("NightfallMeadow/sprite_sheet_256");
        if (authoredSpriteSheet != null)
        {
            heroFrames = new Sprite[4];
            for (int frame = 0; frame < heroFrames.Length; frame++)
            {
                heroFrames[frame] = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Meadow Courier " + frame, frame, 1);
            }
            heroSprite = heroFrames[0];
            portraitSprite = heroSprite;
            slimeSprite = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Dusk Slime", 0, 2);
            hornSprite = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Moonhorn", 5, 2);
            sparkSprite = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Mint Star Spark", 0, 3);
            emberSprite = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Coral Star Spark", 3, 3);
            shardSprite = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Moon Dew", 1, 3);
            chestSprite = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Story Chest", 4, 4);
            orbitSprite = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Fairy Flame", 0, 5);

            wandIcon = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Star Wand Icon", 0, 4);
            ringIcon = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Hearth Notes Icon", 1, 4);
            magnetIcon = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Shepherd Crook Icon", 2, 4);
            heartIcon = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Berry Basket Icon", 3, 4);
            bootIcon = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Sewing Needle Icon", 5, 4);
            pulseIcon = CutePixelKit.CreateAtlasSprite(authoredSpriteSheet, "Protective Pulse Icon", 1, 5);
            BuildMascotSprites();
            return;
        }

        Dictionary<char, Color> hero = new Dictionary<char, Color>
        {
            { 'O', CutePixelKit.Ink }, { 'H', CutePixelKit.Hex("725047") },
            { 'S', CutePixelKit.Hex("FFD3B6") }, { 'C', CutePixelKit.Cream },
            { 'L', CutePixelKit.Lavender }, { 'M', CutePixelKit.Mint },
            { 'B', CutePixelKit.Hex("5A3A38") }, { 'W', CutePixelKit.White }
        };
        string[] heroRows =
        {
            ".....OOOO.....",
            "...OOHHHHOO...",
            "..OHHHHHHHHO..",
            "..OHSSSSSSHO..",
            ".OHSSWSSWSSHO.",
            ".OHSSSSSSSSHO.",
            "..OHSSSSSSHO..",
            "...OOLLLLOO...",
            "..OOLLLLLLOO..",
            ".OCCLLLLLLCCO.",
            ".OCCCCCCCCCCO.",
            "..OCCMCCMCCO..",
            "..OCCMCCMCCO..",
            "...OBB..BBO...",
            "..OBBB..BBBO..",
            "..............."
        };
        heroSprite = CutePixelKit.CreateSprite("Meadow Courier", heroRows, hero, 16f);
        portraitSprite = CutePixelKit.CreateSprite("Courier Portrait", heroRows, hero, 13f);

        slimeSprite = CutePixelKit.CreateSprite(
            "Dusk Slime",
            new[]
            {
                ".............", "....OOOO.....", "..OOMMMMOO...", ".OMMMMMMMMOO..",
                "OMMMWMMWMMMOO", "OMMMMMMMMMMMO", ".OMMMMMMMMMMO.", "..OOMMMMMOO..",
                "....OOOOO....", "............."
            },
            new Dictionary<char, Color>
            {
                { 'O', CutePixelKit.Ink }, { 'M', CutePixelKit.Hex("8B70C7") }, { 'W', CutePixelKit.White }
            }, 16f);

        hornSprite = CutePixelKit.CreateSprite(
            "Moonhorn",
            new[]
            {
                "..G.......G..", ".GGOOOOOOOGG.", "..OBBBBBBBO..", ".OBBBBBBBBBO.",
                "OBBBWWBBWWBBO", "OBBBBBBBBBBBO", ".OBBBPPPPBBBO.", "..OBBBBBBBO..",
                "..OOOBBBOOO..", ".OO..O..O..OO", "............."
            },
            new Dictionary<char, Color>
            {
                { 'O', CutePixelKit.Ink }, { 'B', CutePixelKit.Hex("A65F55") },
                { 'P', CutePixelKit.Peach }, { 'G', CutePixelKit.Gold }, { 'W', CutePixelKit.White }
            }, 16f);

        sparkSprite = CutePixelKit.CreateSprite(
            "Star Spark",
            new[] { "..W..", ".WMW.", "WMMMW", ".WMW.", "..W.." },
            new Dictionary<char, Color> { { 'W', CutePixelKit.White }, { 'M', CutePixelKit.Mint } }, 16f);
        emberSprite = CutePixelKit.CreateSprite(
            "Warm Ember",
            new[] { "..G..", ".GPG.", "GPCPG", ".GPG.", "..G.." },
            new Dictionary<char, Color>
            {
                { 'G', CutePixelKit.Gold }, { 'P', CutePixelKit.Peach }, { 'C', CutePixelKit.Coral }
            }, 16f);
        shardSprite = CutePixelKit.CreateSprite(
            "Moon Dew",
            new[] { "..W..", ".WMW.", "WMSMW", ".WMW.", "..W.." },
            new Dictionary<char, Color>
            {
                { 'W', CutePixelKit.White }, { 'M', CutePixelKit.Mint }, { 'S', CutePixelKit.Sky }
            }, 16f);
        chestSprite = CutePixelKit.CreateSprite(
            "Story Chest",
            new[]
            {
                "..OOOOOOOO..", ".OBBBBBBBBO.", "OBBGGGGGGBBO", "OBBBBBBBBBBO",
                "OOOOOGGOOOOO", "OBBBBBBBBBBO", "OBBBBGGBBBBO", ".OOOOOOOOOO.", "............"
            },
            new Dictionary<char, Color>
            {
                { 'O', CutePixelKit.Ink }, { 'B', CutePixelKit.Hex("8A5A3F") }, { 'G', CutePixelKit.Gold }
            }, 16f);
        orbitSprite = CutePixelKit.CreateSprite(
            "Fairy Flame",
            new[] { "..G..", ".GPG.", "GPWPG", ".GPG.", "..G.." },
            new Dictionary<char, Color>
            {
                { 'G', CutePixelKit.Gold }, { 'P', CutePixelKit.Peach }, { 'W', CutePixelKit.White }
            }, 16f);

        wandIcon = CutePixelKit.CreateSprite(
            "Wand Icon",
            new[] { "......W", ".....WG", "....WG.", "...WG..", "..WG...", ".BO....", "BO....." },
            new Dictionary<char, Color>
            {
                { 'W', CutePixelKit.White }, { 'G', CutePixelKit.Gold }, { 'B', CutePixelKit.Hex("76513F") }, { 'O', CutePixelKit.Ink }
            }, 12f);
        ringIcon = CutePixelKit.CreateSprite(
            "Ring Icon",
            new[] { "..GGG..", ".G...G.", "G..W..G", "G.....G", ".G...G.", "..GGG..", "......." },
            new Dictionary<char, Color> { { 'G', CutePixelKit.Gold }, { 'W', CutePixelKit.White } }, 12f);
        magnetIcon = CutePixelKit.CreateSprite(
            "Magnet Icon",
            new[] { "RR...RR", "RR...RR", "RR...RR", ".RR.RR.", "..RRR..", "...R...", "......." },
            new Dictionary<char, Color> { { 'R', CutePixelKit.Coral } }, 12f);
        heartIcon = CutePixelKit.CreateSprite(
            "Heart Icon",
            new[] { ".RR.RR.", "RRRRRRR", "RRRRRRR", ".RRRRR.", "..RRR..", "...R...", "......." },
            new Dictionary<char, Color> { { 'R', CutePixelKit.Coral } }, 12f);
        bootIcon = CutePixelKit.CreateSprite(
            "Boot Icon",
            new[] { "..BBB..", "..BBB..", "..BBB..", "..BBBB.", ".BBBBB.", "BBBBBB.", "......." },
            new Dictionary<char, Color> { { 'B', CutePixelKit.Hex("7A5141") } }, 12f);
        pulseIcon = orbitSprite;
        BuildMascotSprites();
    }

    private void BuildMascotSprites()
    {
        Dictionary<char, Color> courierPalette = new Dictionary<char, Color>
        {
            { 'O', CutePixelKit.MascotOutline },
            { 'C', CutePixelKit.MascotCream },
            { 'P', CutePixelKit.MascotPink },
            { 'R', CutePixelKit.MascotBlush },
            { 'M', CutePixelKit.MascotMint },
            { 'D', CutePixelKit.MascotOutline },
            { 'Y', CutePixelKit.MascotGold }
        };
        string[][] courierFrames =
        {
            new[]
            {
                "....OO....OO....", "...OOOOOOOOOO...", "..OOCCCCCCOOO..", ".OCCCCCCCCCCCCO.",
                "OCCCDCCCCDCCCOO", "OCCRRCCCCRRCCCO", ".OCCCCCCCCCCCCO.", "..OPPPPPPPPPPO..",
                "..OPPPPPPPPPPO..", "...OMMMMMMMO....", "...OMMMMMMMO....", "....OO..OO......",
                "...OOO..OOO.....", "................", "................", "................"
            },
            new[]
            {
                "....OO....OO....", "...OOOOOOOOOO...", "..OOCCCCCCOOO..", ".OCCCCCCCCCCCCO.",
                "OCCCDCCCCDCCCOO", "OCCRRCCCCRRCCCO", ".OCCCCCCCCCCCCO.", "..OPPPPPPPPPPO..",
                "..OPPPPPPPPPPO..", "...OMMMMMMMO....", "...OMMMMMMMO....", "...OO...OO......",
                "..OOOO..OOOO....", "................", "................", "................"
            },
            new[]
            {
                "....OO....OO....", "...OOOOOOOOOO...", "..OOCCCCCCOOO..", ".OCCCCCCCCCCCCO.",
                "OCCCDCCCCDCCCOO", "OCCRRCCCCRRCCCO", ".OCCCCCCCCCCCCO.", "..OPPPPPPPPPPO..",
                "..OPPPPPPPPPPO..", "...OMMMMMMMO....", "...OMMMMMMMO....", "....OO..OO......",
                "....OOO..OOO....", "................", "................", "................"
            },
            new[]
            {
                "....OO....OO....", "...OOOOOOOOOO...", "..OOCCCCCCOOO..", ".OCCCCCCCCCCCCO.",
                "OCCCDCCCCDCCCOO", "OCCRRCCCCRRCCCO", ".OCCCCCCCCCCCCO.", "..OPPPPPPPPPPO..",
                "..OPPPPPPPPPPO..", "...OMMMMMMMO....", "...OMMMMMMMO....", "...OO...OO......",
                "..OOOO..OOOO....", "................", "................", "................"
            }
        };
        heroFrames = new Sprite[courierFrames.Length];
        for (int frame = 0; frame < courierFrames.Length; frame++)
        {
            heroFrames[frame] = CutePixelKit.CreateSprite(
                "Tiny Meadow Courier " + frame,
                courierFrames[frame],
                courierPalette,
                16f);
        }
        heroSprite = heroFrames[0];
        portraitSprite = CutePixelKit.CreateSprite("Tiny Courier Portrait", courierFrames[0], courierPalette, 13f);

        Dictionary<char, Color> berryPalette = new Dictionary<char, Color>
        {
            { 'O', CutePixelKit.MascotOutline }, { 'B', CutePixelKit.MascotPink },
            { 'R', CutePixelKit.MascotBlush }, { 'D', CutePixelKit.MascotOutline },
            { 'W', CutePixelKit.White }
        };
        slimeSprite = CutePixelKit.CreateSprite(
            "Berry Mochi Mote",
            new[]
            {
                "................", ".....OOOOOO.....", "...OOOBBBBOOO..", "..OOBBBBBBBBOO..",
                ".OOBBDBBDBBBOO..", "OOBBBBBBBBBBBBO.", "OOBBRBBBBBRBBO.", ".OOBBBBBBBBBBO..",
                "..OOBBBBBBBBO...", "...OOOBBBOOO....", ".....OOOO.......", "................"
            },
            berryPalette,
            16f);

        Dictionary<char, Color> puffPalette = new Dictionary<char, Color>
        {
            { 'O', CutePixelKit.MascotOutline }, { 'L', CutePixelKit.MascotLilac },
            { 'C', CutePixelKit.MascotCream }, { 'M', CutePixelKit.MascotMint },
            { 'R', CutePixelKit.MascotBlush }, { 'D', CutePixelKit.MascotOutline }
        };
        hornSprite = CutePixelKit.CreateSprite(
            "Sleepy Moon Puff",
            new[]
            {
                "....LL....LL....", "...LLOO..OOLL...", "..LOOOOOOOOOOOL..", ".OOCCCCCCCCCCOO.",
                "OOCCCDCCCCDCCOOO", "OOCCRRCCCCRRCCOO", ".OCCCCCCCCCCCCO.", "..OMMMMMMMMMMO..",
                "...OMMMMMMMMO...", "...OO..OO..OO...", "..OOO..OO..OOO..", "................"
            },
            puffPalette,
            16f);

        Dictionary<char, Color> woolPalette = new Dictionary<char, Color>
        {
            { 'O', CutePixelKit.MascotOutline }, { 'M', CutePixelKit.MascotMint },
            { 'C', CutePixelKit.MascotCream }, { 'R', CutePixelKit.MascotBlush }
        };
        woolSprite = CutePixelKit.CreateSprite(
            "Wool Sprite",
            new[]
            {
                "............", "...OOOOOO...", "..OOMMMMOO..", ".OOMMMMMMOO.",
                "OOMMCMMCMMOO", "OMMMMMMMMMMO", "OMMRMMMMRMMO", ".OMMMMMMMMO.",
                "..OOMMMOO...", "...OO.OO....", "............"
            },
            woolPalette,
            16f);

        Dictionary<char, Color> mothPalette = new Dictionary<char, Color>
        {
            { 'O', CutePixelKit.MascotOutline }, { 'L', CutePixelKit.MascotLilac },
            { 'M', CutePixelKit.MascotMint }, { 'Y', CutePixelKit.MascotGold },
            { 'C', CutePixelKit.MascotCream }
        };
        mothSprite = CutePixelKit.CreateSprite(
            "Lantern Moth",
            new[]
            {
                "....O..O....", "...OO..OO...", "..OOLLLLOO..", ".OOLLLLLLOO.",
                "OOLLYCYLLOOO", "OOLLCCCCLLOO", ".OOLLLLLLOO.",
                "..OOYYYYOO..", "...OO..OO...", "....O..O....", "............"
            },
            mothPalette,
            16f);

        Dictionary<char, Color> mushroomPalette = new Dictionary<char, Color>
        {
            { 'O', CutePixelKit.MascotOutline }, { 'P', CutePixelKit.MascotPink },
            { 'R', CutePixelKit.MascotBlush }, { 'C', CutePixelKit.MascotCream },
            { 'M', CutePixelKit.MascotMint }
        };
        mushroomSprite = CutePixelKit.CreateSprite(
            "Mushroom Thief",
            new[]
            {
                "....OOOO....", "..OOPPPPOO..", ".OOPPPPPPPO.", "OOPPRPPRPPOO",
                "OOPPPPPPPPOO", "...OOCCOO...", "..OOCCCCOO..", ".OOCCMMCCOO.",
                "..OOCCCCOO..", "...OO..OO...", "............"
            },
            mushroomPalette,
            16f);

        Dictionary<char, Color> witchPalette = new Dictionary<char, Color>
        {
            { 'O', CutePixelKit.MascotOutline }, { 'L', CutePixelKit.MascotLilac },
            { 'C', CutePixelKit.MascotCream }, { 'P', CutePixelKit.MascotPink },
            { 'Y', CutePixelKit.MascotGold }
        };
        witchSprite = CutePixelKit.CreateSprite(
            "Hedge Witch",
            new[]
            {
                "....OOOO....", "...OLLLLO...", "..OLLLLLLLO..", ".OLLLLLLLLLO.",
                "OOLLCYCLLOOO", "OOCCPPCCOOO.", ".OOCCCCCCOO.", "..OOPPPPOO..",
                ".OOPPPPPPO..", "OOPPPPPPPPOO", "............"
            },
            witchPalette,
            16f);

        Dictionary<char, Color> bossPalette = new Dictionary<char, Color>
        {
            { 'O', CutePixelKit.MascotOutline }, { 'P', CutePixelKit.MascotPink },
            { 'R', CutePixelKit.MascotBlush }, { 'L', CutePixelKit.MascotLilac },
            { 'C', CutePixelKit.MascotCream }, { 'Y', CutePixelKit.MascotGold },
            { 'M', CutePixelKit.MascotMint }
        };
        bossSprite = CutePixelKit.CreateSprite(
            "Mallow Warden",
            new[]
            {
                "....OO....OO....", "...OOOYYYYOOO...", "..OOYYYYYYYYOO..", ".OOYYYYYYYYYYOO.",
                "OOYYLLCCLLYYOOOO", "OOYYCCCCCCYYOOOO", "OOYYRCCCCRYYOOOO", ".OOYYYYYYYYYYOO.",
                "..OOPPPPPPPPOO..", "..OPPPPPPPPPPO..", "...OOOMMMOOO....", "....OOO..OOO....",
                "................", "................", "................", "................"
            },
            bossPalette,
            16f);

        sparkSprite = CutePixelKit.CreateSprite(
            "Tiny Mint Star",
            new[] { "...W...", "..WMW..", ".WMMMW.", "WMMWMMW", ".WMMMW.", "..WMW..", "...W..." },
            new Dictionary<char, Color> { { 'W', CutePixelKit.White }, { 'M', CutePixelKit.MascotMint } },
            16f);
        noteSprite = CutePixelKit.CreateSprite(
            "Tiny Hearth Note",
            new[] { "...P...", "..PPP..", ".PPWPP.", "..PPP..", "...P...", "......." },
            new Dictionary<char, Color> { { 'P', CutePixelKit.MascotPink }, { 'W', CutePixelKit.White } },
            16f);
        berrySprite = CutePixelKit.CreateSprite(
            "Tiny Berry Toss",
            new[] { "..PP..", ".PPPP.", "PPWPPP", ".PPPP.", "..PP..", "......" },
            new Dictionary<char, Color> { { 'P', CutePixelKit.MascotBlush }, { 'W', CutePixelKit.White } },
            16f);
        needleSprite = CutePixelKit.CreateSprite(
            "Tiny Sewing Needle",
            new[] { ".....W", "....WW", "...WW.", "..WW..", ".WW...", "W....." },
            new Dictionary<char, Color> { { 'W', CutePixelKit.MascotCream } },
            16f);
        curseSeedSprite = CutePixelKit.CreateSprite(
            "Tiny Curse Seed",
            new[] { "...L...", "..LLL..", ".LLPLLL", "..LLL..", "...L...", "......." },
            new Dictionary<char, Color> { { 'L', CutePixelKit.MascotLilac }, { 'P', CutePixelKit.MascotPink } },
            16f);
        bossOrbSprite = CutePixelKit.CreateSprite(
            "Tiny Boss Orb",
            new[] { "...Y...", "..YYY..", ".YYPYY.", "..YYY..", "...Y...", "......." },
            new Dictionary<char, Color> { { 'Y', CutePixelKit.MascotGold }, { 'P', CutePixelKit.MascotPink } },
            16f);
        hitSparkSprite = CutePixelKit.CreateSprite(
            "Tiny Hit Spark",
            new[] { "...W...", ".W...W.", ".......", ".W...W.", "...W...", "......." },
            new Dictionary<char, Color> { { 'W', CutePixelKit.White } },
            16f);
        bossBurstSprite = CutePixelKit.CreateSprite(
            "Tiny Boss Burst",
            new[] { "...Y...", ".Y...Y.", "Y..P..Y", "...P...", "Y..P..Y", ".Y...Y.", "...Y..." },
            new Dictionary<char, Color> { { 'Y', CutePixelKit.MascotGold }, { 'P', CutePixelKit.MascotPink } },
            16f);
        telegraphSprite = CutePixelKit.CreateSprite(
            "Tiny Charge Telegraph",
            new[] { ".PPPPPP.", "P......P", "P......P", "P......P", "P......P", "P......P", ".PPPPPP." },
            new Dictionary<char, Color> { { 'P', new Color(0.95f, 0.48f, 0.66f, 0.72f) } },
            16f);
        emberSprite = CutePixelKit.CreateSprite(
            "Tiny Berry Heart",
            new[] { ".PP.PP.", "PPPPPPP", "PPPPPPP", ".PPPPP.", "..PPP..", "...P...", "......." },
            new Dictionary<char, Color> { { 'P', CutePixelKit.MascotPink } },
            16f);
        shardSprite = CutePixelKit.CreateSprite(
            "Tiny Moon Dew",
            new[] { "...W...", "..WMW..", ".WMMMW.", ".WMSMW.", "..WMW..", "...W...", "......." },
            new Dictionary<char, Color> { { 'W', CutePixelKit.White }, { 'M', CutePixelKit.MascotMint }, { 'S', CutePixelKit.MascotLilac } },
            16f);
        chestSprite = CutePixelKit.CreateSprite(
            "Tiny Gift Chest",
            new[] { "..OOOOO..", ".OBBBBBBO.", "OBBYYBBBBO", "OBBBBBBBO", "OOOYYOOOO", "OBBBBBBBO", ".OOOOOOO.", "........." },
            new Dictionary<char, Color> { { 'O', CutePixelKit.MascotOutline }, { 'B', CutePixelKit.MascotPink }, { 'Y', CutePixelKit.MascotGold } },
            16f);
        orbitSprite = CutePixelKit.CreateSprite(
            "Tiny Flower Puff",
            new[] { "...M...", ".MPPM..", "MPWPM..", ".MPPM..", "...M...", "......." },
            new Dictionary<char, Color> { { 'M', CutePixelKit.MascotMint }, { 'P', CutePixelKit.MascotPink }, { 'W', CutePixelKit.White } },
            16f);

        wandIcon = CutePixelKit.CreateSprite(
            "Tiny Star Wand Icon",
            new[] { "..Y....", ".YYY...", "..O....", "..O....", ".OO....", "OO.....", "......." },
            new Dictionary<char, Color> { { 'Y', CutePixelKit.MascotGold }, { 'O', CutePixelKit.MascotOutline } }, 12f);
        ringIcon = CutePixelKit.CreateSprite(
            "Tiny Note Ring Icon",
            new[] { "..P.P..", ".PPPPP.", "PP...PP", "PP...PP", ".PPPPPP", "..PPP..", "......." },
            new Dictionary<char, Color> { { 'P', CutePixelKit.MascotPink } }, 12f);
        magnetIcon = CutePixelKit.CreateSprite(
            "Tiny Mint Magnet Icon",
            new[] { "MM...MM", "MM...MM", ".M...M.", "..MMM..", "...M...", ".......", "......." },
            new Dictionary<char, Color> { { 'M', CutePixelKit.MascotMint } }, 12f);
        heartIcon = CutePixelKit.CreateSprite(
            "Tiny Berry Basket Icon",
            new[] { ".PP.PP.", "PPPPPPP", "PPPPPPP", ".PPPPP.", "..PPP..", "...P...", "......." },
            new Dictionary<char, Color> { { 'P', CutePixelKit.MascotBlush } }, 12f);
        bootIcon = CutePixelKit.CreateSprite(
            "Tiny Leaf Shoe Icon",
            new[] { "...M...", "..MMM..", ".MMMM..", "MMMMM..", "..MM...", ".MMM...", "......." },
            new Dictionary<char, Color> { { 'M', CutePixelKit.MascotMint } }, 12f);
        pulseIcon = CutePixelKit.CreateSprite(
            "Tiny Comfort Pulse Icon",
            new[] { "...L...", ".LLL...", "LLLLLLL", ".LLL...", "...L...", ".......", "......." },
            new Dictionary<char, Color> { { 'L', CutePixelKit.MascotLilac } }, 12f);
        notesIcon = CutePixelKit.CreateSprite(
            "Tiny Hearth Notes Icon",
            new[] { "...P...", "..PPP..", ".PPWPP.", "..PPP..", "...P...", ".......", "......." },
            new Dictionary<char, Color> { { 'P', CutePixelKit.MascotPink }, { 'W', CutePixelKit.White } }, 12f);
        berryIcon = CutePixelKit.CreateSprite(
            "Tiny Berry Weapon Icon",
            new[] { "..PP..", ".PPPP.", "PPWPPP", ".PPPP.", "..PP..", ".......", "......." },
            new Dictionary<char, Color> { { 'P', CutePixelKit.MascotBlush }, { 'W', CutePixelKit.White } }, 12f);
        needleIcon = CutePixelKit.CreateSprite(
            "Tiny Needle Weapon Icon",
            new[] { ".....W", "....WW", "...WW.", "..WW..", ".WW...", "W.....", "......." },
            new Dictionary<char, Color> { { 'W', CutePixelKit.MascotCream } }, 12f);
        fireflyIcon = CutePixelKit.CreateSprite(
            "Tiny Firefly Jar Icon",
            new[] { "..MMM..", ".MOOOM.", ".MOYOM.", ".MOOOM.", "..MMM..", "...Y...", "......." },
            new Dictionary<char, Color> { { 'M', CutePixelKit.MascotMint }, { 'O', CutePixelKit.MascotOutline }, { 'Y', CutePixelKit.MascotGold } }, 12f);
    }

    private void BuildFairytaleField()
    {
        GameObject root = new GameObject("Fairytale Meadow");
        farDepthLayer = CreateDepthLayer(root.transform, "2.5D Far Meadow");
        midDepthLayer = CreateDepthLayer(root.transform, "2.5D Mid Meadow");
        nearDepthLayer = CreateDepthLayer(root.transform, "2.5D Near Meadow");

        authoredBackground = Resources.Load<Texture2D>("NightfallMeadow/background_moonlit_clearing_480x270");
        if (authoredBackground != null)
        {
            authoredBackground.filterMode = FilterMode.Point;
            authoredBackground.wrapMode = TextureWrapMode.Clamp;
            // Keep the authored texture available for the UI pipeline, but use
            // a generated low-poly diorama for the actual playfield. A textured
            // rectangle behind mesh characters would read as pixel art, not as
            // the requested polygon style.
            BuildPolygonBackdrop();
            Build2PointFiveProps();
            Build2PointFiveLighting();
            return;
        }

        CutePixelKit.RectObject(farDepthLayer, "Meadow Ground", Vector2.zero, new Vector2(15.8f, 9.0f), CutePixelKit.Hex("273B36"), -42);
        CutePixelKit.RectObject(farDepthLayer, "Moon Wash", new Vector2(1.7f, 1.1f), new Vector2(9.4f, 5.8f), new Color(0.30f, 0.42f, 0.48f, 0.18f), -41);

        Vector2[] stones =
        {
            new Vector2(-6.4f, 2.8f), new Vector2(-5.1f, -2.7f), new Vector2(-2.8f, 3.25f),
            new Vector2(3.8f, 3.0f), new Vector2(6.3f, 1.3f), new Vector2(5.7f, -3.1f),
            new Vector2(0.5f, -3.5f)
        };
        foreach (Vector2 point in stones)
        {
            CutePixelKit.RectObject(root.transform, "Soft Stone", point, new Vector2(0.38f, 0.18f), CutePixelKit.Hex("66716E"), -35);
            CutePixelKit.RectObject(root.transform, "Stone Light", point + new Vector2(-0.06f, 0.055f), new Vector2(0.18f, 0.045f), CutePixelKit.Hex("94A19A"), -34);
        }

        Vector2[] flowers =
        {
            new Vector2(-6.8f, 1.1f), new Vector2(-5.8f, 3.3f), new Vector2(-4.5f, -3.4f),
            new Vector2(-2.1f, 2.8f), new Vector2(2.8f, -3.35f), new Vector2(4.6f, 2.65f),
            new Vector2(6.8f, -1.7f), new Vector2(1.1f, 3.5f)
        };
        foreach (Vector2 point in flowers)
        {
            CutePixelKit.RectObject(root.transform, "Flower Stem", point + new Vector2(0f, -0.08f), new Vector2(0.035f, 0.16f), CutePixelKit.Leaf, -33);
            CutePixelKit.RectObject(root.transform, "Flower", point, new Vector2(0.11f, 0.11f), CutePixelKit.Peach, -32);
        }

        Vector2[] fireflies =
        {
            new Vector2(-6.2f, -0.7f), new Vector2(-4.0f, 2.0f), new Vector2(-1.5f, -2.8f),
            new Vector2(0.1f, 2.5f), new Vector2(2.1f, -1.9f), new Vector2(4.1f, 1.5f),
            new Vector2(6.4f, 3.2f), new Vector2(6.0f, -2.4f)
        };
        foreach (Vector2 point in fireflies)
        {
            CutePixelKit.RectObject(root.transform, "Firefly Glow", point, new Vector2(0.12f, 0.12f), new Color(1f, 0.83f, 0.42f, 0.25f), -29);
            CutePixelKit.RectObject(root.transform, "Firefly", point, new Vector2(0.035f, 0.035f), CutePixelKit.Gold, -28);
        }

        Build2PointFiveProps();
        Build2PointFiveLighting();
    }

    private void BuildPolygonBackdrop()
    {
        CreatePolygonObject(
            "Low Poly Night Backdrop",
            farDepthLayer,
            Vector2.zero,
            new[]
            {
                new Vector2(-WorldHalfWidth - 2f, -WorldHalfHeight - 2f),
                new Vector2(WorldHalfWidth + 2f, -WorldHalfHeight - 2f),
                new Vector2(WorldHalfWidth + 2f, WorldHalfHeight + 2f),
                new Vector2(-WorldHalfWidth - 2f, WorldHalfHeight + 2f)
            },
            CutePixelKit.Hex("17263D"),
            -50);
        CreatePolygonObject(
            "Low Poly Sky Facet Blue",
            farDepthLayer,
            new Vector2(-3.2f, 2.25f),
            new[]
            {
                new Vector2(-5.2f, -1.3f), new Vector2(0.8f, -1.8f),
                new Vector2(3.0f, 1.7f), new Vector2(-1.0f, 2.4f)
            },
            new Color(0.16f, 0.24f, 0.40f, 0.92f),
            -49);
        CreatePolygonObject(
            "Low Poly Sky Facet Plum",
            farDepthLayer,
            new Vector2(3.55f, 2.45f),
            new[]
            {
                new Vector2(-2.6f, -1.4f), new Vector2(3.6f, -1.8f),
                new Vector2(4.4f, 1.8f), new Vector2(0.2f, 2.2f)
            },
            new Color(0.25f, 0.18f, 0.35f, 0.88f),
            -48);
        CreatePolygonObject(
            "Low Poly Meadow Plateau",
            farDepthLayer,
            new Vector2(0f, -1.15f),
            new[]
            {
                new Vector2(-WorldHalfWidth, -WorldHalfHeight), new Vector2(-28f, -18f),
                new Vector2(-14f, -21f), new Vector2(2f, -17f),
                new Vector2(18f, -20f), new Vector2(WorldHalfWidth, -16f),
                new Vector2(WorldHalfWidth, -WorldHalfHeight), new Vector2(-WorldHalfWidth, -WorldHalfHeight)
            },
            new Color(0.12f, 0.32f, 0.30f, 1f),
            -40);
        CreatePolygonObject(
            "Low Poly Meadow Highlight",
            farDepthLayer,
            new Vector2(-0.5f, -2.05f),
            new[]
            {
                new Vector2(-6.8f, -1.05f), new Vector2(-2.2f, -0.28f),
                new Vector2(1.7f, -0.82f), new Vector2(5.8f, -0.18f),
                new Vector2(7.0f, -0.92f), new Vector2(2.2f, -1.72f),
                new Vector2(-2.4f, -1.45f)
            },
            new Color(0.20f, 0.42f, 0.37f, 0.76f),
            -39);
        CreatePolygonObject(
            "Low Poly Distant Ridge",
            farDepthLayer,
            new Vector2(0f, 1.3f),
            new[]
            {
                new Vector2(-WorldHalfWidth, -0.28f), new Vector2(-34f, 0.85f),
                new Vector2(-25f, 0.35f), new Vector2(-17f, 1.15f),
                new Vector2(-8f, 0.42f), new Vector2(2f, 1.05f),
                new Vector2(12f, 0.30f), new Vector2(22f, 1.2f),
                new Vector2(30f, 0.45f), new Vector2(WorldHalfWidth, 1.0f),
                new Vector2(WorldHalfWidth, -0.72f), new Vector2(-WorldHalfWidth, -0.72f)
            },
            new Color(0.08f, 0.23f, 0.28f, 0.84f),
            -38);

        BuildLargeMeadowTiles();
    }

    private void BuildLargeMeadowTiles()
    {
        int tileIndex = 0;
        for (int x = -36; x <= 36; x += 12)
        {
            for (int y = -24; y <= 24; y += 10)
            {
                float skew = Mathf.Sin((x + y) * 0.23f) * 0.9f;
                Color tileColor;
                int palette = Mathf.Abs((x / 12) + (y / 10)) % 4;
                if (palette == 0) tileColor = new Color(0.11f, 0.29f, 0.30f, 0.96f);
                else if (palette == 1) tileColor = new Color(0.13f, 0.33f, 0.31f, 0.96f);
                else if (palette == 2) tileColor = new Color(0.12f, 0.30f, 0.34f, 0.96f);
                else tileColor = new Color(0.15f, 0.28f, 0.34f, 0.96f);

                CreatePolygonObject(
                    "Meadow Tile " + tileIndex++,
                    farDepthLayer,
                    new Vector2(x + skew, y),
                    new[]
                    {
                        new Vector2(-7.0f, -5.2f), new Vector2(2.2f, -5.5f),
                        new Vector2(7.1f, -1.7f), new Vector2(4.6f, 4.6f),
                        new Vector2(-2.4f, 5.2f), new Vector2(-7.2f, 1.4f)
                    },
                    tileColor,
                    -41);
            }
        }
    }

    private Transform CreateDepthLayer(Transform parent, string name)
    {
        GameObject layer = new GameObject(name);
        layer.transform.SetParent(parent, false);
        return layer.transform;
    }

    private void Build2PointFiveProps()
    {
        Vector2[] midTrees =
        {
            new Vector2(-6.45f, 2.65f), new Vector2(6.55f, 2.55f),
            new Vector2(-6.8f, -0.75f), new Vector2(6.85f, -0.65f)
        };
        for (int i = 0; i < midTrees.Length; i++)
        {
            CreatePolygonTree(midDepthLayer, "Midground Polygon Canopy " + i, midTrees[i], 0.92f, -12);
        }

        Vector2[] nearTrees =
        {
            new Vector2(-6.95f, -3.45f), new Vector2(6.85f, -3.55f),
            new Vector2(-6.85f, -2.15f), new Vector2(6.9f, -2.05f)
        };
        for (int i = 0; i < nearTrees.Length; i++)
        {
            CreatePolygonTree(nearDepthLayer, "Near Polygon Canopy " + i, nearTrees[i], 1.22f, 120);
        }

        BuildWorldPolygonFoliage();

        Vector2[] foregroundGrass =
        {
            new Vector2(-3.1f, -3.65f), new Vector2(-1.75f, -3.8f),
            new Vector2(1.9f, -3.78f), new Vector2(3.45f, -3.62f)
        };
        for (int i = 0; i < foregroundGrass.Length; i++)
        {
            CreatePolygonGrassTuft(nearDepthLayer, "Foreground Polygon Grass " + i, foregroundGrass[i], 0.72f, 126);
        }

        CutePixelKit.RectObject(
            nearDepthLayer,
            "Near Meadow Edge",
            new Vector2(0f, -4.12f),
            new Vector2(16f, 0.34f),
            new Color(0.07f, 0.13f, 0.16f, 0.32f),
            130);

        BuildPolygonMeadowFacets();
    }

    private void CreatePolygonTree(Transform parent, string name, Vector2 position, float scale, int sortingOrder)
    {
        CreatePolygonObject(
            name + " Trunk",
            parent,
            position + new Vector2(0f, -0.42f * scale),
            new[]
            {
                new Vector2(-0.12f, -0.42f), new Vector2(0.14f, -0.42f),
                new Vector2(0.11f, 0.42f), new Vector2(-0.10f, 0.42f)
            },
            CutePixelKit.Hex("765042"),
            sortingOrder);
        GameObject canopy = CreatePolygonObject(
            name + " Canopy",
            parent,
            position + new Vector2(0f, 0.25f * scale),
            new[]
            {
                new Vector2(-0.74f, -0.10f), new Vector2(-0.48f, 0.62f),
                new Vector2(-0.08f, 0.98f), new Vector2(0.50f, 0.62f),
                new Vector2(0.78f, -0.08f), new Vector2(0.30f, -0.48f),
                new Vector2(-0.34f, -0.46f)
            },
            CutePixelKit.Hex("2D6A5B"),
            sortingOrder + 1);
        canopy.transform.localScale = Vector3.one * scale;
        GameObject canopyFacet = CreatePolygonObject(
            name + " Canopy Facet",
            parent,
            position + new Vector2(-0.16f * scale, 0.43f * scale),
            new[]
            {
                new Vector2(-0.42f, -0.12f), new Vector2(-0.07f, 0.64f),
                new Vector2(0.42f, -0.05f), new Vector2(0.08f, -0.30f)
            },
            CutePixelKit.Hex("63B36F"),
            sortingOrder + 2);
        canopyFacet.transform.localScale = Vector3.one * scale;
        CreatePolygonObject(
            name + " Ground Shadow",
            parent,
            position + new Vector2(0.08f, -0.82f * scale),
            new[]
            {
                new Vector2(-0.62f, -0.10f), new Vector2(-0.18f, -0.22f),
                new Vector2(0.70f, -0.06f), new Vector2(0.18f, 0.12f)
            },
            new Color(0.035f, 0.05f, 0.10f, 0.55f),
            sortingOrder - 1);
    }

    private void CreatePolygonGrassTuft(Transform parent, string name, Vector2 position, float scale, int sortingOrder)
    {
        GameObject grass = CreatePolygonObject(
            name,
            parent,
            position,
            new[]
            {
                new Vector2(-0.34f, -0.30f), new Vector2(-0.08f, 0.52f),
                new Vector2(0f, -0.05f), new Vector2(0.24f, 0.66f),
                new Vector2(0.34f, -0.30f)
            },
            CutePixelKit.Hex("72C47C"),
            sortingOrder);
        grass.transform.localScale = Vector3.one * scale;
    }

    private void BuildWorldPolygonFoliage()
    {
        int treeIndex = 0;
        for (int x = -36; x <= 36; x += 9)
        {
            float topY = 9.5f + Mathf.Sin(x * 0.31f) * 1.8f;
            float bottomY = -11.0f + Mathf.Cos(x * 0.27f) * 1.7f;
            CreatePolygonTree(
                midDepthLayer,
                "World Border Tree " + treeIndex++,
                new Vector2(x + Mathf.Sin(x * 0.17f) * 1.2f, topY),
                0.72f + Mathf.Abs(Mathf.Sin(x * 0.13f)) * 0.18f,
                -13);
            CreatePolygonTree(
                midDepthLayer,
                "World Border Tree " + treeIndex++,
                new Vector2(x + Mathf.Cos(x * 0.19f) * 1.0f, bottomY),
                0.74f + Mathf.Abs(Mathf.Cos(x * 0.11f)) * 0.16f,
                -13);
        }

        for (int y = -9; y <= 9; y += 9)
        {
            CreatePolygonTree(
                midDepthLayer,
                "World Side Tree " + treeIndex++,
                new Vector2(-23.5f + Mathf.Sin(y * 0.4f) * 1.2f, y),
                0.82f,
                -13);
            CreatePolygonTree(
                midDepthLayer,
                "World Side Tree " + treeIndex++,
                new Vector2(23.5f + Mathf.Cos(y * 0.4f) * 1.2f, y),
                0.84f,
                -13);
        }
    }

    private void BuildPolygonMeadowFacets()
    {
        // A few large, low-contrast facets sell the 2.5D ground plane without
        // covering the authored background or turning the arena into a flat UI.
        CreatePolygonObject(
            "Moonlit Ground Facet Left",
            midDepthLayer,
            new Vector2(-4.9f, -2.55f),
            new[]
            {
                new Vector2(-2.15f, -0.28f), new Vector2(-0.72f, -0.62f),
                new Vector2(1.15f, -0.24f), new Vector2(0.32f, 0.16f),
                new Vector2(-1.18f, 0.34f)
            },
            new Color(0.20f, 0.35f, 0.36f, 0.34f),
            -22);
        CreatePolygonObject(
            "Moonlit Ground Facet Right",
            midDepthLayer,
            new Vector2(4.85f, -2.65f),
            new[]
            {
                new Vector2(-1.18f, -0.30f), new Vector2(0.28f, -0.62f),
                new Vector2(2.10f, -0.12f), new Vector2(1.16f, 0.32f),
                new Vector2(-0.30f, 0.12f)
            },
            new Color(0.25f, 0.29f, 0.42f, 0.30f),
            -21);
        CreatePolygonObject(
            "Moonlit Ground Facet Center",
            midDepthLayer,
            new Vector2(0.2f, -3.18f),
            new[]
            {
                new Vector2(-2.42f, -0.12f), new Vector2(-0.58f, -0.46f),
                new Vector2(1.95f, -0.10f), new Vector2(0.58f, 0.42f),
                new Vector2(-1.18f, 0.30f)
            },
            new Color(0.16f, 0.30f, 0.32f, 0.28f),
            -20);

        Vector2[] canopyPoints =
        {
            new Vector2(-6.95f, 2.75f), new Vector2(6.95f, 2.75f),
            new Vector2(-6.95f, -0.85f), new Vector2(6.95f, -0.75f)
        };
        for (int i = 0; i < canopyPoints.Length; i++)
        {
            float side = canopyPoints[i].x < 0f ? -1f : 1f;
            CreatePolygonObject(
                "Faceted Canopy " + i,
                midDepthLayer,
                canopyPoints[i],
                new[]
                {
                    new Vector2(0f, -0.85f), new Vector2(side * 0.68f, -0.20f),
                    new Vector2(side * 0.48f, 0.72f), new Vector2(0f, 1.10f),
                    new Vector2(-side * 0.46f, 0.56f), new Vector2(-side * 0.70f, -0.18f)
                },
                i % 2 == 0
                    ? new Color(0.10f, 0.26f, 0.30f, 0.58f)
                    : new Color(0.18f, 0.24f, 0.37f, 0.54f),
                -14);
        }
    }

    private GameObject CreatePolygonObject(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2[] points,
        Color fill,
        int sortingOrder)
    {
        GameObject polygon = new GameObject(objectName);
        if (parent != null) polygon.transform.SetParent(parent, false);
        polygon.transform.localPosition = new Vector3(position.x, position.y, 0f);

        Mesh mesh = new Mesh { name = objectName + " Mesh" };
        Vector3[] vertices = new Vector3[points.Length + 1];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i + 1] = new Vector3(points[i].x, points[i].y, 0f);
        }
        int[] triangles = new int[points.Length * 3];
        for (int i = 0; i < points.Length; i++)
        {
            int triangle = i * 3;
            triangles[triangle] = 0;
            triangles[triangle + 1] = i + 1;
            triangles[triangle + 2] = (i + 1) % points.Length + 1;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        MeshFilter filter = polygon.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer meshRenderer = polygon.AddComponent<MeshRenderer>();
        meshRenderer.sortingOrder = sortingOrder;
        meshRenderer.receiveShadows = false;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            Material material = new Material(shader) { name = objectName + " Material" };
            material.color = fill;
            meshRenderer.sharedMaterial = material;
        }
        return polygon;
    }

    private LineRenderer CreatePolygonOutline(GameObject polygon, Vector2[] points, Color color, float width, int sortingOrder)
    {
        GameObject outlineObject = new GameObject(polygon.name + " Edge");
        outlineObject.transform.SetParent(polygon.transform, false);
        LineRenderer outline = outlineObject.AddComponent<LineRenderer>();
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.positionCount = points.Length;
        outline.widthMultiplier = width;
        outline.numCornerVertices = 0;
        outline.numCapVertices = 0;
        outline.startColor = color;
        outline.endColor = color;
        outline.sortingOrder = sortingOrder;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader != null) outline.material = new Material(shader) { color = color };
        for (int i = 0; i < points.Length; i++)
        {
            outline.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
        }
        return outline;
    }

    private void Build2PointFiveLighting()
    {
        moonGlowSprite = CreateRadialSprite(
            "Moon Volume Glow",
            new Color(0.46f, 0.66f, 1f, 0.28f),
            48);
        CutePixelKit.SpriteObject(
            farDepthLayer,
            "Moon Volume",
            moonGlowSprite,
            new Vector2(3.65f, 2.55f),
            2.2f,
            -44);

        lanternGlowSprite = CreateRadialSprite(
            "Courier Lantern Glow",
            new Color(1f, 0.65f, 0.30f, 0.30f),
            48);
        playerLanternGlow = CutePixelKit.SpriteObject(
            midDepthLayer,
            "Courier Lantern Bloom",
            lanternGlowSprite,
            PresentationSpawnPoint + new Vector2(0f, 0.32f),
            1.25f,
            -4);
    }

    private Sprite CreateRadialSprite(string name, Color color, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = name + " Texture";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float stepped = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.7f);
                stepped = Mathf.Round(stepped * 6f) / 6f;
                float alpha = stepped * color.a;
                if (alpha > 0f && alpha < color.a * 0.5f && ((x + y) & 1) == 1) alpha *= 0.55f;
                pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size * 0.5f,
            0,
            SpriteMeshType.FullRect);
        sprite.name = name;
        return sprite;
    }

    private void UpdateTwoPointFiveSpace()
    {
        if (simulation == null) return;
        GameObject playerObject = GetValue("player") as GameObject;
        if (playerObject == null) return;

        Vector3 playerPosition = playerObject.transform.position;
        Camera sceneCamera = Camera.main;
        if (sceneCamera != null)
        {
            Vector3 cameraTarget = new Vector3(playerPosition.x, playerPosition.y, sceneCamera.transform.position.z);
            float cameraBlend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f);
            sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, cameraTarget, cameraBlend);
        }

        float horizontal = Mathf.Clamp(playerPosition.x / WorldHalfWidth, -1f, 1f);
        float vertical = Mathf.Clamp(playerPosition.y / WorldHalfHeight, -1f, 1f);
        Vector3 parallax = new Vector3(-horizontal * 0.18f, -vertical * 0.08f, 0f);
        float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 8f);
        if (farDepthLayer != null) farDepthLayer.localPosition = Vector3.Lerp(farDepthLayer.localPosition, parallax * 0.24f, blend);
        if (midDepthLayer != null) midDepthLayer.localPosition = Vector3.Lerp(midDepthLayer.localPosition, parallax * 0.52f, blend);
        if (nearDepthLayer != null) nearDepthLayer.localPosition = Vector3.Lerp(nearDepthLayer.localPosition, parallax, blend);

        if (playerLanternGlow != null)
        {
            playerLanternGlow.transform.position = playerPosition + new Vector3(0f, 0.32f, 0f);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.035f;
            playerLanternGlow.transform.localScale = Vector3.one * (1.25f * pulse);
        }
    }

    private void ApplySkinToWorld(bool hideLegacyField)
    {
        Sprite activeHeroSprite = GetHeroPresentationSprite();
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null) continue;
            string objectName = renderer.gameObject.name;

            if (objectName.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Legacy pixel shadows are replaced by the polygon grounding
                // shadows created for the actor rigs below.
                renderer.enabled = false;
                continue;
            }

            if (hideLegacyField &&
                (objectName.StartsWith("Nightfall Arena") || objectName.StartsWith("Arena Surface") ||
                 objectName.StartsWith("Grid ") || objectName.StartsWith("Border ") || objectName.StartsWith("Signal Dust")))
            {
                renderer.enabled = false;
                continue;
            }

            if (objectName == "Night Courier")
            {
                renderer.sprite = activeHeroSprite;
                renderer.flipX = heroFacingLeft;
            }
            else if (objectName == "Red Drone") renderer.sprite = slimeSprite;
            else if (objectName == "Brute Drone") renderer.sprite = hornSprite;
            else if (objectName == "Wool Sprite") renderer.sprite = woolSprite;
            else if (objectName == "Lantern Moth") renderer.sprite = mothSprite;
            else if (objectName == "Mushroom Thief") renderer.sprite = mushroomSprite;
            else if (objectName == "Hedge Witch") renderer.sprite = witchSprite;
            else if (objectName == "Mallow Warden") renderer.sprite = bossSprite;
            else if (objectName == "Spark Bolt") renderer.sprite = sparkSprite;
            else if (objectName == "Cinder Bolt") renderer.sprite = emberSprite;
            else if (objectName == "Hearth Note") renderer.sprite = noteSprite;
            else if (objectName == "Berry Toss") renderer.sprite = berrySprite;
            else if (objectName == "Sewing Needle") renderer.sprite = needleSprite;
            else if (objectName == "Curse Seed") renderer.sprite = curseSeedSprite;
            else if (objectName == "Boss Orb") renderer.sprite = bossOrbSprite;
            else if (objectName == "Signal Shard") renderer.sprite = shardSprite;
            else if (objectName == "Night Chest") renderer.sprite = chestSprite;
            else if (objectName == "Ember Ring") renderer.sprite = orbitSprite;
            else if (objectName == "Firefly Jar") renderer.sprite = orbitSprite;
            else if (objectName == "Hit Spark") renderer.sprite = hitSparkSprite;
            else if (objectName == "Boss Burst") renderer.sprite = bossBurstSprite;
            else if (objectName == "Charge Telegraph") renderer.sprite = telegraphSprite;

            bool isActor = objectName == "Night Courier" || objectName == "Red Drone" || objectName == "Brute Drone" ||
                           objectName == "Wool Sprite" || objectName == "Lantern Moth" || objectName == "Mushroom Thief" ||
                           objectName == "Hedge Witch" || objectName == "Mallow Warden";
            if (isActor)
            {
                ApplyActorDepth(renderer);
                ApplyPolygonActorSkin(renderer);
                // The scene actors are now mesh-built polygon characters. The
                // original pixel sprites stay available for the HUD portrait,
                // but are not rendered in the playfield.
                renderer.enabled = false;
            }
        }
    }

    private void ApplyActorDepth(SpriteRenderer renderer)
    {
        GameObject actorObject = renderer.gameObject;
        Vector3 baseScale;
        if (!actorBaseScales.TryGetValue(actorObject, out baseScale))
        {
            baseScale = renderer.transform.localScale;
            actorBaseScales[actorObject] = baseScale;
        }

        float depth = Mathf.InverseLerp(4.05f, -4.05f, renderer.transform.position.y);
        float depthScale = Mathf.Lerp(0.92f, 1.10f, depth);
        renderer.transform.localScale = baseScale * depthScale;
        renderer.sortingOrder = 32 + Mathf.RoundToInt(depth * 70f);

        if (actorShadowSprite == null) return;
        GameObject shadow = GetOrCreateActorShadow(actorObject, renderer.transform.parent);
        if (shadow == null) return;

        shadow.transform.position = renderer.transform.position + new Vector3(0f, -0.22f, 0f);
        shadow.transform.localScale = new Vector3(1.18f * depthScale, 0.56f * depthScale, 1f);
        Renderer shadowRenderer = shadow.GetComponent<Renderer>();
        if (shadowRenderer != null) shadowRenderer.sortingOrder = renderer.sortingOrder - 1;
    }

    private void ApplyPolygonActorSkin(SpriteRenderer renderer)
    {
        GameObject actorObject = renderer.gameObject;
        PolygonActorRig rig;
        if (!polygonActorRigs.TryGetValue(actorObject, out rig) || rig == null)
        {
            rig = BuildPolygonActorRig(actorObject.name, actorObject.transform);
            polygonActorRigs[actorObject] = rig;
            polygonLastPositions[actorObject] = actorObject.transform.position;
        }

        int baseOrder = renderer.sortingOrder;
        if (rig.Silhouette != null)
        {
            MeshRenderer silhouette = rig.Silhouette.GetComponent<MeshRenderer>();
            if (silhouette != null) silhouette.sortingOrder = baseOrder - 3;
        }
        if (rig.Facet != null)
        {
            MeshRenderer facet = rig.Facet.GetComponent<MeshRenderer>();
            if (facet != null) facet.sortingOrder = baseOrder - 2;
        }
        if (rig.Accent != null)
        {
            MeshRenderer accent = rig.Accent.GetComponent<MeshRenderer>();
            if (accent != null) accent.sortingOrder = baseOrder - 1;
        }
        if (rig.Outline != null) rig.Outline.sortingOrder = baseOrder - 2;
        if (rig.DepthPlate != null)
        {
            MeshRenderer depthPlate = rig.DepthPlate.GetComponent<MeshRenderer>();
            if (depthPlate != null) depthPlate.sortingOrder = baseOrder - 4;
        }
        for (int i = 0; i < rig.Details.Count; i++)
        {
            if (rig.Details[i] == null) continue;
            MeshRenderer detail = rig.Details[i].GetComponent<MeshRenderer>();
            if (detail != null) detail.sortingOrder = baseOrder - 1;
        }
    }

    private PolygonActorRig BuildPolygonActorRig(string actorName, Transform actorTransform)
    {
        Vector2[] silhouettePoints;
        Vector2[] facetPoints;
        Vector2[] accentPoints = null;
        Vector2 accentPosition = Vector2.zero;
        Color silhouetteColor;
        Color facetColor;
        Color outlineColor = CutePixelKit.MascotOutline;
        bool winged = false;

        switch (actorName)
        {
            case "Night Courier":
                silhouettePoints = new[]
                {
                    new Vector2(-0.38f, -0.48f), new Vector2(-0.55f, 0.04f),
                    new Vector2(-0.28f, 0.47f), new Vector2(0.25f, 0.45f),
                    new Vector2(0.55f, 0.05f), new Vector2(0.36f, -0.46f),
                    new Vector2(0f, -0.59f)
                };
                facetPoints = new[]
                {
                    new Vector2(-0.23f, -0.34f), new Vector2(0.03f, 0.34f),
                    new Vector2(0.30f, -0.25f)
                };
                accentPoints = new[]
                {
                    new Vector2(0f, 0.16f), new Vector2(0.12f, 0f),
                    new Vector2(0f, -0.16f), new Vector2(-0.12f, 0f)
                };
                accentPosition = new Vector2(0.35f, 0.16f);
                silhouetteColor = CutePixelKit.Hex("655389");
                facetColor = CutePixelKit.MascotMint;
                outlineColor = CutePixelKit.Hex("30243F");
                break;
            case "Brute Drone":
            case "Mallow Warden":
                silhouettePoints = actorName == "Mallow Warden"
                    ? new[]
                    {
                        new Vector2(-0.78f, -0.56f), new Vector2(-0.92f, 0.10f),
                        new Vector2(-0.42f, 0.70f), new Vector2(0f, 0.88f),
                        new Vector2(0.43f, 0.70f), new Vector2(0.92f, 0.10f),
                        new Vector2(0.76f, -0.58f), new Vector2(0f, -0.80f)
                    }
                    : new[]
                    {
                        new Vector2(-0.50f, -0.34f), new Vector2(-0.57f, 0.16f),
                        new Vector2(-0.25f, 0.56f), new Vector2(0.25f, 0.56f),
                        new Vector2(0.59f, 0.12f), new Vector2(0.40f, -0.44f),
                        new Vector2(0f, -0.60f)
                    };
                facetPoints = actorName == "Mallow Warden"
                    ? new[]
                    {
                        new Vector2(-0.40f, 0.18f), new Vector2(0f, 0.70f),
                        new Vector2(0.44f, 0.18f), new Vector2(0f, -0.18f)
                    }
                    : new[]
                    {
                        new Vector2(-0.28f, 0.22f), new Vector2(0f, 0.48f),
                        new Vector2(0.30f, 0.12f), new Vector2(0f, -0.16f)
                    };
                accentPoints = new[]
                {
                    new Vector2(0f, 0.16f), new Vector2(0.13f, 0f),
                    new Vector2(0f, -0.16f), new Vector2(-0.13f, 0f)
                };
                accentPosition = actorName == "Mallow Warden" ? new Vector2(0f, 0.72f) : new Vector2(0.34f, 0.28f);
                silhouetteColor = actorName == "Mallow Warden" ? CutePixelKit.Hex("5A3E70") : CutePixelKit.Hex("A96255");
                facetColor = actorName == "Mallow Warden" ? CutePixelKit.MascotGold : CutePixelKit.MascotGold;
                outlineColor = actorName == "Mallow Warden" ? CutePixelKit.Hex("2B203C") : CutePixelKit.MascotOutline;
                break;
            case "Lantern Moth":
                silhouettePoints = new[]
                {
                    new Vector2(-0.56f, 0.18f), new Vector2(-0.30f, 0.50f),
                    new Vector2(0f, 0.18f), new Vector2(0.30f, 0.50f),
                    new Vector2(0.56f, 0.18f), new Vector2(0.24f, -0.12f),
                    new Vector2(0f, -0.47f), new Vector2(-0.24f, -0.12f)
                };
                facetPoints = new[]
                {
                    new Vector2(-0.16f, -0.26f), new Vector2(0f, 0.34f),
                    new Vector2(0.16f, -0.26f), new Vector2(0f, -0.42f)
                };
                silhouetteColor = CutePixelKit.MascotLilac;
                facetColor = CutePixelKit.MascotGold;
                outlineColor = CutePixelKit.Hex("49385F");
                winged = true;
                break;
            case "Hedge Witch":
                silhouettePoints = new[]
                {
                    new Vector2(-0.45f, -0.40f), new Vector2(-0.55f, 0.16f),
                    new Vector2(0f, 0.68f), new Vector2(0.55f, 0.16f),
                    new Vector2(0.43f, -0.42f), new Vector2(0f, -0.56f)
                };
                facetPoints = new[]
                {
                    new Vector2(-0.30f, -0.20f), new Vector2(0f, 0.48f),
                    new Vector2(0.30f, -0.20f), new Vector2(0f, -0.38f)
                };
                silhouetteColor = CutePixelKit.MascotLilac;
                facetColor = CutePixelKit.MascotPink;
                outlineColor = CutePixelKit.Hex("4A385C");
                break;
            case "Mushroom Thief":
                silhouettePoints = new[]
                {
                    new Vector2(-0.50f, 0.02f), new Vector2(-0.34f, 0.40f),
                    new Vector2(0f, 0.53f), new Vector2(0.36f, 0.40f),
                    new Vector2(0.52f, 0.02f), new Vector2(0.26f, -0.08f),
                    new Vector2(-0.28f, -0.08f)
                };
                facetPoints = new[]
                {
                    new Vector2(-0.22f, -0.06f), new Vector2(0f, 0.30f),
                    new Vector2(0.24f, -0.06f), new Vector2(0.14f, -0.40f),
                    new Vector2(-0.14f, -0.40f)
                };
                silhouetteColor = CutePixelKit.MascotBlush;
                facetColor = CutePixelKit.MascotCream;
                outlineColor = CutePixelKit.Hex("5B334A");
                break;
            case "Wool Sprite":
                silhouettePoints = new[]
                {
                    new Vector2(-0.40f, -0.34f), new Vector2(-0.49f, 0.10f),
                    new Vector2(-0.22f, 0.43f), new Vector2(0.20f, 0.46f),
                    new Vector2(0.48f, 0.12f), new Vector2(0.34f, -0.35f),
                    new Vector2(0f, -0.52f)
                };
                facetPoints = new[]
                {
                    new Vector2(-0.23f, 0.10f), new Vector2(0f, 0.34f),
                    new Vector2(0.25f, 0.08f), new Vector2(0f, -0.18f)
                };
                silhouetteColor = CutePixelKit.MascotMint;
                facetColor = CutePixelKit.MascotCream;
                outlineColor = CutePixelKit.Hex("315C5B");
                break;
            default:
                silhouettePoints = new[]
                {
                    new Vector2(-0.35f, -0.28f), new Vector2(-0.42f, 0.10f),
                    new Vector2(-0.18f, 0.35f), new Vector2(0.20f, 0.35f),
                    new Vector2(0.42f, 0.10f), new Vector2(0.32f, -0.30f),
                    new Vector2(0f, -0.44f)
                };
                facetPoints = new[]
                {
                    new Vector2(-0.22f, 0.10f), new Vector2(0f, 0.27f),
                    new Vector2(0.23f, 0.08f), new Vector2(0f, -0.15f)
                };
                silhouetteColor = CutePixelKit.MascotPink;
                facetColor = CutePixelKit.MascotBlush;
                break;
        }

        GameObject root = new GameObject(actorName + " Polygon Rig");
        root.transform.SetParent(actorTransform, false);
        root.transform.localPosition = new Vector3(0f, 0.02f, 0.14f);
        PolygonActorRig rig = new PolygonActorRig
        {
            Root = root,
            Phase = Mathf.Abs(actorName.GetHashCode() % 1000) * 0.01f,
            Winged = winged
        };
        Color depthColor = new Color(
            silhouetteColor.r * 0.52f,
            silhouetteColor.g * 0.52f,
            silhouetteColor.b * 0.62f,
            1f);
        rig.DepthPlate = CreatePolygonObject(
            actorName + " Polygon Depth Plate",
            root.transform,
            new Vector2(0.085f, -0.105f),
            silhouettePoints,
            depthColor,
            -1);
        rig.Silhouette = CreatePolygonObject(actorName + " Polygon Silhouette", root.transform, Vector2.zero, silhouettePoints, silhouetteColor, 0);
        rig.Facet = CreatePolygonObject(actorName + " Polygon Facet", root.transform, Vector2.zero, facetPoints, facetColor, 1);
        rig.Outline = CreatePolygonOutline(rig.Silhouette, silhouettePoints, outlineColor, actorName == "Mallow Warden" ? 0.045f : 0.032f, 2);
        if (accentPoints != null)
        {
            rig.Accent = CreatePolygonObject(actorName + " Polygon Accent", root.transform, accentPosition, accentPoints, CutePixelKit.MascotGold, 3);
        }
        AddPolygonFaceDetails(actorName, rig, root.transform, outlineColor);
        return rig;
    }

    private void AddPolygonFaceDetails(string actorName, PolygonActorRig rig, Transform root, Color outlineColor)
    {
        Vector2[] eye =
        {
            new Vector2(-0.045f, -0.06f), new Vector2(0f, 0.035f),
            new Vector2(0.045f, -0.06f), new Vector2(0f, -0.105f)
        };
        float eyeX = actorName == "Mallow Warden" ? 0.24f : actorName == "Brute Drone" ? 0.16f : 0.105f;
        float eyeY = actorName == "Mallow Warden" ? 0.22f : actorName == "Hedge Witch" ? 0.05f : 0.10f;
        Color eyeColor = actorName == "Mallow Warden" ? CutePixelKit.MascotGold : outlineColor;
        rig.Details.Add(CreatePolygonObject(actorName + " Polygon Eye L", root, new Vector2(-eyeX, eyeY), eye, eyeColor, 4));
        rig.Details.Add(CreatePolygonObject(actorName + " Polygon Eye R", root, new Vector2(eyeX, eyeY), eye, eyeColor, 4));

        if (actorName == "Lantern Moth")
        {
            Vector2[] antenna =
            {
                new Vector2(-0.025f, -0.08f), new Vector2(0f, 0.38f),
                new Vector2(0.025f, -0.08f)
            };
            rig.Details.Add(CreatePolygonObject(actorName + " Polygon Antenna", root, new Vector2(0f, 0.27f), antenna, outlineColor, 4));
        }
        else if (actorName == "Mushroom Thief")
        {
            Vector2[] stem =
            {
                new Vector2(-0.10f, -0.23f), new Vector2(0.10f, -0.23f),
                new Vector2(0.07f, 0.04f), new Vector2(-0.08f, 0.04f)
            };
            rig.Details.Add(CreatePolygonObject(actorName + " Polygon Stem", root, new Vector2(0f, -0.08f), stem, CutePixelKit.MascotCream, 4));
        }
    }

    private void UpdatePolygonActorMotion()
    {
        if (polygonActorRigs.Count == 0) return;
        float dt = Mathf.Max(0.001f, Time.unscaledDeltaTime);
        float now = Time.unscaledTime;
        foreach (KeyValuePair<GameObject, PolygonActorRig> pair in polygonActorRigs)
        {
            GameObject actorObject = pair.Key;
            PolygonActorRig rig = pair.Value;
            if (actorObject == null || rig == null || rig.Root == null) continue;

            Vector3 currentPosition = actorObject.transform.position;
            Vector3 lastPosition;
            if (!polygonLastPositions.TryGetValue(actorObject, out lastPosition)) lastPosition = currentPosition;
            Vector3 delta = currentPosition - lastPosition;
            polygonLastPositions[actorObject] = currentPosition;
            float speed = delta.magnitude / dt;
            float motion = Mathf.Clamp01(speed / 3.8f);
            float wave = Mathf.Sin(now * (4.2f + motion * 3.8f) + rig.Phase);

            rig.Root.transform.localPosition = new Vector3(0f, 0.02f + wave * (0.012f + motion * 0.045f), 0.14f);
            float lean = Mathf.Clamp(delta.x / dt * 5.5f, -13f, 13f);
            float yaw = Mathf.Sin(now * 1.35f + rig.Phase) * (rig.Winged ? 10f : 4f);
            rig.Root.transform.localRotation = Quaternion.Euler(yaw, 0f, lean + wave * (1.5f + motion * 4f));
            rig.Root.transform.localScale = new Vector3(1f + motion * 0.055f, 1f - motion * 0.045f, 1f);

            if (rig.Facet != null)
            {
                rig.Facet.transform.localRotation = Quaternion.Euler(0f, 0f, wave * (2f + motion * 10f));
            }
            if (rig.Accent != null)
            {
                float accentPulse = 1f + Mathf.Sin(now * 5.5f + rig.Phase * 1.7f) * (0.06f + motion * 0.08f);
                rig.Accent.transform.localScale = Vector3.one * accentPulse;
            }
            if (rig.Winged && rig.Silhouette != null)
            {
                float wingBeat = 1f + Mathf.Sin(now * 8.5f + rig.Phase) * 0.12f;
                rig.Silhouette.transform.localScale = new Vector3(wingBeat, 1f, 1f);
            }
        }
    }

    private GameObject GetOrCreateActorShadow(GameObject actorObject, Transform parent)
    {
        GameObject shadow;
        if (actorShadowObjects.TryGetValue(actorObject, out shadow) && shadow != null) return shadow;

        shadow = CreatePolygonObject(
            actorObject.name + " Polygon Ground Shadow",
            parent,
            actorObject.transform.position + new Vector3(0f, -0.22f, 0f),
            new[]
            {
                new Vector2(-0.66f, 0f), new Vector2(-0.34f, 0.13f),
                new Vector2(0.34f, 0.13f), new Vector2(0.66f, 0f),
                new Vector2(0.28f, -0.13f), new Vector2(-0.32f, -0.13f)
            },
            new Color(0.035f, 0.025f, 0.07f, 0.58f),
            0);
        actorShadowObjects[actorObject] = shadow;
        return shadow;
    }

    private Sprite GetHeroPresentationSprite()
    {
        if (heroFrames == null || heroFrames.Length == 0) return heroSprite;

        GameObject playerObject = GetValue("player") as GameObject;
        if (playerObject == null) return heroFrames[0];

        Vector3 currentPosition = playerObject.transform.position;
        if (!heroPositionKnown)
        {
            lastHeroPosition = currentPosition;
            heroPositionKnown = true;
        }

        Vector3 delta = currentPosition - lastHeroPosition;
        bool moving = GetMode() == "Playing" && delta.sqrMagnitude > 0.000001f;
        if (moving)
        {
            heroWalkTime += Time.unscaledDeltaTime * 10f;
            if (Mathf.Abs(delta.x) > 0.001f) heroFacingLeft = delta.x < 0f;
        }
        else
        {
            heroWalkTime = 0f;
        }
        lastHeroPosition = currentPosition;

        int frameIndex = moving ? Mathf.FloorToInt(heroWalkTime) % heroFrames.Length : 0;
        return heroFrames[frameIndex] != null ? heroFrames[frameIndex] : heroFrames[0];
    }

    private void BuildStyles()
    {
        if (stylesBuilt) return;
        stylesBuilt = true;
        Font font = CutePixelKit.FriendlyFont;

        titleStyle = MakeStyle(font, 30, CutePixelKit.Hex("5D3D58"), FontStyle.Bold, TextAnchor.MiddleCenter, true);
        headingStyle = MakeStyle(font, 17, CutePixelKit.Cream, FontStyle.Bold, TextAnchor.MiddleLeft, false);
        bodyStyle = MakeStyle(font, 14, CutePixelKit.Hex("644656"), FontStyle.Normal, TextAnchor.UpperLeft, true);
        tinyStyle = MakeStyle(font, 11, CutePixelKit.Cream, FontStyle.Bold, TextAnchor.MiddleLeft, false);
        centeredStyle = MakeStyle(font, 13, CutePixelKit.Cream, FontStyle.Bold, TextAnchor.MiddleCenter, true);
        cardTitleStyle = MakeStyle(font, 18, CutePixelKit.MascotOutline, FontStyle.Bold, TextAnchor.UpperLeft, true);
        cardBodyStyle = MakeStyle(font, 13, CutePixelKit.Hex("6D5260"), FontStyle.Normal, TextAnchor.UpperLeft, true);

        parchmentPanelStyle = MakePanelStyle(parchmentPanel, 10);
        darkPanelStyle = MakePanelStyle(darkPanel, 8);
        timerPanelStyle = MakePanelStyle(woodPanel, 8);
        slotPanelStyle = MakePanelStyle(slotPanelTexture, 8);

        cardNormalStyle = MakeCardStyle(font, cardPanelTexture, cardMintTexture, cardCoralTexture);
        cardMintStyle = MakeCardStyle(font, cardMintTexture, cardPanelTexture, cardCoralTexture);
        cardCoralStyle = MakeCardStyle(font, cardCoralTexture, cardMintTexture, cardPanelTexture);
        buttonStyle = cardNormalStyle;
    }

    private static GUIStyle MakeStyle(Font font, int size, Color color, FontStyle style, TextAnchor anchor, bool wrap)
    {
        return new GUIStyle
        {
            font = font,
            fontSize = size,
            fontStyle = style,
            normal = { textColor = color },
            alignment = anchor,
            wordWrap = wrap,
            richText = true
        };
    }

    private static GUIStyle MakePanelStyle(Texture2D background, int border)
    {
        return new GUIStyle
        {
            normal = { background = background },
            border = new RectOffset(border, border, border, border),
            padding = new RectOffset(0, 0, 0, 0),
            stretchWidth = true,
            stretchHeight = true
        };
    }

    private static GUIStyle MakeCardStyle(Font font, Texture2D normal, Texture2D hover, Texture2D active)
    {
        return new GUIStyle
        {
            normal = { background = normal, textColor = CutePixelKit.Hex("4B3042") },
            hover = { background = hover, textColor = CutePixelKit.Hex("4B3042") },
            active = { background = active, textColor = CutePixelKit.Hex("4B3042") },
            focused = { background = hover, textColor = CutePixelKit.Hex("4B3042") },
            font = font,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(18, 18, 18, 18),
            padding = new RectOffset(14, 14, 12, 12)
        };
    }

    private static void DrawPanel(Rect rect, GUIStyle style)
    {
        if (style == null || style.normal.background == null) return;
        GUI.Box(rect, GUIContent.none, style);
    }

    private void OnGUI()
    {
        if (!attached) return;
        BuildStyles();

        float scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
        float offsetX = (Screen.width - ReferenceWidth * scale) * 0.5f;
        float offsetY = (Screen.height - ReferenceHeight * scale) * 0.5f;
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

        DrawCompactHud();

        string mode = GetMode();
        if (mode == "Menu") DrawMenu();
        else if (mode == "LevelUp") DrawLevelUp();
        else if (mode == "Paused") DrawStateCard("A little rest", "The meadow is waiting.", "Press P to continue");
        else if (mode == "Won") DrawStateCard("Dawn found you", "You carried the light through the whole night.", "Press R for another walk");
        else if (mode == "Lost") DrawStateCard("The lantern went quiet", "Every small run leaves a story behind.", "Press R to try again");

        float toastTimer = GetFloat("toastTimer");
        if (toastTimer > 0f)
        {
            DrawPanel(new Rect(300f, 488f, 360f, 42f), timerPanelStyle);
            GUI.Label(new Rect(316f, 496f, 328f, 26f), Humanize(GetString("toastMessage")), centeredStyle);
        }

        GUI.matrix = previous;
    }

    private void DrawCompactHud()
    {
        float health = GetFloat("playerHealth");
        float maxHealth = Mathf.Max(1f, GetInt("maxHealth"));
        int xp = GetInt("xp");
        int xpToNext = Mathf.Max(1, GetInt("xpToNext"));
        int level = GetInt("level");
        int kills = GetInt("kills");
        int chests = GetInt("chestsOpened");
        float elapsed = GetFloat("elapsed");
        float pulse = GetFloat("pulseEnergy");

        DrawPanel(new Rect(18f, 16f, 252f, 72f), darkPanelStyle);
        DrawSprite(portraitSprite, new Rect(27f, 22f, 54f, 58f));
        GUI.Label(new Rect(88f, 22f, 170f, 22f), "Meadow Courier", headingStyle);
        DrawBar(new Rect(88f, 48f, 158f, 12f), health / maxHealth, healthFill);
        GUI.Label(new Rect(88f, 62f, 170f, 18f), Mathf.CeilToInt(health) + " / " + Mathf.CeilToInt(maxHealth) + " hearts", tinyStyle);

        DrawPanel(new Rect(405f, 16f, 150f, 46f), timerPanelStyle);
        GUI.Label(new Rect(417f, 21f, 126f, 28f), CutePixelKit.FormatTime(elapsed), centeredStyle);
        GUI.Label(new Rect(417f, 43f, 126f, 14f), "until dawn", MakeStyle(CutePixelKit.FriendlyFont, 9, CutePixelKit.Paper, FontStyle.Normal, TextAnchor.MiddleCenter, false));

        DrawPanel(new Rect(708f, 16f, 234f, 46f), darkPanelStyle);
        GUI.Label(new Rect(720f, 22f, 210f, 18f), "Lv. " + level + "   •   " + kills + " little foes", tinyStyle);
        GUI.Label(new Rect(720f, 42f, 210f, 14f), chests + " story chests found", MakeStyle(CutePixelKit.FriendlyFont, 9, CutePixelKit.Paper, FontStyle.Normal, TextAnchor.MiddleLeft, false));

        if (GetBool("bossActive"))
        {
            float bossHealth = GetFloat("bossHealth");
            float bossMaxHealth = Mathf.Max(1f, GetFloat("bossMaxHealth"));
            DrawPanel(new Rect(280f, 68f, 400f, 46f), darkPanelStyle);
            GUI.Label(new Rect(296f, 71f, 368f, 16f), GetString("bossDisplayName"), MakeStyle(CutePixelKit.FriendlyFont, 11, CutePixelKit.MascotPink, FontStyle.Bold, TextAnchor.MiddleCenter, false));
            DrawBar(new Rect(302f, 92f, 356f, 10f), bossHealth / bossMaxHealth, healthFill);
        }
        else if (GetFloat("bossWarningTimer") > 0f)
        {
            DrawPanel(new Rect(302f, 68f, 356f, 32f), timerPanelStyle);
            GUI.Label(new Rect(314f, 73f, 332f, 20f), "A LARGE SHADOW IS LISTENING", MakeStyle(CutePixelKit.FriendlyFont, 10, CutePixelKit.MascotPink, FontStyle.Bold, TextAnchor.MiddleCenter, false));
        }

        DrawLoadout();

        DrawBar(new Rect(18f, 516f, 924f, 9f), xp / (float)xpToNext, xpFill);
        GUI.Label(new Rect(18f, 492f, 300f, 20f), "Moon dew  " + xp + " / " + xpToNext, tinyStyle);
        GUI.Label(new Rect(720f, 492f, 222f, 20f), "Pulse  " + Mathf.RoundToInt(pulse) + "%", MakeStyle(CutePixelKit.FriendlyFont, 11, CutePixelKit.Cream, FontStyle.Bold, TextAnchor.MiddleRight, false));
        DrawBar(new Rect(822f, 483f, 120f, 7f), pulse / 100f, pulseFill);
    }

    private void DrawLoadout()
    {
        bool ring = GetBool("hasEmberRing");
        bool evolved = GetBool("cinderVolley");
        bool notes = GetBool("hasHearthNotes");
        bool berry = GetBool("hasBerryBasket");
        bool needle = GetBool("hasSewingNeedle");
        bool firefly = GetBool("hasFireflyJar");
        int wandLevel = GetInt("wandLevel");
        float magnet = GetFloat("magnetRange");
        float move = GetFloat("moveSpeed");
        int maxHealth = GetInt("maxHealth");

        float x = 321f;
        const float y = 465f;
        DrawSlot(new Rect(x, y, 48f, 48f), wandIcon, evolved ? "E" : Mathf.Max(1, wandLevel).ToString());
        DrawSlot(new Rect(x + 54f, y, 48f, 48f), notes ? notesIcon : ring ? ringIcon : null, notes ? "" : ring ? "1" : "");
        DrawSlot(new Rect(x + 108f, y, 48f, 48f), berry ? berryIcon : magnet > 1.5f ? magnetIcon : null, berry ? "" : magnet > 1.5f ? "+" : "");
        DrawSlot(new Rect(x + 162f, y, 48f, 48f), needle ? needleIcon : maxHealth > 180 ? heartIcon : null, needle ? "" : maxHealth > 180 ? "+" : "");
        DrawSlot(new Rect(x + 216f, y, 48f, 48f), firefly ? fireflyIcon : move > 4.2f ? bootIcon : null, firefly ? "" : move > 4.2f ? "+" : "");
        DrawSlot(new Rect(x + 270f, y, 48f, 48f), pulseIcon != null ? pulseIcon : orbitSprite, "");
    }

    private void DrawSlot(Rect rect, Sprite icon, string badge)
    {
        DrawPanel(rect, slotPanelStyle);
        if (icon != null) DrawSprite(icon, new Rect(rect.x + 8f, rect.y + 7f, 32f, 32f));
        if (!string.IsNullOrEmpty(badge))
        {
            GUI.Label(new Rect(rect.x + 29f, rect.y + 28f, 16f, 14f), badge, MakeStyle(CutePixelKit.FriendlyFont, 9, CutePixelKit.Gold, FontStyle.Bold, TextAnchor.MiddleCenter, false));
        }
    }

    private void DrawMenu()
    {
        GUI.DrawTexture(new Rect(0f, 0f, ReferenceWidth, ReferenceHeight), veil, ScaleMode.StretchToFill);
        DrawPanel(new Rect(190f, 112f, 580f, 350f), parchmentPanelStyle);
        GUI.Label(new Rect(235f, 142f, 490f, 54f), "Nightfall Meadow", titleStyle);
        GUI.Label(new Rect(250f, 201f, 460f, 56f), "A tiny lantern. A soft field.\nSomething small is coming closer.", bodyStyle);
        GUI.Label(new Rect(270f, 276f, 420f, 56f), "Move with WASD or arrow keys.\nYour little friend helps by itself.", MakeStyle(CutePixelKit.FriendlyFont, 14, CutePixelKit.Hex("644656"), FontStyle.Bold, TextAnchor.MiddleCenter, true));
        GUI.Label(new Rect(270f, 344f, 420f, 25f), "Space sends a little comfort pulse", MakeStyle(CutePixelKit.FriendlyFont, 12, CutePixelKit.Hex("8B6572"), FontStyle.Normal, TextAnchor.MiddleCenter, false));
        GUI.Label(new Rect(270f, 389f, 420f, 32f), "Press Enter to visit the meadow", MakeStyle(CutePixelKit.FriendlyFont, 15, CutePixelKit.Hex("5D3D58"), FontStyle.Bold, TextAnchor.MiddleCenter, false));
    }

    private void DrawLevelUp()
    {
        GUI.DrawTexture(new Rect(0f, 0f, ReferenceWidth, ReferenceHeight), veil, ScaleMode.StretchToFill);
        GUI.Label(new Rect(180f, 74f, 600f, 46f), "Choose a tiny blessing", MakeStyle(CutePixelKit.FriendlyFont, 25, CutePixelKit.Cream, FontStyle.Bold, TextAnchor.MiddleCenter, false));
        GUI.Label(new Rect(220f, 113f, 520f, 24f), "The night can wait while you decide", centeredStyle);

        IList choices = GetList("upgradeChoices");
        for (int i = 0; i < 3; i++)
        {
            Rect card = new Rect(85f + i * 270f, 160f, 250f, 310f);
            object choice = choices != null && i < choices.Count ? choices[i] : null;
            string tag = ReadChoice(choice, "Tag");
            string title = Humanize(ReadChoice(choice, "Title"));
            string description = Humanize(ReadChoice(choice, "Description"));

            if (GUI.Button(card, GUIContent.none, StyleForChoice(tag))) InvokeUpgrade(i);
            GUI.Label(new Rect(card.x + 20f, card.y + 18f, card.width - 40f, 20f), (i + 1) + "   " + Humanize(tag), MakeStyle(CutePixelKit.FriendlyFont, 10, CutePixelKit.Hex("A0614D"), FontStyle.Bold, TextAnchor.MiddleLeft, false));
            DrawSprite(IconForChoice(title, tag), new Rect(card.x + 91f, card.y + 50f, 68f, 68f));
            GUI.Label(new Rect(card.x + 20f, card.y + 132f, card.width - 40f, 60f), title, cardTitleStyle);
            GUI.Label(new Rect(card.x + 20f, card.y + 202f, card.width - 40f, 74f), description, cardBodyStyle);
            GUI.Label(new Rect(card.x + 20f, card.y + 278f, card.width - 40f, 20f), "Choose " + (i + 1), MakeStyle(CutePixelKit.FriendlyFont, 11, CutePixelKit.Hex("815044"), FontStyle.Bold, TextAnchor.MiddleCenter, false));
        }
    }

    private Sprite IconForChoice(string title, string tag)
    {
        string key = (title + " " + tag).ToLowerInvariant();
        if (key.Contains("hearth") || key.Contains("notes")) return notesIcon;
        if (key.Contains("berry") || key.Contains("basket")) return berryIcon;
        if (key.Contains("needle") || key.Contains("sewing")) return needleIcon;
        if (key.Contains("firefly") || key.Contains("jar")) return fireflyIcon;
        if (key.Contains("armor") || key.Contains("recovery") || key.Contains("tea")) return heartIcon;
        if (key.Contains("luck") || key.Contains("thread")) return magnetIcon;
        if (key.Contains("wide") || key.Contains("area")) return pulseIcon;
        if (key.Contains("ring") || key.Contains("ember")) return ringIcon;
        if (key.Contains("gravity") || key.Contains("magnet")) return magnetIcon;
        if (key.Contains("wind") || key.Contains("vital")) return heartIcon;
        if (key.Contains("feet") || key.Contains("haste")) return bootIcon;
        return wandIcon;
    }

    private GUIStyle StyleForChoice(string tag)
    {
        string key = (tag ?? string.Empty).ToLowerInvariant();
        if (key.Contains("evolution") || key.Contains("rare")) return cardCoralStyle ?? buttonStyle;
        if (key.Contains("passive") || key.Contains("blessing")) return cardMintStyle ?? buttonStyle;
        return cardNormalStyle ?? buttonStyle;
    }

    private void DrawStateCard(string title, string message, string prompt)
    {
        GUI.DrawTexture(new Rect(0f, 0f, ReferenceWidth, ReferenceHeight), veil, ScaleMode.StretchToFill);
        DrawPanel(new Rect(238f, 175f, 484f, 230f), parchmentPanelStyle);
        GUI.Label(new Rect(270f, 205f, 420f, 46f), title, MakeStyle(CutePixelKit.FriendlyFont, 25, CutePixelKit.Hex("4C3045"), FontStyle.Bold, TextAnchor.MiddleCenter, false));
        GUI.Label(new Rect(285f, 267f, 390f, 54f), message, MakeStyle(CutePixelKit.FriendlyFont, 14, CutePixelKit.Hex("5E4647"), FontStyle.Normal, TextAnchor.MiddleCenter, true));
        GUI.Label(new Rect(285f, 342f, 390f, 25f), prompt, MakeStyle(CutePixelKit.FriendlyFont, 13, CutePixelKit.Hex("7D5147"), FontStyle.Bold, TextAnchor.MiddleCenter, false));
    }

    private void DrawBar(Rect rect, float amount, Texture2D fill)
    {
        GUI.DrawTexture(rect, barBack, ScaleMode.StretchToFill);
        Rect inner = new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, (rect.width - 4f) * Mathf.Clamp01(amount)), Mathf.Max(1f, rect.height - 4f));
        if (inner.width > 0f) GUI.DrawTexture(inner, fill, ScaleMode.StretchToFill);
    }

    private static void DrawSprite(Sprite sprite, Rect rect)
    {
        if (sprite == null || sprite.texture == null) return;
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, new Rect(
            sprite.textureRect.x / sprite.texture.width,
            sprite.textureRect.y / sprite.texture.height,
            sprite.textureRect.width / sprite.texture.width,
            sprite.textureRect.height / sprite.texture.height), true);
    }

    private void InvokeUpgrade(int index)
    {
        if (applyUpgrade == null) return;
        try { applyUpgrade.Invoke(simulation, new object[] { index }); }
        catch (TargetInvocationException exception) { Debug.LogException(exception.InnerException ?? exception); }
    }

    private FieldInfo GetField(string name)
    {
        FieldInfo field;
        if (fieldCache.TryGetValue(name, out field)) return field;
        field = simulation.GetType().GetField(name, InstancePrivate);
        fieldCache[name] = field;
        return field;
    }

    private object GetValue(string name)
    {
        if (simulation == null) return null;
        FieldInfo field = GetField(name);
        return field == null ? null : field.GetValue(simulation);
    }

    private int GetInt(string name)
    {
        object value = GetValue(name);
        return value is int ? (int)value : 0;
    }

    private float GetFloat(string name)
    {
        object value = GetValue(name);
        return value is float ? (float)value : 0f;
    }

    private bool GetBool(string name)
    {
        object value = GetValue(name);
        return value is bool && (bool)value;
    }

    private string GetString(string name)
    {
        object value = GetValue(name);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetMode()
    {
        object value = GetValue("mode");
        return value == null ? string.Empty : value.ToString();
    }

    private IList GetList(string name)
    {
        return GetValue(name) as IList;
    }

    private static string ReadChoice(object choice, string fieldName)
    {
        if (choice == null) return string.Empty;
        FieldInfo field = choice.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        object value = field == null ? null : field.GetValue(choice);
        return value == null ? string.Empty : value.ToString();
    }

    private static string Humanize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string value = text.Replace("//", " · ").Replace("_", " ").Trim();
        value = value.Replace("INSTALLED", "learned").Replace("CHEST OPENED", "Story chest opened");
        value = value.Replace("WEAPON", "wand craft").Replace("PASSIVE", "small blessing").Replace("EVOLUTION", "rare tale");
        value = value.ToLowerInvariant();
        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }
}
