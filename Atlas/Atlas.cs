namespace Atlas
{
    using GameHelper;
    using GameHelper.Plugin;
    using GameHelper.Utils;
    using ImGuiNET;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Numerics;
    using System.Text;
    using System.Xml;

    public sealed class Atlas : PCore<AtlasSettings>
    {
        private string SettingPathname => Path.Join(this.DllDirectory, "config", "settings.txt");

        public override void OnDisable()
        {
        }

        public override void OnEnable(bool isGameOpened)
        {
            if (File.Exists(this.SettingPathname))
            {
                var content = File.ReadAllText(this.SettingPathname);
                var serializerSettings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
                this.Settings = JsonConvert.DeserializeObject<AtlasSettings>(content, serializerSettings);
            }
        }

        public override void SaveSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this.SettingPathname));
            var settingsData = JsonConvert.SerializeObject(this.Settings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(this.SettingPathname, settingsData);
        }

        public override void DrawSettings()
        {
            ImGui.Checkbox($"##DrawWhenForeground", ref Settings.DrawWhenForeground);
            ImGui.SameLine();
            ImGui.Text("Hide Atlas when game is in the background");
            ImGui.Checkbox($"##HideCompletedMaps", ref Settings.HideCompletedMaps);
            ImGui.SameLine();
            ImGui.Text($"Hide Completed Maps");
            ImGui.SameLine();
            ColorSwatch($"##DefaultBackgroundColor", ref Settings.DefaultBackgroundColor);
            ImGui.SameLine();
            ImGui.Text($"Default Background Color");
            ImGui.SameLine();
            ColorSwatch($"##DefaultFontColor", ref Settings.DefaultFontColor);
            ImGui.SameLine();
            ImGui.Text($"Default Font Color");

            ImGui.SliderFloat("##ScaleMultiplier", ref Settings.ScaleMultiplier, 0, 10.0f);
            ImGui.SameLine();
            ImGui.Text($"Scale Multiplier");

            ImGui.InputText($"##MapGroupName", ref Settings.GroupNameInput, 256);
            ImGui.SameLine();
            if (ImGui.Button("Add new map group"))
            {
                Settings.MapGroups.Add(new MapGroupSettings(Settings.GroupNameInput, Settings.DefaultBackgroundColor, Settings.DefaultFontColor));
                Settings.GroupNameInput = string.Empty;
            }

            for (int i = 0; i < Settings.MapGroups.Count; i++)
            {
                var mapGroup = Settings.MapGroups[i];

                if (ImGui.CollapsingHeader($"{mapGroup.Name}##MapGroup{i}"))
                {
                    ColorSwatch($"##MapGroupBackgroundColor{i}", ref mapGroup.BackgroundColor);
                    ImGui.SameLine();
                    ImGui.Text($"Background Color");
                    ImGui.SameLine();
                    ColorSwatch($"##MapGroupFontColor{i}", ref mapGroup.FontColor);
                    ImGui.SameLine();
                    ImGui.Text($"Font Color");

                    for (int j = 0; j < mapGroup.Maps.Count; j++)
                    {
                        var mapName = mapGroup.Maps[j];

                        if (ImGui.InputText($"##MapName{i}-{j}", ref mapName, 256)) mapGroup.Maps[j] = mapName;

                        ImGui.SameLine();
                        if (ImGui.Button($"Delete##MapNameDelete{i}-{j}"))
                        {
                            mapGroup.Maps.RemoveAt(j);
                            break;
                        }
                    }

                    if (ImGui.Button($"Add new map##AddNewMap{i}"))
                        mapGroup.Maps.Add(string.Empty);
                }
            }
        }

        public override void DrawUI()
        {
            if (ProcessHandle == 0)
                ProcessHandle = ProcessMemoryUtilities.Managed.NativeWrapper.OpenProcess(ProcessMemoryUtilities.Native.ProcessAccessFlags.Read, (int)Core.Process.Pid);

            var drawList = ImGui.GetBackgroundDrawList();
            var atlasNodes = GetAtlasNodes();

            if (Settings.DrawWhenForeground && !Core.Process.Foreground)
            {
                return;
            }

            foreach (var atlasNode in atlasNodes)
            {
                var mapName = atlasNode.MapName;
                if (string.IsNullOrWhiteSpace(mapName)) continue;
                if (Settings.HideCompletedMaps && (atlasNode.IsCompleted || (mapName.EndsWith("Citadel") && atlasNode.IsFailedAttempt))) continue;

                var textSize = ImGui.CalcTextSize(mapName);
                var backgroundColor = Settings.MapGroups.Find(group => group.Maps.Contains(mapName))?.BackgroundColor ?? Settings.DefaultBackgroundColor;
                var fontColor = Settings.MapGroups.Find(group => group.Maps.Contains(mapName))?.FontColor ?? Settings.DefaultFontColor;

                var mapPosition = atlasNode.Position * Settings.ScaleMultiplier;
                var padding = new Vector2(5, 2);
                var bgPos = new Vector2(mapPosition.X - padding.X, mapPosition.Y - padding.Y);
                var bgSize = new Vector2(textSize.X + padding.X * 2, textSize.Y + padding.Y * 2);

                drawList.AddRectFilled(bgPos, bgPos + bgSize, ImGuiHelper.Color(backgroundColor));
                drawList.AddText(mapPosition, ImGuiHelper.Color(fontColor), mapName);
            }
        }

        private static void ColorSwatch(string label, ref System.Numerics.Vector4 color)
        {

            if (ImGui.ColorButton(label, color))
                ImGui.OpenPopup(label);

            if (ImGui.BeginPopup(label))
            {
                ImGui.ColorPicker4(label, ref color);
                ImGui.EndPopup();
            }

            if (!label.StartsWith("##"))
            {
                ImGui.SameLine();
                ImGui.Text(label);
            }
        }

        // ==================================================
        // TEMPORARY CODE
        // ==================================================
        public static IntPtr ProcessHandle { get; set; }

        public static T Read<T>(IntPtr address) where T : unmanaged
        {
            if (address == IntPtr.Zero) return default;

            T result = default;
            ProcessMemoryUtilities.Managed.NativeWrapper.ReadProcessMemory(ProcessHandle, address, ref result);
            return result;
        }

        public static string ReadWideString(nint address, int stringLength)
        {
            if (address == IntPtr.Zero || stringLength <= 0) return string.Empty;

            byte[] result = new byte[stringLength * 2];
            ProcessMemoryUtilities.Managed.NativeWrapper.ReadProcessMemoryArray(ProcessHandle, address, result);
            return Encoding.Unicode.GetString(result).Split('\0')[0];
        }

        private IEnumerable<AtlasNode> GetAtlasNodes()
        {
            var nodes = new List<AtlasNode>();

            var uiElement = Atlas.Read<UiElement>(Core.States.InGameStateObject.GameUi.Address);
            uiElement = uiElement.GetChild(24);
            uiElement = uiElement.GetChild(0);
            uiElement = uiElement.GetChild(6);

            if (!uiElement.IsVisible || uiElement.FirstChild == IntPtr.Zero || uiElement.Length > 10000) return nodes;

            for (var i = 0; i < uiElement.Length; i++)
                nodes.Add(uiElement.GetAtlasNode(i));

            return nodes;
        }
    }
}
