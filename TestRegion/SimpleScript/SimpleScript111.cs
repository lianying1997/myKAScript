// using System;
// using KodakkuAssist.Module.GameEvent;
// using KodakkuAssist.Script;
// using KodakkuAssist.Module.GameEvent.Struct;
// using KodakkuAssist.Module.Draw;
// using System.Windows.Forms;
// using System.Numerics;
// using Dalamud.Utility.Numerics;
using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Network.Structures.InfoProxy;
using Newtonsoft.Json;
using Dalamud.Utility.Numerics;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;

using KodakkuAssist.Script;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Module.Draw;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.GeneratedSheets2;

namespace MyScriptNamespace
{
    /// <summary>
    /// name and version affect the script name and version number displayed in the user interface.
    /// territorys specifies the regions where this trigger is effective. If left empty, it will be effective in all regions.
    /// Classes with the same GUID will be considered the same trigger. Please ensure your GUID is unique and does not conflict with others.
    /// </summary>
    [ScriptType(name: "SimpleScript114", territorys: [],guid: "ec092b1a-f5f3-4c9f-bb12-6f5feac50b33",version:"0.0.0.123")]
    public class SimpleScript
    {
        /// <summary>
        /// note will be displayed to the user as a tooltip.
        /// </summary>
        [UserSetting(note:"This is a test Property")]
        public int prop1 { get; set; } = 1;
        [UserSetting("Another Test Property")]
        public bool prop2 { get; set; } = false;
        int n = 0;
        Vector4 color_pink = new Vector4(1f, 0.5f, 0f, 10f);
        /// <summary>
        /// This method is called at the start of each battle reset.
        /// If this method is not defined, the program will execute an empty method.
        /// </summary>
        public void Init(ScriptAccessory accessory)
        {
            n = 0;
        }

        private static bool ParseObjectId(string? idStr, out uint id)
        {
            id = 0;
            if (string.IsNullOrEmpty(idStr)) return false;
            try
            {
                var idStr2 = idStr.Replace("0x", "");
                id = uint.Parse(idStr2, System.Globalization.NumberStyles.HexNumber);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// name is the name of the method as presented to the user.
        /// eventType is the type of event that triggers this method.
        /// eventCondition is an array of strings specifying the properties that the event must have,
        /// in the format name:value,For specific details, please refer to the GameEvent of the plugin.
        /// userControl set to false will make the method not be shown to the user
        /// and cannot be disabled by the user.
        /// Please note, the method will be executed asynchronously.
        /// </summary>
        /// <param name="event">The event instance that triggers this method.</param>
        /// <param name="accessory">Pass the instances of methods and data that might be needed.</param>
        [ScriptMethod(name: "Test StartCasting",eventType: EventTypeEnum.StartCasting,eventCondition: ["ActionId:133"])]
        public void PrintInfo(Event @event, ScriptAccessory accessory)
        {
            n++;
            accessory.Method.SendChat($"{@event["SourceId"]} {n}-th use the Medica II");
        }

        [ScriptMethod(name: "Test Draw", eventType: EventTypeEnum.ActionEffect,eventCondition: ["ActionId:124"])]
        public void DrawCircle(Event @event, ScriptAccessory accessory)
        {
            var prop = accessory.Data.GetDefaultDrawProperties();
            prop.Owner = Convert.ToUInt32(@event["SourceId"],16);
            prop.DestoryAt = 2000;
            // prop.Color = accessory.Data.DefaultSafeColor;

            // prop.Color = new(1f,0f,1f,1f);
            prop.Color = color_pink;
            // prop.Offset = new(0, 0.02f, 0);
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, prop);
        }

        [ScriptMethod(name: "Test 鱼料救疗触发击退", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(131|135)$"])]
        public void DrawLine(Event @event, ScriptAccessory accessory)
        {
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Scale = new(1.5f, 16);
            dp.Color = color_pink;
            dp.Owner = accessory.Data.Me;
            if (ParseObjectId(@event["SourceId"], out var sid))
            {
                dp.TargetObject = sid;
            }
            dp.Rotation = float.Pi;
            dp.DestoryAt = 10000;

            var aid = @event["ActionId"];
            if (aid == "131") { dp.Name = "击退标志123"; accessory.Method.TextInfo("击退标志1", 1000, true); }
            else if (aid == "135") { dp.Name = "击退标志2";accessory.Method.TextInfo("击退标志2", 1000, true); }
            dp.Offset = new(0, 0, 0);
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);
            accessory.Log.Debug($"AutoDraw 检测到击退标志");
            // accessory.Method.TextInfo(accessory.Data.DefaultDangerColor.GetType().ToString(),1500,true);
        }

        [ScriptMethod(name: "Test 防击退删掉画图", eventType: EventTypeEnum.ActionEffect,eventCondition: ["ActionId:regex:^(7559|7548|7389|7562|7430|136)$"])]
        // 沉稳咏唱|亲疏自行|原初的解放
        public void RemoveLine(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["SourceId"], out var id)) return;
            if (id == accessory.Data.Me)
            {
                accessory.Method.RemoveDraw("^击退标志\\d+$");
                accessory.Method.TextInfo("检测到防击退",2000,true);
                accessory.Log.Debug($"AutoDraw 检测到防击退");
            }
        }

        [ScriptMethod(name: "Unconfigurable Method", eventType: EventTypeEnum.ActionEffect,eventCondition: ["ActionId:124"],userControl:false)]
        public void UnconfigurableMethod(Event @event, ScriptAccessory accessory)
        {
            accessory.Log.Debug($"The unconfigurable method has been triggered.");
        }

        [ScriptMethod(name: "TestStatusAdd", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:158"])]
        public void TestStatusAdd(Event @event, ScriptAccessory accessory)
        {
            var dp = accessory.Data.GetDefaultDrawProperties();
            accessory.Log.Debug($"通过StatusID捕捉到再生");
            dp.Name = $"魔法阵1-扇形绘图";
            dp.Scale = new(50);
            // dp.Owner = accessory.Data.Me;
            dp.Position = new(0, 0, 100);
            dp.Radian = float.Pi / 18 * 6;
            dp.Color = color_pink;
            dp.Rotation = float.Pi;
            dp.DestoryAt = 20000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
        }

        [ScriptMethod(name: "TestStatusAdd2", eventType: EventTypeEnum.StatusAdd, eventCondition: ["Param:0"])]
        public void TestStatusAdd2(Event @event, ScriptAccessory accessory)
        {
            accessory.Log.Debug($"通过Param捕捉到再生");
        }
    }
}

