using System;
using UnityEngine;
using UnityEngine.UI;
using BrewedInk.CRT;

namespace CodeForge.UI
{
    [ExecuteAlways]
    public class CRTGameViewController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera arenaCamera;
        [SerializeField] private RawImage crtDisplay;

        [Header("Configuration")]
        [SerializeField] private bool enableCRT = true;
        [Tooltip("The CRT Material asset. Adjust scanlines, curvature, vignette, colors, etc. directly on this material in the Inspector!")]
        [SerializeField] private Material crtMaterial;
        [Tooltip("Optional preset for initializing or resetting CRT properties via context menu.")]
        [SerializeField] private CRTDataObject crtPreset;

        [Header("Quality / Resolution")]
        [Range(1, 4)]
        [SerializeField] private int downsampleFactor = 1;

        private RenderTexture arenaRenderTexture;
        private Material runtimeMaterial;
        private string lastValidationId;

        private static readonly int PropShowFrame = Shader.PropertyToID("_ShowFrame");
        private static readonly int PropScanlineCount = Shader.PropertyToID("_ScanlineCount");
        private static readonly int PropScanlineIntensity = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int PropScanlineSpeed = Shader.PropertyToID("_ScanlineSpeed");

        private static readonly int PropMaxColorsRed = Shader.PropertyToID("_MaxColorsRed");
        private static readonly int PropMaxColorsGreen = Shader.PropertyToID("_MaxColorsGreen");
        private static readonly int PropMaxColorsBlue = Shader.PropertyToID("_MaxColorsBlue");
        private static readonly int PropDitheringAmount = Shader.PropertyToID("_Spread");
        private static readonly int PropDitheringAmount8 = Shader.PropertyToID("_Spread8");
        private static readonly int PropVignette = Shader.PropertyToID("_VigSize");
        private static readonly int PropMonitorRoundness = Shader.PropertyToID("_BorderOutterRound");
        private static readonly int PropMonitorTexture = Shader.PropertyToID("_BorderTex");
        private static readonly int PropMonitorColor = Shader.PropertyToID("_BorderTint");
        private static readonly int PropInnerDarkness = Shader.PropertyToID("_BorderInnerDarkerAmount");
        private static readonly int PropInnerGlow = Shader.PropertyToID("_CrtGlowAmount");
        private static readonly int PropInnerReflectionRadius = Shader.PropertyToID("_CrtReflectionRadius");
        private static readonly int PropInnerReflectionCurve = Shader.PropertyToID("_CrtReflectionCurve");
        private static readonly int PropMonitorCurve = Shader.PropertyToID("_Curvature2");
        private static readonly int PropInnerCurve = Shader.PropertyToID("_Curvature");
        private static readonly int PropZoom = Shader.PropertyToID("_BorderZoom");
        private static readonly int PropInnerSizeX = Shader.PropertyToID("_BorderInnerSizeX");
        private static readonly int PropInnerSizeY = Shader.PropertyToID("_BorderInnerSizeY");
        private static readonly int PropOutterSizeX = Shader.PropertyToID("_BorderOutterSizeX");
        private static readonly int PropOutterSizeY = Shader.PropertyToID("_BorderOutterSizeY");
        private static readonly int PropColorScans = Shader.PropertyToID("_ColorScans");
        private static readonly int PropDesaturation = Shader.PropertyToID("_Desaturation");
        private static readonly int PropBrewedInkBayer4 = Shader.PropertyToID("_BrewedInk_Bayer4");
        private static readonly int PropBrewedInkBayer8 = Shader.PropertyToID("_BrewedInk_Bayer8");

        private static readonly float[] bayer4 = new float[] {
            0, 8, 2, 10,
            12, 4, 14, 6,
            3, 11, 1, 9,
            15, 7, 13, 5
        };

        private static readonly float[] bayer8 = new float[] {
            0, 32, 8, 40, 2, 34, 10, 42,
            48, 16, 56, 24, 50, 18, 58, 26,
            12, 44, 4, 36, 14, 46, 6, 38,
            60, 28, 52, 20, 62, 30, 54, 22,
            3, 35, 11, 43, 1, 33, 9, 41,
            51, 19, 59, 27, 49, 17, 57, 25,
            15, 47, 7, 39, 13, 45, 5, 37,
            63, 31, 55, 23, 61, 29, 53, 21
        };

        private void OnEnable()
        {
            if (crtPreset != null)
            {
                lastValidationId = crtPreset.validationId;
            }
            SetupReferences();
            UpdatePipeline();
        }

        private void OnDisable()
        {
            TeardownPipeline();
        }

        private void OnDestroy()
        {
            TeardownPipeline();
        }

