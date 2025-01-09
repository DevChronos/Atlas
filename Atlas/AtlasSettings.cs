namespace Atlas
{
    using GameHelper.Plugin;
    using System.Collections.Generic;
    using System.Numerics;

    public sealed class AtlasSettings : IPSettings
    {
        public Vector4 DefaultBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.4f);
        public Vector4 DefaultFontColor = new(1.0f, 1.0f, 1.0f, 1.0f);
        public bool HideCompletedMaps = true;
        public float ScaleMultiplier = 1.0f;
        public List<MapGroupSettings> MapGroups = [];
        public string GroupNameInput = string.Empty;
        public bool DrawWhenForeground = true;

        public AtlasSettings()
        {
            var citadels = new MapGroupSettings("Citadels", new Vector4(1.0f, 0.0f, 0.0f, 0.4f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            citadels.Maps.Add("The Copper Citadel");
            citadels.Maps.Add("The Iron Citadel");
            citadels.Maps.Add("The Stone Citadel");

            var good = new MapGroupSettings("Good", new Vector4(0.0f, 1.0f, 0.0f, 0.4f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            good.Maps.Add("Burial Bog");
            good.Maps.Add("Creek");
            good.Maps.Add("Rustbowl");
            good.Maps.Add("Sandspit");
            good.Maps.Add("Savannah");
            good.Maps.Add("Steaming Springs");
            good.Maps.Add("Steppe");
            good.Maps.Add("Wetlands");
            good.Maps.Add("Willow");

            MapGroups.Add(citadels);
            MapGroups.Add(good);
        }
    }

    public class MapGroupSettings(string name, Vector4 backgroundColor, Vector4 fontColor)
    {
        public string Name = name;
        public Vector4 BackgroundColor = backgroundColor;
        public Vector4 FontColor = fontColor;
        public List<string> Maps = [];
        public string MapNameInput = string.Empty;
    }
}
