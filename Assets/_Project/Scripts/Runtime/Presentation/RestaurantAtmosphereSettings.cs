using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroStarRestaurant.Presentation
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Presentation/Atmosphere Settings")]
    public sealed class RestaurantAtmosphereSettings : ScriptableObject
    {
        [SerializeField] private Color _ambientSky = new Color(.5f, .52f, .515f);
        [SerializeField] private Color _ambientEquator = new Color(.43f, .455f, .44f);
        [SerializeField] private Color _ambientGround = new Color(.29f, .305f, .295f);
        [SerializeField, Range(0, 1)] private float _reflectionIntensity = .25f;
        [SerializeField] private Color _background = new Color(.035f, .045f, .06f);

        // A single evening preset, applied on activation/comparison; independent of GameTime.
        public void ApplyEnvironment(Camera camera)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = _ambientSky;
            RenderSettings.ambientEquatorColor = _ambientEquator;
            RenderSettings.ambientGroundColor = _ambientGround;
            RenderSettings.ambientIntensity = 1;
            RenderSettings.reflectionIntensity = _reflectionIntensity;
            RenderSettings.fog = false;
            camera.backgroundColor = _background;
        }
    }
}
