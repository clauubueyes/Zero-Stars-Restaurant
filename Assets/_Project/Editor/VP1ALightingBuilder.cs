using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Presentation;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Editor
{
    public static class VP1ALightingBuilder
    {
        public const string SettingsPath = "Assets/_Project/ScriptableObjects/RestaurantAtmosphere.asset";
        public const string ProfilePath = "Assets/_Project/ScriptableObjects/RestaurantAtmosphereVolume.asset";
        private const string MaterialFolder = "Assets/_Project/Materials/Atmosphere";

        public static RestaurantAtmosphere ConfigureScene(Scene scene)
        {
            T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            var installed = All<RestaurantAtmosphere>().SingleOrDefault();
            if (installed != null) { installed.Validate(); return installed; } // Never reposition an installed pass.
            var supply = All<RestaurantElectricity>().Single(); supply.Validate();
            var camera = All<Camera>().Single();
            var referenceLight = All<Light>().Single(light => light.type == LightType.Directional);
            var surfaces = All<CleanableSurface>();
            var grill = surfaces.Single(surface => surface.DisplayName == "Grill").Support;
            var prep = All<Transform>().Single(t => t.name == "CookingPrepBench").GetComponent<BoxCollider>();
            var assembly = surfaces.Single(surface => surface.DisplayName == "Assembly 2").Support;
            var delivery = All<DeliveryZone>().Single().Support;
            var cold = All<ColdStorage>().Single(storage => storage.name == "Fridge");
            var transforms = All<Transform>();
            BoxCollider Box(string name) => transforms.Single(t => t.name == name).GetComponent<BoxCollider>();
            var west = Box("WallWest"); var east = Box("WallEast");
            var north = Box("WallNorth"); var south = Box("WallSouth");
            Physics.SyncTransforms();
            float ceilingHeight = new[] { west, east, north, south }.Min(box => box.bounds.max.y);
            float fixtureHeight = ceilingHeight - .17f;

            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_Project/Materials", "Atmosphere");
            var housing = Material("FluorescentHousing", new Color(.25f, .27f, .26f), .22f, .28f);
            var caps = Material("FluorescentEndCaps", new Color(.48f, .46f, .40f), .08f, 0);
            var tubes = Material("FluorescentTube", new Color(.76f, .8f, .79f), .35f, 0, new Color(3.1f, 3.2f, 3.08f));
            var ceiling = Material("AtmosphereCeiling", new Color(.16f, .17f, .16f), .05f, 0);
            var settings = AssetDatabase.LoadAssetAtPath<RestaurantAtmosphereSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<RestaurantAtmosphereSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            var profile = Profile();

            var root = new GameObject("AtmospherePass"); SceneManager.MoveGameObjectToScene(root, scene);
            var visuals = new GameObject("AtmosphereVisuals"); visuals.transform.SetParent(root.transform, false);
            // Only new visual surfaces, no colliders. The current walls determine the footprint.
            Surface(visuals.transform, "MainVisualCeiling", ceiling,
                new Vector3((west.bounds.max.x + east.bounds.min.x) / 2, ceilingHeight + .04f, (south.bounds.max.z + north.bounds.min.z) / 2),
                new Vector3(east.bounds.min.x - west.bounds.max.x, .08f, north.bounds.min.z - south.bounds.max.z));
            var annexWest = Box("ProcurementWestWall"); var annexNorth = Box("ProcurementNorthWall"); var annexSouth = Box("ProcurementSouthWall");
            Surface(visuals.transform, "ProcurementVisualCeiling", ceiling,
                new Vector3((annexWest.bounds.max.x + west.bounds.max.x) / 2, ceilingHeight + .04f, (annexSouth.bounds.max.z + annexNorth.bounds.min.z) / 2),
                new Vector3(west.bounds.max.x - annexWest.bounds.max.x, .08f, annexNorth.bounds.min.z - annexSouth.bounds.max.z));

            Vector3 Above(Vector3 position, float zOffset = 0) => new Vector3(position.x, fixtureHeight, position.z + zOffset);
            Fixture(visuals.transform, "GrillFluorescent", Above(grill.bounds.center, -.45f), 6, 6.5f, true, supply, housing, caps, tubes);
            Fixture(visuals.transform, "AssemblyFluorescent", Above(assembly.bounds.center, -.45f), 6, 6.5f, true, supply, housing, caps, tubes);
            Fixture(visuals.transform, "ColdStorageFluorescent", Above(cold.transform.position + Vector3.right * 1.25f, -.65f), 10, 6, false, supply, housing, caps, tubes);
            Fixture(visuals.transform, "PrepFluorescent", Above(prep.bounds.center), 16, 6, true, supply, housing, caps, tubes);
            Fixture(visuals.transform, "PassFluorescent", Above(delivery.bounds.center), 14, 6, true, supply, housing, caps, tubes);
            var customerPosition = new Vector3(delivery.bounds.center.x - 1.5f, fixtureHeight, (south.bounds.max.z + delivery.bounds.center.z) / 2);
            Fixture(visuals.transform, "CustomerFluorescent", customerPosition, 10, 7, false, supply, housing, caps, tubes);
            var procurement = All<ZeroStarRestaurant.Economy.IngredientPurchaseStation>().Single();
            var output = (Transform)new SerializedObject(procurement).FindProperty("_output").objectReferenceValue;
            Fixture(visuals.transform, "ProcurementFluorescent", Above(output.position, .4f), 10, 5.5f, false, supply, housing, caps, tubes);

            foreach (string opening in new[] { "EntranceLintel", "ExitLintel" })
            {
                var lintel = Box(opening).bounds;
                var light = Spot(visuals.transform, opening + "ExteriorDusk", new Vector3(lintel.center.x, ceilingHeight - .6f, lintel.min.z - .6f),
                    1.5f, 5, false, new Color(.74f, .8f, .88f));
                light.spotAngle = 90;
                light.innerSpotAngle = 75;
                light.transform.LookAt(new Vector3(lintel.center.x, .35f, lintel.max.z + 1.5f));
            }
            var volume = root.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10; volume.sharedProfile = profile;
            var atmosphere = root.AddComponent<RestaurantAtmosphere>();
            atmosphere.InitializeForEditor(settings, visuals, volume, camera, referenceLight);
            EditorUtility.SetDirty(atmosphere); EditorSceneManager.MarkSceneDirty(scene);
            return atmosphere;
        }

        private static Material Material(string name, Color color, float smoothness, float metallic, Color emission = default)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness); material.SetFloat("_Metallic", metallic);
            if (emission.maxColorComponent > 0)
            {
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", emission);
                // URP requires AnyEmissive to retain _EMISSION. Realtime GI is disabled;
                // this material contributes no baked lighting that can survive a power cut.
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(material, path); return material;
        }

        private static VolumeProfile Profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath);
            var tonemapping = profile.Add<Tonemapping>(true); tonemapping.mode.value = TonemappingMode.ACES;
            var color = profile.Add<ColorAdjustments>(true); color.postExposure.value = .25f; color.contrast.value = 8; color.saturation.value = -8;
            var balance = profile.Add<WhiteBalance>(true); balance.temperature.value = -3; balance.tint.value = -1;
            var bloom = profile.Add<Bloom>(true); bloom.threshold.value = 1.6f; bloom.intensity.value = .12f; bloom.scatter.value = .35f;
            bloom.clamp.value = 4; bloom.highQualityFiltering.value = false;
            var vignette = profile.Add<Vignette>(true); vignette.intensity.value = .08f; vignette.smoothness.value = .45f;
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile); return profile;
        }

        private static Renderer Surface(Transform parent, string name, Material material, Vector3 position, Vector3 scale, PrimitiveType primitive = PrimitiveType.Cube)
        {
            var item = GameObject.CreatePrimitive(primitive); item.name = name; item.transform.SetParent(parent, false);
            item.transform.position = position; item.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            var renderer = item.GetComponent<Renderer>(); renderer.sharedMaterial = material; return renderer;
        }

        private static void Fixture(Transform parent, string name, Vector3 position, float intensity, float range, bool shadows,
            RestaurantElectricity supply, Material housing, Material caps, Material tubes)
        {
            var fixture = new GameObject(name); fixture.transform.SetParent(parent, false); fixture.transform.position = position;
            Surface(fixture.transform, "Housing", housing, position, new Vector3(1.5f, .065f, .3f));
            foreach (float x in new[] { -.69f, .69f })
                Surface(fixture.transform, "EndCap", caps, position + new Vector3(x, -.035f, 0), new Vector3(.07f, .085f, .3f));
            var renderers = new List<Renderer>();
            foreach (float z in new[] { -.08f, .08f })
            {
                var tube = Surface(fixture.transform, "Tube", tubes, position + new Vector3(0, -.055f, z), new Vector3(.035f, .64f, .035f), PrimitiveType.Cylinder);
                tube.transform.rotation = Quaternion.Euler(0, 0, 90); tube.shadowCastingMode = ShadowCastingMode.Off; renderers.Add(tube);
            }
            var light = Spot(fixture.transform, "FunctionalLight", position + Vector3.down * .12f, intensity, range, shadows, new Color(.96f, 1, .97f));
            var data = new SerializedObject(fixture.AddComponent<PoweredLightFixture>());
            data.FindProperty("_supply").objectReferenceValue = supply; data.FindProperty("_light").objectReferenceValue = light;
            var list = data.FindProperty("_tubes"); list.arraySize = renderers.Count;
            for (int index = 0; index < renderers.Count; index++) list.GetArrayElementAtIndex(index).objectReferenceValue = renderers[index];
            data.FindProperty("_litEmission").colorValue = tubes.GetColor("_EmissionColor"); data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Light Spot(Transform parent, string name, Vector3 position, float intensity, float range, bool shadows, Color color)
        {
            var item = new GameObject(name, typeof(Light)); item.transform.SetParent(parent, false);
            item.transform.SetPositionAndRotation(position, Quaternion.Euler(90, 0, 0));
            var light = item.GetComponent<Light>(); light.type = LightType.Spot; light.color = color;
            light.intensity = intensity; light.range = range; light.spotAngle = 140; light.innerSpotAngle = 100;
            light.lightmapBakeType = LightmapBakeType.Realtime; light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowBias = .03f; light.shadowNormalBias = .15f; light.shadowNearPlane = .1f;
            var additional = light.GetUniversalAdditionalLightData(); additional.usePipelineSettings = false;
            var data = new SerializedObject(additional); data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = 1;
            data.ApplyModifiedPropertiesWithoutUndo(); return light;
        }

        [MenuItem("Zero Star Restaurant/Visual/Install VP1A Lighting and Atmosphere (incremental)")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Save edits, stop Play and close PrototypeRestaurant before installing VP1A.");
            string original = File.ReadAllText(M1GreyboxBuilder.ScenePath);
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            string scratch = "Assets/_Project/Scenes/__VP1A_" + Guid.NewGuid().ToString("N") + ".unity";
            try
            {
                SceneManager.SetActiveScene(scene);
                if (scene.GetRootGameObjects().Any(root => root.GetComponent<RestaurantAtmosphere>() != null)) return;
                ConfigureScene(scene); AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene, scratch, true)) throw new InvalidOperationException("Could not serialize VP1A additions.");
                string updated = MergeOnlyLighting(original, File.ReadAllText(scratch));
                Directory.CreateDirectory("Build/VP1A");
                if (!File.Exists("Build/VP1A/PrototypeRestaurant.before.unity"))
                    File.WriteAllText("Build/VP1A/PrototypeRestaurant.before.unity", original, new UTF8Encoding(false));
                File.WriteAllText(M1GreyboxBuilder.ScenePath, updated, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(M1GreyboxBuilder.ScenePath);
            }
            finally
            {
                SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true);
                // Only the temporary copy created by this invocation is removed.
                AssetDatabase.DeleteAsset(scratch);
            }
        }

        // Unity serializes a temporary copy; preserve all original documents and fileIDs verbatim,
        // except the listed lighting fields and the appended new root. Never serialize gameplay again.
        public static string MergeOnlyLighting(string original, string serialized)
        {
            List<string> Split(string text) => Regex.Split(text, "(?=^--- !u!)", RegexOptions.Multiline).ToList();
            long Id(string text) => long.Parse(Regex.Match(text, @"^--- !u!\d+ &(-?\d+)").Groups[1].Value);
            var before = Split(original); var after = Split(serialized).Skip(1).ToDictionary(Id);
            string newline = original.Contains("\r\n") ? "\r\n" : "\n";
            var originalIds = new HashSet<long>(before.Skip(1).Select(Id));
            var cameraDataGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.FindAssets("t:MonoScript UniversalAdditionalCameraData")
                .Select(AssetDatabase.GUIDToAssetPath).Single(path => Path.GetFileNameWithoutExtension(path) == "UniversalAdditionalCameraData"));
            for (int index = 1; index < before.Count; index++)
            {
                string block = before[index]; long id = Id(block);
                if (!after.TryGetValue(id, out string replacement)) throw new InvalidOperationException("Existing scene document was removed: " + id);
                string[] fields = Array.Empty<string>();
                if (block.StartsWith("--- !u!104 &")) fields = new[] { "m_AmbientSkyColor", "m_AmbientEquatorColor", "m_AmbientGroundColor", "m_AmbientIntensity", "m_AmbientMode", "m_ReflectionIntensity", "m_Fog" };
                else if (block.StartsWith("--- !u!108 &") && block.Contains("  m_Type: 1")) fields = new[] { "m_Enabled" };
                else if (block.StartsWith("--- !u!20 &")) fields = new[] { "m_BackGroundColor" };
                else if (block.Contains("guid: " + cameraDataGuid + ",")) fields = new[] { "m_RenderPostProcessing" };
                foreach (string field in fields)
                {
                    string pattern = "^  " + field + @": .*\r?$";
                    string line = Regex.Match(replacement, pattern, RegexOptions.Multiline).Value.TrimEnd('\r');
                    if (line.Length == 0 || !Regex.IsMatch(block, pattern, RegexOptions.Multiline)) throw new InvalidOperationException("Lighting field missing: " + field);
                    block = Regex.Replace(block, pattern, line + (newline == "\r\n" ? "\r" : ""), RegexOptions.Multiline);
                }
                if (block.StartsWith("--- !u!1660057539 &"))
                {
                    var roots = Regex.Matches(block, @"  - \{fileID: (-?\d+)\}").Cast<Match>().Select(match => match.Value).ToHashSet();
                    var additions = Regex.Matches(replacement, @"  - \{fileID: (-?\d+)\}").Cast<Match>().Where(match => !roots.Contains(match.Value)).ToArray();
                    if (additions.Length != 1) throw new InvalidOperationException("VP1A must append exactly one root.");
                    block = block.TrimEnd('\r', '\n') + newline + additions[0].Value + newline;
                }
                before[index] = block;
            }
            foreach (var pair in after.Where(pair => !originalIds.Contains(pair.Key)))
                before.Add(pair.Value.Replace("\r\n", "\n").Replace("\n", newline));
            return string.Concat(before);
        }
    }
}
