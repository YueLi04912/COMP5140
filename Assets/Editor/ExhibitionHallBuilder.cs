using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to build a 20m x 200m exhibition hall in SampleScene.
/// Run via: Tools > Build Exhibition Hall
/// </summary>
public class ExhibitionHallBuilder
{
    // ── Hall dimensions ──────────────────────────────────────────────
    private const float Width  = 20f;   // X
    private const float Height = 4f;    // Y  (ceiling height)
    private const float Depth  = 20f;   // Z
    private const float Thick  = 0.2f;  // Wall / floor thickness

    // ── Spotlight settings ───────────────────────────────────────────
    private const float SpotHeight      = 3.8f;  // hang height (just below ceiling)
    private const float SpotRange       = 8f;
    private const float SpotAngle       = 35f;
    private const float SpotIntensity   = 30f;   // URP real-world lux-style value
    private const float SpotInnerRatio  = 0.5f;  // inner / outer angle ratio

    // Warm gallery white (3000 K-ish)
    private static readonly Color SpotColor = new Color(1f, 0.92f, 0.78f);

    // ── Exhibit cube settings ────────────────────────────────────────
    private static readonly Vector3 ExhibitScale  = new Vector3(1f, 1f, 1f);
    private const float ExhibitY = 0.5f;  // sits on the floor
    private const float ExhibitZ = 2.5f;  // distance from centre along ±Z

    // ── Pedestal settings ────────────────────────────────────────────
    private static readonly Vector3 PedestalScale = new Vector3(1.2f, 0.4f, 1.2f);

    [MenuItem("Tools/Build Exhibition Hall")]
    public static void Build()
    {
        // Remove old hall if it exists
        var old = GameObject.Find("ExhibitionHall");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
        }

        // Root container
        GameObject root = new GameObject("ExhibitionHall");
        Undo.RegisterCreatedObjectUndo(root, "Create Exhibition Hall");

        // ── 1. Architecture ──────────────────────────────────────────
        BuildArchitecture(root.transform);

        // ── 2. Exhibits ──────────────────────────────────────────────
        BuildExhibit(root.transform, "Exhibit_A",
                     new Vector3(-2f, 0f,  ExhibitZ), new Color(0.3f, 0.5f, 0.9f));
        BuildExhibit(root.transform, "Exhibit_B",
                     new Vector3( 2f, 0f, -ExhibitZ), new Color(0.9f, 0.4f, 0.3f));

