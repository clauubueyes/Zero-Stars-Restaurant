using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Editor
{
    public static class VP1BCArtBuilder
    {
        private static readonly List<GameObject> Shells = new List<GameObject>();
        private static readonly Dictionary<Renderer, bool> Originals = new Dictionary<Renderer, bool>();
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static Material M(string name) => Materials[name];
        private static void Palette()
        {
            VP1BCArtAssets.Prepare(); Materials.Clear();
            void Add(string name, float r, float g, float b, string pattern, float metal = 0, float smooth = .22f)
                => Materials.Add(name, VP1BCArtAssets.Material(name, new Color(r, g, b), pattern, metal, smooth));
            Add("BrushedSteel", .64f, .67f, .65f, "Steel", .60f, .34f);
            Add("DullGalvanized", .38f, .40f, .38f, "Steel", .60f, .22f);
            Add("SeasonedGriddle", .24f, .22f, .19f, "Griddle", .35f, .23f);
            Add("FloorTile", .45f, .46f, .43f, "Floor", 0, .20f);
            Add("KitchenTile", .76f, .77f, .70f, "Tile", 0, .32f);
            Add("OldCreamPaint", .64f, .61f, .51f, "Paint", 0, .10f);
            Add("CeilingPanel", .47f, .46f, .40f, "Ceiling", 0, .10f);
            Add("BrownLaminate", .30f, .215f, .13f, "Wood", 0, .27f);
            Add("FadedWood", .43f, .33f, .21f, "Wood", 0, .12f);
            Add("BlackRubber", .045f, .047f, .044f, "Paint", 0, .10f);
            Add("CreamEnamel", .70f, .71f, .65f, "Paint", .12f, .30f);
            Add("FreezerEnamel", .56f, .61f, .60f, "Paint", .12f, .25f);
            Add("FadedRed", .32f, .075f, .05f, "Paint", 0, .16f);
            Add("Paper", .81f, .77f, .63f, "Paint", 0, .05f);
            Add("Cardboard", .42f, .30f, .17f, "Paint", 0, .06f);
            Add("BunCrust", .79f, .46f, .17f, "Bread", 0, .18f);
            Add("BunCrumb", .80f, .68f, .43f, "Bread", 0, .08f);
            Add("RawPatty", .57f, .18f, .155f, "Meat", 0, .24f);
            Add("CookedPatty", .26f, .13f, .065f, "Cooked", 0, .16f);
            Add("BurntPatty", .045f, .033f, .025f, "Cooked", 0, .05f);
            Add("CheeseSlice", .92f, .62f, .14f, "Paint", 0, .27f);
            Add("PlateCeramic", .79f, .78f, .68f, "Paint", 0, .40f);
            Add("OldSponge", .45f, .36f, .12f, "Bread", 0, .05f);
        }
        private static GameObject Root(Transform parent)
        {
            var shell = new GameObject("Visual_VP1BC"); shell.transform.SetParent(parent, false);
            Vector3 s = parent.lossyScale;
            shell.transform.localScale = new Vector3(1 / s.x, 1 / s.y, 1 / s.z);
            Shells.Add(shell); return shell;
        }
        private static void Hide(Renderer renderer)
        { if (renderer != null && !Originals.ContainsKey(renderer)) { Originals.Add(renderer, renderer.enabled); renderer.enabled = false; } }
        private static Vector3 Size(Transform t)
        {
            var box = t.GetComponent<BoxCollider>(); var mesh = t.GetComponent<MeshFilter>();
            Vector3 size = box != null ? box.size : mesh.sharedMesh.bounds.size;
            return Vector3.Scale(size, new Vector3(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z)));
        }
        private static Renderer Piece(Transform parent, string name, Vector3 position, Vector3 size, string material)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); item.transform.SetParent(parent, false);
            item.transform.localPosition = position; item.GetComponent<MeshFilter>().sharedMesh = VP1BCArtAssets.Box(size);
            var renderer = item.GetComponent<MeshRenderer>(); renderer.sharedMaterial = M(material); return renderer;
        }
        private static Transform ReplaceBox(Transform t, string material)
        {
            var root = Root(t).transform; Hide(t.GetComponent<Renderer>());
            var box = t.GetComponent<BoxCollider>(); Vector3 center = box != null ? box.center : Vector3.zero;
            center = Vector3.Scale(center, t.lossyScale);
            Piece(root, "Cladding", center, Size(t), material); return root;
        }
        private static void Table(Transform t, bool enclosed = false)
        {
            var root = Root(t).transform; Hide(t.GetComponent<Renderer>()); var s = Size(t);
            // Match the original top exactly, so food and cleaning contact remain truthful.
            Piece(root, "StainlessWorktop", new Vector3(0, s.y * .5f - .028f, 0), new Vector3(s.x, .056f, s.z), "BrushedSteel");
            if (enclosed)
                Piece(root, "ClosedCabinet", new Vector3(0, -.025f, 0), new Vector3(s.x - .07f, s.y - .15f, s.z - .07f), "DullGalvanized");
            else
            {
                // Enclosed apron keeps the preserved greybox collision volume visually legible.
                Piece(root, "RecessedApron", new Vector3(0, -.06f, 0), new Vector3(s.x - .08f, s.y - .18f, s.z - .08f), "DullGalvanized");
            }
            foreach (float x in new[] { -s.x * .5f + .07f, s.x * .5f - .07f }) foreach (float z in new[] { -s.z * .5f + .07f, s.z * .5f - .07f })
                Piece(root, "SquareLeg", new Vector3(x, -.035f, z), new Vector3(.065f, s.y - .07f, .065f), "BrushedSteel");
            for (int i = 0; i < Mathf.Max(1, Mathf.FloorToInt(s.x / 1.1f)); i++)
            {
                float x = -s.x * .5f + .4f + i * 1.05f;
                Piece(root, "DrawerPull", new Vector3(x, s.y * .23f, -s.z * .5f - .004f), new Vector3(.23f, .025f, .035f), "BlackRubber");
                Piece(root, "PanelSeam", new Vector3(x + .38f, -.01f, -s.z * .5f + .012f), new Vector3(.008f, s.y - .18f, .01f), "BlackRubber");
            }
        }
        private static void Wall(Transform t)
        {
            var root = ReplaceBox(t, "OldCreamPaint"); Vector3 s = Size(t);
            bool alongX = s.x > s.z; float length = alongX ? s.x : s.z;
            float thin = alongX ? s.z : s.x;
            foreach (float side in new[] { -1f, 1f })
            {
                bool kitchen = t.name.Contains("North") || t.name == "KitchenDivider" || t.name == "WallEast";
                float height = Mathf.Min(s.y, kitchen ? 1.55f : .9f);
                Vector3 p = alongX ? new Vector3(0, -s.y * .5f + height * .5f, side * (thin * .5f + .002f)) :
                    new Vector3(side * (thin * .5f + .002f), -s.y * .5f + height * .5f, 0);
                Vector3 size = alongX ? new Vector3(length, height, .004f) : new Vector3(.004f, height, length);
                Piece(root, kitchen ? "TileWainscot" : "PaintWainscot", p, size, kitchen ? "KitchenTile" : "CreamEnamel");
                p.y = -s.y * .5f + .055f; size.y = .11f;
                Piece(root, "Skirting", p, size, "DullGalvanized");
                p.y = -s.y * .5f + height; size.y = .025f;
                Piece(root, "WainscotTrim", p, size, "BrownLaminate");
            }
        }
        private static void Grill(Transform baseObject, Transform hot)
        {
            Table(baseObject, true); ReplaceBox(hot, "SeasonedGriddle");
            var root = Shells[Shells.Count - 2].transform; Vector3 s = Size(baseObject);
            Piece(root, "ControlFascia", new Vector3(0, .30f, -s.z * .5f - .015f), new Vector3(s.x, .21f, .05f), "BrushedSteel");
            for (int i = 0; i < 4; i++)
            {
                var knob = new GameObject("TemperatureDial", typeof(MeshFilter), typeof(MeshRenderer)); knob.transform.SetParent(root, false);
                knob.transform.localPosition = new Vector3(-s.x * .32f + i * s.x * .215f, .3f, -s.z * .5f - .055f);
                knob.transform.localRotation = Quaternion.Euler(90, 0, 0); knob.transform.localScale = new Vector3(.09f, .035f, .09f);
                knob.GetComponent<MeshFilter>().sharedMesh = VP1BCArtAssets.FoodMesh("Patty"); knob.GetComponent<Renderer>().sharedMaterial = M("BlackRubber");
                Piece(root, "DialIndex", new Vector3(-s.x * .32f + i * s.x * .215f, .34f, -s.z * .5f - .076f), new Vector3(.01f, .028f, .012f), "Paper");
            }
            Piece(root, "RearSplash", new Vector3(0, .67f, s.z * .5f - .018f), new Vector3(s.x, .35f, .035f), "BrushedSteel");
            Piece(root, "GreaseDrawer", new Vector3(0, .13f, -s.z * .5f - .025f), new Vector3(s.x * .7f, .09f, .06f), "DullGalvanized");
            Piece(root, "ExtractorHood", new Vector3(0, 2.28f, .05f), new Vector3(s.x + .12f, .28f, 1.05f), "DullGalvanized").shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Piece(root, "ExtractorLip", new Vector3(0, 2.11f, -.4f), new Vector3(s.x + .15f, .07f, .12f), "BrushedSteel").shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < 9; i++) Piece(root, "FilterSlat", new Vector3(-s.x * .4f + i * s.x * .1f, 2.105f, .05f), new Vector3(.025f, .025f, .7f), "BlackRubber").shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Piece(root, "ExtractionDuct", new Vector3(.38f, 2.65f, .18f), new Vector3(.45f, .48f, .48f), "DullGalvanized").shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        private static void Cold(ColdStorage storage)
        {
            bool freezer = storage.name == "Freezer";
            foreach (var t in storage.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponent<MeshFilter>() != null).ToArray())
                ReplaceBox(t, t.name == "Shelf" ? "BrushedSteel" : freezer ? "FreezerEnamel" : "CreamEnamel");
            var root = Root(storage.transform).transform;
            // Existing cabinets are open. A fixed, visibly open door leaf sits flush against the side;
            // no new door blocks the original thermal interior, opening or physical shelf.
            Piece(root, "FixedOpenDoor", new Vector3(-.83f, 1.53f, -.02f), new Vector3(.055f, 1.42f, 1.18f), freezer ? "FreezerEnamel" : "CreamEnamel");
            Piece(root, "DoorGasket", new Vector3(-.80f, 1.53f, -.02f), new Vector3(.018f, 1.34f, 1.08f), "BlackRubber");
            Piece(root, "DoorHandle", new Vector3(-.875f, 1.55f, -.42f), new Vector3(.07f, .46f, .055f), "BrushedSteel");
            Piece(root, "CompressorGrille", new Vector3(0, .25f, -.706f), new Vector3(1.42f, .25f, .018f), "DullGalvanized");
            for (int i = 0; i < 6; i++) Piece(root, "Louver", new Vector3(0, .16f + i * .035f, -.72f), new Vector3(1.30f, .009f, .014f), "BlackRubber");
            Piece(root, "HeaderBadge", new Vector3(.42f, 2.30f, -.706f), new Vector3(.37f, .055f, .012f), freezer ? "FadedRed" : "BlackRubber");
            Label(root, freezer ? "FREEZER  /  -18 C" : "COLD STORE  /  +4 C", new Vector3(.40f, 2.3f, -.719f), .024f);
        }
        private static TextMesh Label(Transform parent, string text, Vector3 position, float size, float roll = 0)
        {
            var item = new GameObject("PhysicalLabel", typeof(TextMesh)); item.transform.SetParent(parent, false);
            item.transform.localPosition = position; item.transform.localRotation = Quaternion.Euler(0, 0, roll);
            var label = item.GetComponent<TextMesh>(); label.text = text; label.characterSize = size / 6.4f; label.fontSize = 64;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = new Color(.8f, .78f, .67f);
            return label;
        }
        private static void Counter(Transform t)
        {
            var root = ReplaceBox(t, "BrownLaminate"); var s = Size(t);
            Piece(root, "CounterTop", new Vector3(0, s.y * .5f - .022f, 0), new Vector3(s.x, .044f, s.z), "CreamEnamel");
            Piece(root, "MetalEdge", new Vector3(0, s.y * .5f - .05f, -s.z * .5f - .002f), new Vector3(s.x, .07f, .015f), "BrushedSteel");
            Piece(root, "KickPlate", new Vector3(0, -s.y * .5f + .065f, -s.z * .5f - .004f), new Vector3(s.x, .13f, .014f), "DullGalvanized");
            for (float x = -s.x * .5f + 1.2f; x < s.x * .5f; x += 1.5f)
                Piece(root, "PanelJoint", new Vector3(x, -.035f, -s.z * .5f - .005f), new Vector3(.014f, s.y - .16f, .018f), "BlackRubber");
            Piece(root, "PassFascia", new Vector3(0, .23f, -s.z * .5f - .022f), new Vector3(.90f, .21f, .025f), "FadedRed");
            Label(root, "ORDER PICKUP", new Vector3(0, .23f, -s.z * .5f - .039f), .055f);
        }
        private static void Pass(Transform t)
        {
            var root = ReplaceBox(t, "BrushedSteel"); var s = Size(t);
            foreach (float x in new[] { -s.x * .5f + .009f, s.x * .5f - .009f })
                Piece(root, "TrayEdge", new Vector3(x, .02f, 0), new Vector3(.018f, .028f, s.z), "DullGalvanized");
            Piece(root, "RearTrayEdge", new Vector3(0, .02f, s.z * .5f - .009f), new Vector3(s.x, .028f, .018f), "DullGalvanized");
        }
        private static void DressProps(Transform north, Transform south, Transform east)
        {
            var root = Root(north).transform; var s = Size(north);
            // Wall-mounted above the worktop; leaves every functional top clear.
            Piece(root, "WallShelf", new Vector3(.30f, .12f, -s.z * .5f - .24f), new Vector3(2.6f, .045f, .42f), "BrushedSteel").shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (float x in new[] { -.78f, 1.38f }) Piece(root, "ShelfBracket", new Vector3(x, -.03f, -.40f), new Vector3(.035f, .30f, .20f), "DullGalvanized");
            for (int i = 0; i < 4; i++)
            {
                Piece(root, "StorageTin", new Vector3(-.62f + i * .54f, .27f, -.47f), new Vector3(.24f + .02f * (i % 2), .25f, .24f), i % 2 == 0 ? "CreamEnamel" : "DullGalvanized");
                Piece(root, "TinLid", new Vector3(-.62f + i * .54f, .40f, -.47f), new Vector3(.27f, .02f, .27f), "BlackRubber");
            }
            Piece(root, "SurfaceConduit", new Vector3(1.90f, .72f, -.30f), new Vector3(3.8f, .025f, .025f), "DullGalvanized");
            Piece(root, "SocketBox", new Vector3(2.1f, -.18f, -.28f), new Vector3(.16f, .19f, .05f), "CreamEnamel");
            Piece(root, "SocketInset", new Vector3(2.1f, -.18f, -.312f), new Vector3(.065f, .085f, .012f), "BlackRubber");
            var publicRoot = Root(south).transform; var ps = Size(south);
            Piece(publicRoot, "OldMenuBoard", new Vector3(.3f, .12f, ps.z * .5f + .035f), new Vector3(1.5f, 1.0f, .06f), "FadedWood");
            Piece(publicRoot, "MenuInset", new Vector3(.3f, .12f, ps.z * .5f + .072f), new Vector3(1.38f, .88f, .018f), "BlackRubber");
            var menu = new GameObject("MenuLettering", typeof(TextMesh)); menu.transform.SetParent(publicRoot, false);
            menu.transform.localPosition = new Vector3(.3f, .12f, ps.z * .5f + .084f);
            menu.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var text = menu.GetComponent<TextMesh>(); text.text = "HOT FOOD\n\nHAMBURGER     5.00\nCHEESEBURGER  6.50\n\nORDER AT COUNTER";
            text.characterSize = .012f; text.fontSize = 64; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = new Color(.73f, .69f, .54f);
            var eroot = Root(east).transform; var es = Size(east);
            var notice = Piece(eroot, "MaintenanceNotice", new Vector3(-es.x * .5f - .012f, .1f, -3.0f), new Vector3(.014f, .42f, .30f), "Paper");
            notice.transform.localRotation = Quaternion.Euler(4, 0, 0);
            Piece(eroot, "WallVent", new Vector3(-es.x * .5f - .04f, .95f, 2.2f), new Vector3(.08f, .43f, .64f), "CreamEnamel");
            for (int i = 0; i < 7; i++) Piece(eroot, "VentSlot", new Vector3(-es.x * .5f - .085f, .80f + i * .048f, 2.2f), new Vector3(.008f, .012f, .53f), "BlackRubber");
        }
        private static void Food(GameObject item)
        {
            var food = item.GetComponent<FoodItem>(); string name = food != null ? food.Definition.Id : "plate";
            var root = new GameObject("Visual_VP1BC"); root.transform.SetParent(item.transform, false); Shells.Add(root); Hide(item.GetComponent<Renderer>());
            string kind = name.Contains("bun") ? "Bun" : name.Contains("cheese") ? "Cheese" : name == "plate" ? "Plate" : "Patty";
            var visual = new GameObject(kind, typeof(MeshFilter), typeof(MeshRenderer)); visual.transform.SetParent(root.transform, false);
            visual.GetComponent<MeshFilter>().sharedMesh = kind == "Cheese" ? VP1BCArtAssets.Box(new Vector3(.98f, .92f, .98f)) : VP1BCArtAssets.FoodMesh(kind);
            var renderer = visual.GetComponent<Renderer>(); renderer.sharedMaterial = M(kind == "Bun" ? "BunCrust" : kind == "Cheese" ? "CheeseSlice" : kind == "Plate" ? "PlateCeramic" : "RawPatty");
            if (kind == "Bun")
            {
                var seam = Piece(root.transform, "CutCrumb", new Vector3(0, -.32f, 0), new Vector3(.87f, .045f, .87f), "BunCrumb");
                // A thin round cut instead of an exposed square slab.
                seam.GetComponent<MeshFilter>().sharedMesh = VP1BCArtAssets.FoodMesh("Patty"); seam.transform.localScale = new Vector3(.95f, .045f, .95f);
                // Sesame is one combined mesh, without dozens of transforms per unit.
                var instances = new List<CombineInstance>(); var seed = VP1BCArtAssets.Box(new Vector3(.020f, .012f, .035f));
                for (int i = 0; i < 22; i++)
                {
                    float a = i * 2.39996f, radius = .34f * Mathf.Sqrt((i + .5f) / 22);
                    Vector3 p = new Vector3(Mathf.Cos(a) * radius, .49f - radius * radius * .95f, Mathf.Sin(a) * radius);
                    instances.Add(new CombineInstance { mesh = seed, transform = Matrix4x4.TRS(p, Quaternion.Euler(0, i * 37, 0), Vector3.one) });
                }
                string path = VP1BCArtAssets.Folder + "/Meshes/Sesame.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null) { mesh = new Mesh { name = "Sesame" }; mesh.CombineMeshes(instances.ToArray()); AssetDatabase.CreateAsset(mesh, path); }
                var seeds = new GameObject("Sesame", typeof(MeshFilter), typeof(MeshRenderer)); seeds.transform.SetParent(root.transform, false);
                seeds.GetComponent<MeshFilter>().sharedMesh = mesh; seeds.GetComponent<Renderer>().sharedMaterial = M("BunCrumb");
            }
            if (kind == "Patty")
            {
                var data = new SerializedObject(root.AddComponent<FoodStageVisual>());
                data.FindProperty("_food").objectReferenceValue = food; data.FindProperty("_renderer").objectReferenceValue = renderer;
                data.FindProperty("_raw").objectReferenceValue = M("RawPatty"); data.FindProperty("_cooked").objectReferenceValue = M("CookedPatty");
                data.FindProperty("_burnt").objectReferenceValue = M("BurntPatty"); data.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        public static RestaurantArtPass ConfigureScene(Scene scene)
        {
            T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
            var existing = All<RestaurantArtPass>().SingleOrDefault(); if (existing != null) return existing;
            Palette(); Shells.Clear(); Originals.Clear(); var transforms = All<Transform>();
            Transform Find(string name) => transforms.Single(t => t.name == name);
            foreach (string name in new[] { "Floor", "ProcurementFloor" }) ReplaceBox(Find(name), "FloorTile");
            foreach (var t in transforms.Where(t => t.GetComponent<MeshRenderer>() != null &&
                (t.name.StartsWith("Wall") || t.name.EndsWith("WallEnd") || t.name.Contains("Lintel") || t.name.StartsWith("Procurement") && t.name.EndsWith("Wall") || t.name == "KitchenDivider"))) Wall(t);
            foreach (string name in new[] { "MainVisualCeiling", "ProcurementVisualCeiling" }) ReplaceBox(Find(name), "CeilingPanel");
            Grill(Find("GrillBase"), Find("GrillHotSurface"));
            foreach (string name in new[] { "AssemblyWorkbench", "CookingPrepBench", "WorktopVolume", "TableVolume" }) Table(Find(name));
            foreach (var t in transforms.Where(t => t.name.StartsWith("PrepSupport") || t.name == "TrayVisual")) ReplaceBox(t, "BrushedSteel");
            Counter(Find("ServiceCounterVolume")); Pass(Find("DeliveryPad"));
            foreach (var cold in All<ColdStorage>()) Cold(cold);
            foreach (var food in All<FoodItem>()) Food(food.gameObject);
            // The procurement bench stays in the user's annex. Buttons remain the same interactables.
            foreach (var t in transforms.Where(t => t.name.StartsWith("Buy")))
            {
                var root = ReplaceBox(t, "DullGalvanized"); var s = Size(t);
                Piece(root, "ButtonFace", new Vector3(0, 0, s.z * .5f + .006f), new Vector3(s.x - .035f, s.y - .035f, .012f), "CreamEnamel");
                var label = Label(root, t.name == "BuyBun" ? "BUN  0.35" : t.name == "BuyRawBeefPatty" ? "PATTY  0.80" : t.name == "BuyCheese" ? "CHEESE  0.25" : "PLATE  0.50", new Vector3(0, 0, s.z * .5f + .014f), .060f);
                label.transform.localRotation = Quaternion.Euler(0, 180, 0);
                label.color = new Color(.08f, .07f, .05f);
            }
            var output = ReplaceBox(Find("OutputMarker"), "BrushedSteel");
            ReplaceBox(Find("CleaningTool"), "OldSponge");
            Label(output, "COLLECT HERE", new Vector3(0, .008f, .13f), .055f).transform.localRotation = Quaternion.Euler(90, 180, 0);
            var power = ReplaceBox(Find("ElectricitySwitch"), "CreamEnamel");
            Label(power, "POWER", new Vector3(0, .12f, -.08f), .070f).color = new Color(.08f, .07f, .05f);
            Piece(power, "SwitchLever", new Vector3(0, -.05f, -.09f), new Vector3(.08f, .18f, .045f), "FadedRed");
            // Hide floating development signage only. Their texts/transforms and interaction HUD remain intact.
            foreach (var t in transforms.Where(t => t.GetComponent<TextMesh>() != null)) Hide(t.GetComponent<Renderer>());
            foreach (var t in transforms.Where(t => t.name is "MarkerVisual" or "KitchenPassMarker" or "FinalizeTab")) Hide(t.GetComponent<Renderer>());
            DressProps(Find("WallNorth"), Find("WallSouth"), Find("WallEast"));
            var rootObject = new GameObject("RestaurantArtPass"); SceneManager.MoveGameObjectToScene(rootObject, scene);
            string exposurePath = VP1BCArtAssets.Folder + "/RestaurantArtExposure.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(exposurePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, exposurePath);
                var color = profile.Add<ColorAdjustments>(false); color.postExposure.overrideState = true; color.postExposure.value = .85f;
                AssetDatabase.AddObjectToAsset(color, profile); EditorUtility.SetDirty(profile);
            }
            var volume = rootObject.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 11; volume.sharedProfile = profile;
            var pass = rootObject.AddComponent<RestaurantArtPass>(); pass.InitializeForEditor(Shells.ToArray(), Originals.Keys.ToArray(), Originals.Values.ToArray(), volume);
            EditorSceneManager.MarkSceneDirty(scene); return pass;
        }
        [MenuItem("Zero Star Restaurant/Visual/Install VP1B-C Restaurant Art (incremental)")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Save edits, leave Play and close PrototypeRestaurant before installing art.");
            Directory.CreateDirectory("Build/VP1BC"); Palette();
            foreach (string path in new[] { "Assets/_Project/Prefabs/Food/Bun.prefab", "Assets/_Project/Prefabs/Food/RawBeefPatty.prefab", "Assets/_Project/Prefabs/Food/Cheese.prefab", "Assets/_Project/Prefabs/Plate.prefab" })
            {
                string original = File.ReadAllText(path); var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (contents.transform.Find("Visual_VP1BC") != null) continue;
                    Shells.Clear(); Originals.Clear(); Food(contents);
                    // Saving to its existing prefab retains original local fileIDs. A new path remaps them.
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    string merged;
                    try { merged = MergeVisualDocuments(original, File.ReadAllText(path)); }
                    catch { File.WriteAllText(path, original, new UTF8Encoding(false)); throw; }
                    File.WriteAllText(path, merged, new UTF8Encoding(false));
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                AssetDatabase.ImportAsset(path);
            }
            if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var previous = SceneManager.GetActiveScene(); string baseline = File.ReadAllText(M1GreyboxBuilder.ScenePath);
            var scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            string temporary = "Assets/_Project/Scenes/__VP1BC_" + Guid.NewGuid().ToString("N") + ".unity";
            try
            {
                if (scene.GetRootGameObjects().Any(r => r.GetComponent<RestaurantArtPass>() != null)) return;
                SceneManager.SetActiveScene(scene); ConfigureScene(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, temporary, true);
                string updated = MergeVisualDocuments(baseline, File.ReadAllText(temporary));
                File.WriteAllText("Build/VP1BC/PrototypeRestaurant.before.unity", baseline, new UTF8Encoding(false));
                File.WriteAllText(M1GreyboxBuilder.ScenePath, updated, new UTF8Encoding(false)); AssetDatabase.ImportAsset(M1GreyboxBuilder.ScenePath);
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); AssetDatabase.DeleteAsset(temporary); }
        }
        // Keep every original serialized document. Only append child/root references and hide old renderers.
        public static string MergeVisualDocuments(string original, string serialized)
        {
            List<string> Split(string text) => Regex.Split(text.Replace("\r\n", "\n"), "(?=^--- !u!)", RegexOptions.Multiline).ToList();
            long Id(string block) => long.Parse(Regex.Match(block, @"^--- !u!\d+ &(-?\d+)").Groups[1].Value);
            var before = Split(original); var after = Split(serialized).Skip(1).ToDictionary(Id); var ids = before.Skip(1).Select(Id).ToHashSet();
            for (int i = 1; i < before.Count; i++)
            {
                string block = before[i]; if (!after.TryGetValue(Id(block), out string replacement)) throw new InvalidOperationException("Removed original document.");
                if (block.StartsWith("--- !u!4 &"))
                {
                    string pattern = @"(?m)^  m_Children:(?: \[\])?\n(?:  - \{fileID: [^\r\n]*\}\n)*";
                    string oldChildren = Regex.Match(block, pattern).Value, newChildren = Regex.Match(replacement, pattern).Value;
                    foreach (Match child in Regex.Matches(oldChildren, @"\{fileID: (-?\d+)\}"))
                        if (!newChildren.Contains(child.Value)) throw new InvalidOperationException("Removed original child.");
                    var oldRefs = Regex.Matches(oldChildren, @"  - \{fileID: (-?\d+)\}").Cast<Match>().Select(m => m.Value).ToArray();
                    var additions = Regex.Matches(newChildren, @"  - \{fileID: (-?\d+)\}").Cast<Match>().Select(m => m.Value).Where(r => !oldRefs.Contains(r)).ToArray();
                    if (additions.Length > 0)
                    {
                        string children = "  m_Children:\n" + string.Join("\n", oldRefs.Concat(additions)) + "\n";
                        block = Regex.Replace(block, pattern, children);
                    }
                }
                else if (block.StartsWith("--- !u!23 &"))
                    block = Regex.Replace(block, @"(?m)^  m_Enabled: \d+$", Regex.Match(replacement, @"(?m)^  m_Enabled: \d+$").Value);
                else if (block.StartsWith("--- !u!1660057539 &"))
                {
                    var roots = Regex.Matches(block, @"  - \{fileID: (-?\d+)\}").Cast<Match>().Select(m => m.Value).ToHashSet();
                    foreach (Match root in Regex.Matches(replacement, @"  - \{fileID: (-?\d+)\}"))
                        if (!roots.Contains(root.Value)) block = block.TrimEnd('\n') + "\n" + root.Value + "\n";
                }
                before[i] = block;
            }
            foreach (var pair in after.Where(p => !ids.Contains(p.Key))) before.Add(pair.Value);
            return string.Concat(before).Replace("\n", original.Contains("\r\n") ? "\r\n" : "\n");
        }
    }
}