        private void Update()
        {
            if (!enableCRT)
            {
                if (arenaRenderTexture != null) TeardownPipeline();
                return;
            }

            SetupReferences();
            EnsureRenderTexture();

            // Detect when user edits the preset ScriptableObject (e.g. Subtle.asset) in the Inspector
            if (crtPreset != null && !string.Equals(lastValidationId, crtPreset.validationId))
            {
                lastValidationId = crtPreset.validationId;
                ApplyPresetToMaterial();
            }

            UpdateMaterialProperties();
        }

        public void SetupReferences()
        {
            if (arenaCamera == null)
            {
                var camObj = GameObject.Find("ArenaCamera");
                if (camObj != null) arenaCamera = camObj.GetComponent<Camera>();
            }

#if UNITY_EDITOR
            if (crtMaterial == null)
            {
                crtMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/CRT-Free/Materials/CRTMaterial.mat");
            }
            if (crtPreset == null)
            {
                crtPreset = UnityEditor.AssetDatabase.LoadAssetAtPath<CRTDataObject>("Assets/CRT-Free/CRTs/Subtle.asset");
            }
#endif

            EnsureDisplayCamera();
            EnsurePlayerHealthPlateInArena();
            EnsureDisplayUI();
        }

        public void EnsurePlayerHealthPlateInArena()
        {
            var playerHealth = GameObject.Find("PlayerHealthPlate");
            if (playerHealth == null || arenaCamera == null) return;

            var arenaCanvasObj = GameObject.Find("ArenaUICanvas");
            if (arenaCanvasObj == null)
            {
                arenaCanvasObj = new GameObject("ArenaUICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }

            var canvas = arenaCanvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = arenaCamera;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 10;

            var scaler = arenaCanvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 672);
            scaler.matchWidthOrHeight = 0.5f;

            if (playerHealth.transform.parent != arenaCanvasObj.transform)
            {
                playerHealth.transform.SetParent(arenaCanvasObj.transform, false);

                var rt = playerHealth.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.04f, 0.83f);
                rt.anchorMax = new Vector2(0.48f, 0.96f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private void EnsureDisplayCamera()
        {
            var camObj = GameObject.Find("DisplayCamera");
            if (camObj == null)
            {
                camObj = new GameObject("DisplayCamera", typeof(Camera));
                var c = camObj.GetComponent<Camera>();
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f); // #14171C
                c.cullingMask = 0; // Nothing
                c.depth = -10;
                c.rect = new Rect(0f, 0f, 1f, 1f);
                c.targetTexture = null;
            }
        }

        private void EnsureDisplayUI()
        {
            if (crtDisplay != null) return;

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            var existing = canvas.transform.Find("GameViewCRT");
            if (existing != null)
            {
                crtDisplay = existing.GetComponent<RawImage>();
                return;
            }

            var displayObj = new GameObject("GameViewCRT", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            displayObj.transform.SetParent(canvas.transform, false);

            // Position behind overlays (like player health plate and popovers), but in front of canvas background
            displayObj.transform.SetSiblingIndex(2);

            var rt = displayObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.50f, 0.30f);
            rt.anchorMax = new Vector2(1.00f, 1.00f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            crtDisplay = displayObj.GetComponent<RawImage>();
            crtDisplay.raycastTarget = false;
        }

        private void EnsureRenderTexture()
        {
            if (crtDisplay == null || arenaCamera == null) return;

            var rt = crtDisplay.rectTransform;
            int width = Mathf.Max(320, Mathf.RoundToInt(rt.rect.width / Mathf.Max(1, downsampleFactor)));
            int height = Mathf.Max(240, Mathf.RoundToInt(rt.rect.height / Mathf.Max(1, downsampleFactor)));

            if (arenaRenderTexture == null || arenaRenderTexture.width != width || arenaRenderTexture.height != height)
            {
                if (arenaRenderTexture != null)
                {
                    arenaCamera.targetTexture = null;
                    arenaRenderTexture.Release();
                    DestroyImmediate(arenaRenderTexture);
                }

                arenaRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "CRT_ArenaRenderTexture",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                arenaRenderTexture.Create();

                arenaCamera.targetTexture = arenaRenderTexture;
                arenaCamera.rect = new Rect(0f, 0f, 1f, 1f);

                crtDisplay.texture = arenaRenderTexture;
            }

            if (arenaCamera.targetTexture != arenaRenderTexture)
            {
                arenaCamera.targetTexture = arenaRenderTexture;
                arenaCamera.rect = new Rect(0f, 0f, 1f, 1f);
            }

            if (crtDisplay.texture != arenaRenderTexture)
            {
                crtDisplay.texture = arenaRenderTexture;
            }
        }

        private void UpdateMaterialProperties()
        {
            if (crtDisplay == null) return;

            // Ensure global Bayer dithering arrays are available to the shader
            Shader.SetGlobalFloatArray(PropBrewedInkBayer4, bayer4);
            Shader.SetGlobalFloatArray(PropBrewedInkBayer8, bayer8);

            // Clean up any legacy runtime clone so crtMaterial is used directly
            if (runtimeMaterial != null)
            {
                DestroyImmediate(runtimeMaterial);
                runtimeMaterial = null;
            }

            // Assign crtMaterial directly to the display RawImage.
            // This ensures all tweaks in the Project/Inspector update the screen in real-time!
            if (crtMaterial != null && crtDisplay.material != crtMaterial)
            {
                crtDisplay.material = crtMaterial;
            }
        }

        [ContextMenu("Apply Preset To CRT Material")]
        public void ApplyPresetToMaterial()
        {
            if (crtMaterial == null || crtPreset == null || crtPreset.data == null) return;
            var data = crtPreset.data;

            crtMaterial.SetFloat(PropShowFrame, data.showFrame ? 1f : 0f);
            crtMaterial.SetFloat(PropScanlineCount, data.scanlineCount > 0 ? data.scanlineCount : 280f);
            crtMaterial.SetFloat(PropScanlineIntensity, data.scanlineIntensity);
            crtMaterial.SetFloat(PropScanlineSpeed, data.scanlineSpeed);

            crtMaterial.SetFloat(PropMaxColorsRed, data.maxColorChannels.red);
            crtMaterial.SetFloat(PropMaxColorsGreen, data.maxColorChannels.green);
            crtMaterial.SetFloat(PropMaxColorsBlue, data.maxColorChannels.blue);
            crtMaterial.SetFloat(PropDitheringAmount, data.dithering4);
            crtMaterial.SetFloat(PropDitheringAmount8, data.dithering8);
            crtMaterial.SetFloat(PropVignette, data.vignette);
            crtMaterial.SetFloat(PropMonitorRoundness, data.monitorRoundness);
            crtMaterial.SetFloat(PropInnerDarkness, 1f - data.innerMonitorDarkness);
            crtMaterial.SetFloat(PropInnerGlow, data.innerMonitorShine);
            crtMaterial.SetFloat(PropInnerReflectionRadius, data.innerMonitorShineRadius);
            crtMaterial.SetFloat(PropInnerReflectionCurve, data.innerMonitorShineCurve);
            crtMaterial.SetFloat(PropMonitorCurve, data.monitorCurve);
            crtMaterial.SetFloat(PropInnerCurve, data.innerCurve);
            crtMaterial.SetFloat(PropZoom, data.zoom);
            crtMaterial.SetFloat(PropInnerSizeX, data.monitorInnerSize.width);
            crtMaterial.SetFloat(PropInnerSizeY, data.monitorInnerSize.height);
            crtMaterial.SetFloat(PropDesaturation, data.maxColorChannels.greyScale);
            crtMaterial.SetFloat(PropOutterSizeX, data.monitorOutterSize.width);
            crtMaterial.SetFloat(PropOutterSizeY, data.monitorOutterSize.height);
            crtMaterial.SetVector(PropColorScans, new Vector4(
                data.colorScans.greenChannelMultiplier,
                data.colorScans.redBlueChannelMultiplier,
                data.colorScans.sizeMultiplier,
                1f
            ));
            crtMaterial.SetTexture(PropMonitorTexture, data.monitorTexture);
            crtMaterial.SetColor(PropMonitorColor, data.monitorColor);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(crtMaterial);
#endif
        }

        private void UpdatePipeline()
        {
            if (enableCRT)
            {
                SetupReferences();
                EnsureRenderTexture();
                UpdateMaterialProperties();
            }
            else
            {
                TeardownPipeline();
            }
        }

        private void TeardownPipeline()
        {
            if (arenaCamera != null)
            {
                arenaCamera.targetTexture = null;
                arenaCamera.rect = new Rect(0.50f, 0.30f, 0.50f, 0.70f);
            }

            if (arenaRenderTexture != null)
            {
                arenaRenderTexture.Release();
                DestroyImmediate(arenaRenderTexture);
                arenaRenderTexture = null;
            }

            if (runtimeMaterial != null)
            {
                DestroyImmediate(runtimeMaterial);
                runtimeMaterial = null;
            }

            if (crtDisplay != null)
            {
                crtDisplay.texture = null;
                crtDisplay.material = null;
            }
        }
    }
}