        // ── 3. Lighting ──────────────────────────────────────────────
        // Lower ambient so spotlights are dramatic
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);
        RenderSettings.ambientIntensity = 0.15f;

        // Disable the scene's directional light (indoor hall)
        var dirLight = GameObject.Find("Directional Light");
        if (dirLight != null) dirLight.SetActive(false);

        BuildLighting(root.transform);

        // ── 4. Reposition camera for a nice overview ─────────────────
        RepositionCamera();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[ExhibitionHallBuilder] Hall built successfully. Press Ctrl+S to save.");
    }

    // ────────────────────────────────────────────────────────────────
    // Architecture: floor, ceiling, 4 walls
    // ────────────────────────────────────────────────────────────────
    private static void BuildArchitecture(Transform parent)
    {
        GameObject arch = new GameObject("Architecture");
        arch.transform.SetParent(parent, false);

        // Floor
        CreateBox(arch.transform, "Floor",
                  new Vector3(0, -Thick * 0.5f, 0),
                  new Vector3(Width, Thick, Depth),
                  new Color(0.82f, 0.80f, 0.76f));  // warm light grey

        // Ceiling
        CreateBox(arch.transform, "Ceiling",
                  new Vector3(0, Height + Thick * 0.5f, 0),
                  new Vector3(Width, Thick, Depth),
                  new Color(0.95f, 0.95f, 0.95f));

        // Back wall (+Z)
        CreateBox(arch.transform, "Wall_Back",
                  new Vector3(0, Height * 0.5f,  Depth * 0.5f + Thick * 0.5f),
                  new Vector3(Width, Height, Thick),
                  new Color(0.90f, 0.88f, 0.85f));

        // Front wall (−Z)
        CreateBox(arch.transform, "Wall_Front",
                  new Vector3(0, Height * 0.5f, -Depth * 0.5f - Thick * 0.5f),
                  new Vector3(Width, Height, Thick),
                  new Color(0.90f, 0.88f, 0.85f));

        // Right wall (+X)
        CreateBox(arch.transform, "Wall_Right",
                  new Vector3( Width * 0.5f + Thick * 0.5f, Height * 0.5f, 0),
                  new Vector3(Thick, Height, Depth + Thick * 2f),
                  new Color(0.88f, 0.86f, 0.83f));

        // Left wall (−X)
        CreateBox(arch.transform, "Wall_Left",
                  new Vector3(-Width * 0.5f - Thick * 0.5f, Height * 0.5f, 0),
                  new Vector3(Thick, Height, Depth + Thick * 2f),
                  new Color(0.88f, 0.86f, 0.83f));
    }

    // ────────────────────────────────────────────────────────────────
    // Single exhibit: pedestal + cube placeholder
    // ────────────────────────────────────────────────────────────────
    private static void BuildExhibit(Transform parent, string name,
                                     Vector3 position, Color color)
    {
        GameObject exhibit = new GameObject(name);
        exhibit.transform.SetParent(parent, false);
        exhibit.transform.localPosition = position;

        // Pedestal
        CreateBox(exhibit.transform, "Pedestal",
                  new Vector3(0, PedestalScale.y * 0.5f, 0),
                  PedestalScale,
                  new Color(0.75f, 0.74f, 0.72f));

        // Placeholder cube (represents the future exhibit object)
        CreateBox(exhibit.transform, "PlaceholderCube",
                  new Vector3(0, PedestalScale.y + ExhibitScale.y * 0.5f, 0),
                  ExhibitScale,
                  color);
    }

    // ────────────────────────────────────────────────────────────────
    // Lighting: ceiling spotlights aimed at each exhibit,
    //           plus soft fill lights along the walls
    // ────────────────────────────────────────────────────────────────
    private static void BuildLighting(Transform parent)
    {
        GameObject lightRoot = new GameObject("Lighting");
        lightRoot.transform.SetParent(parent, false);

        // ── Spotlights over exhibits ──
        // Exhibit A at (-2, 0,  2.5)
        CreateSpotlight(lightRoot.transform, "Spot_ExhibitA",
                        new Vector3(-2f, SpotHeight,  ExhibitZ),
                        new Vector3(80f, 0f, 0f),    // tilt toward −Z
                        SpotColor, SpotIntensity, SpotRange, SpotAngle);

        CreateSpotlight(lightRoot.transform, "Spot_ExhibitA_Side",
                        new Vector3(-3.5f, SpotHeight - 0.5f,  ExhibitZ - 0.5f),
                        new Vector3(70f, 30f, 0f),
                        SpotColor, SpotIntensity * 0.5f, SpotRange * 0.7f, SpotAngle - 5f);

        // Exhibit B at (2, 0, -2.5)
        CreateSpotlight(lightRoot.transform, "Spot_ExhibitB",
                        new Vector3( 2f, SpotHeight, -ExhibitZ),
                        new Vector3(80f, 0f, 0f),
                        SpotColor, SpotIntensity, SpotRange, SpotAngle);

        CreateSpotlight(lightRoot.transform, "Spot_ExhibitB_Side",
                        new Vector3( 3.5f, SpotHeight - 0.5f, -ExhibitZ + 0.5f),
                        new Vector3(70f, -30f, 0f),
                        SpotColor, SpotIntensity * 0.5f, SpotRange * 0.7f, SpotAngle - 5f);

        // ── Soft ambient fill (point lights at corners, very dim) ──
        float cx = Width  * 0.5f - 0.5f;
        float cz = Depth  * 0.5f - 0.5f;
        Color fillColor = new Color(0.7f, 0.75f, 1.0f);   // cool fill
        float fillIntensity = 1.5f;
        float fillRange     = 6f;

        CreatePointLight(lightRoot.transform, "Fill_NE", new Vector3( cx, 3f,  cz), fillColor, fillIntensity, fillRange);
        CreatePointLight(lightRoot.transform, "Fill_NW", new Vector3(-cx, 3f,  cz), fillColor, fillIntensity, fillRange);
        CreatePointLight(lightRoot.transform, "Fill_SE", new Vector3( cx, 3f, -cz), fillColor, fillIntensity, fillRange);
        CreatePointLight(lightRoot.transform, "Fill_SW", new Vector3(-cx, 3f, -cz), fillColor, fillIntensity, fillRange);

        // ── Ceiling track rail indicator (thin boxes) ──
        GameObject rail = new GameObject("LightRail");
        rail.transform.SetParent(lightRoot.transform, false);
        CreateBox(rail.transform, "Rail_A",
                  new Vector3(-2f, Height - 0.05f, 0f),
                  new Vector3(0.1f, 0.08f, Depth - 1f),
                  Color.black);
        CreateBox(rail.transform, "Rail_B",
                  new Vector3( 2f, Height - 0.05f, 0f),
                  new Vector3(0.1f, 0.08f, Depth - 1f),
                  Color.black);
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────
    private static GameObject CreateBox(Transform parent, string name,
                                        Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;

        // Assign a unique material instance with the desired color
        var renderer = go.GetComponent<Renderer>();
        Material mat = new Material(GetDefaultMaterial());
        mat.color = color;
        renderer.sharedMaterial = mat;

        // Make it static for lightmapping potential
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.BatchingStatic);

        return go;
    }

    private static void CreateSpotlight(Transform parent, string name,
                                        Vector3 localPos, Vector3 eulerAngles,
                                        Color color, float intensity,
                                        float range, float spotAngle)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition    = localPos;
        go.transform.localEulerAngles = eulerAngles;

        Light light = go.AddComponent<Light>();
        light.type           = LightType.Spot;
        light.color          = color;
        light.intensity      = intensity;
        light.range          = range;
        light.spotAngle      = spotAngle;
        light.innerSpotAngle = spotAngle * SpotInnerRatio;
        light.shadows        = LightShadows.Soft;
        light.shadowStrength = 0.8f;
        light.renderMode     = LightRenderMode.ForcePixel;
        light.lightmapBakeType = LightmapBakeType.Realtime;
    }

    private static void CreatePointLight(Transform parent, string name,
                                         Vector3 localPos, Color color,
                                         float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        Light light = go.AddComponent<Light>();
        light.type        = LightType.Point;
        light.color       = color;
        light.intensity   = intensity;
        light.range       = range;
        light.shadows     = LightShadows.None;
        light.lightmapBakeType = LightmapBakeType.Realtime;
    }

    private static void RepositionCamera()
    {
        var camObj = GameObject.Find("Main Camera");
        if (camObj == null) return;

        // Isometric-ish overview position from outside front
        camObj.transform.position    = new Vector3(0f, 4f, -12f);
        camObj.transform.eulerAngles = new Vector3(18f, 0f, 0f);
    }

    // Returns the URP / built-in default unlit-ish opaque material
    private static Material GetDefaultMaterial()
    {
        // URP Lit shader
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null) return new Material(urpLit);

        // Fallback: Standard
        Shader standard = Shader.Find("Standard");
        if (standard != null) return new Material(standard);

        return new Material(Shader.Find("Diffuse"));
    }
}
