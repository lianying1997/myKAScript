using System;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;
using KodakkuAssist.Module.Draw;
using Dalamud.Utility.Numerics;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using Dalamud.Memory.Exceptions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommons;
using System.Linq;
using ImGuiNET;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using KodakkuAssist.Module.GameOperate;

namespace LYScriptNamespace
{
    [ScriptType(name: "极火天王绘图", territorys: [1096], guid: "0fdb383b-0aad-44a1-8af8-3261264b308a", version: "0.0.0.1", author: "LY")]

    public class RubicanteEx
    {
        // 第ph次炼狱魔法阵记录
        int ph = 0;
        uint rotcnt = 0;
        uint innerCircleId = 0;
        uint innerRotateDir = 0;
        uint outerRotateDir = 0;
        uint middleCircleId = 0;
        uint outerCircleId = 0;
        List<uint> magicCircleType = new();      // 红色半场刀魔法阵
        List<float> magicCircleRotation = new();     // 蓝色扇形魔法阵
        float innerRotation = 0f;

        Vector4 color_lightOrange = new Vector4(1f, 1f, 0.2f, 1f);
        Vector4 color_lightpurple = new Vector4(1f, 0.2f, 1f, 1f);

        public void Init(ScriptAccessory accessory)
        {
            // 初始化删除所有画图与标点
            accessory.Method.RemoveDraw(@".*");
            ph = 0;
            innerCircleId = 0;
            middleCircleId = 0;
            outerCircleId = 0;
            innerRotateDir = 0;
            outerRotateDir = 0;
            innerRotation = 0;
            magicCircleType = new();
            magicCircleRotation = new();
            rotcnt = 0;
            //accessory.Method.MarkClear();
        }

        // 炼狱魔阵：狱炎
        [ScriptMethod(name: "炼狱朱炎-切换", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:33001"])]
        public void OrdealPurgationPhase(Event @event, ScriptAccessory accessory)
        {
            ph = ph + 1;
            accessory.Method.TextInfo($"第{ph}次炼狱魔法阵……", 2000, true);
        }

        // 32044 火焰流         AOE
        // 32551 炼狱招来       场地效果
        // 31934 魔法阵展开     场地效果
        // 33001 炼狱朱炎       体操，无效果

        // StatusAdd: ID|StackCount|Param|Duration|Name
        // 542  单根线  该魔法阵是内圈  环ID F529
        // 543  V字
        // 544  两根线
        // 545  猫猫头  该魔法阵是中圈  环ID F528
        // 546  八方线  该魔法阵是外圈  环ID F527

        // 32804    旋转技能        对F527、F529 无意义
        // 32805    随机旋转        一次魔法阵结束后随机旋转，有意义
        // 32506    旋转技能        外圈八个魔法阵旋转
        // 564  顺时针旋转 F527     外环
        // 562  逆时针旋转 F529     内环

        // ----O-----
        // |
        // P   B
        // O:100,80
        // P:80,100
        // B:100,100

        // Param 542 为单根线
        // ?可能不需要
        [ScriptMethod(name: "魔法阵-单根线", eventType: EventTypeEnum.StatusAdd, eventCondition: ["Param:542"], userControl: false)]
        public void FieryExpiationSingleLine(Event @event, ScriptAccessory accessory)
        {
            // accessory.Log.Debug($"ph：{ph}");
            // accessory.Log.Debug($"捕捉到单根线魔法阵…");
            // 第1次炼狱魔法阵
            if (ph == 0)
            {
                // 此处记录下内环魔法阵
                if (ParseObjectId(@event["TargetId"], out var tid))
                {
                    innerCircleId = tid;
                    // accessory.Log.Debug($"记录下内圈魔法阵：{innerCircleId}");
                }
            }
        }

        // Param 545 为猫猫头
        // ?可能不需要
        [ScriptMethod(name: "魔法阵-猫猫头", eventType: EventTypeEnum.StatusAdd, eventCondition: ["Param:545"], userControl: false)]
        public void FieryExpiationCat(Event @event, ScriptAccessory accessory)
        {
            // accessory.Log.Debug($"ph：{ph}");
            // accessory.Log.Debug($"捕捉到猫猫头魔法阵…");
            // 第1次炼狱魔法阵
            if (ph == 0)
            {
                // 此处记录下内环魔法阵
                if (ParseObjectId(@event["TargetId"], out var tid))
                {
                    middleCircleId = tid;
                    // accessory.Log.Debug($"记录下中圈魔法阵：{middleCircleId}");
                }
            }
        }

        // Param 546 为八方线
        // ?可能不需要
        [ScriptMethod(name: "魔法阵-八方线", eventType: EventTypeEnum.StatusAdd, eventCondition: ["Param:546"], userControl: false)]
        public void FieryExpiationOut(Event @event, ScriptAccessory accessory)
        {
            // accessory.Log.Debug($"ph：{ph}");
            // accessory.Log.Debug($"捕捉到八方线魔法阵…");
            // 第1次炼狱魔法阵
            if (ph == 0)
            {
                // 此处记录下内环魔法阵
                if (ParseObjectId(@event["TargetId"], out var tid))
                {
                    outerCircleId = tid;
                    // accessory.Log.Debug($"记录下外圈魔法阵：{outerCircleId}");
                }
            }
        }

        [ScriptMethod(name: "魔法阵-结束后随机旋转", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:32805"], userControl: false)]
        public void FieryExpiationEndRotate(Event @event, ScriptAccessory accessory)
        {
            
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            if (!float.TryParse(@event["SourceRotation"], out var srot)) return;
            // 第2次炼狱魔法阵前
            if (ph == 1)
            {
                if (sid == innerCircleId) {
                    innerRotation = srot;
                    accessory.Log.Debug($"捕捉到随机旋转的魔法阵…{srot}");
                }
            }
        }

        // 捕捉魔法阵
        // 0x15759 = 87897, 0x15760 = 87904
        [ScriptMethod(name: "捕捉外部魔法阵", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["SourceDataId:regex:^(15759|15760)$"], userControl: false)]
        public void FetchMagicCircle(Event @event, ScriptAccessory accessory)
        {
            // if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            if (!ParseObjectId(@event["SourceDataId"], out var sdid)) return;
            if(!float.TryParse(@event["SourceRotation"], out var srot)) return;
            if (sdid == 87897) {
                magicCircleType.Add(2); // Blue, Fan
                magicCircleRotation.Add(srot);
            } else {
                magicCircleType.Add(1); // Red, Rect
                magicCircleRotation.Add(srot);
            }
            // accessory.Log.Debug($"捕捉到外部魔法阵{(sdid == 87897 ? "红" : "蓝")}，位于{srot}…");
        }

        // ^(?:(?!骑士|贤者|战士|机工士|绘灵法师|召唤师|钐镰客|赤魔法师|吟游诗人|烈日巴哈姆特|红宝石兽|黄宝石泰坦|绿宝石迦楼罗|红宝石伊弗利特|后式自走人偶).)*$ 不匹配
        // 00000001 内环，00000003 外环
        // 131073 顺，2097168 逆
        [ScriptMethod(name: "魔法阵-顺逆旋转", eventType: EventTypeEnum.EnvControl, eventCondition: ["Index:regex:^(0000000[13])$"], userControl: false)]
        public void FieryExpiation(Event @event, ScriptAccessory accessory)
        {
            accessory.Log.Debug($"From FieryExpiation: ph = {ph}");

            if (!ParseObjectId(@event["Id"], out var id)) return;
            if (@event["Index"] == "00000001") {
                innerRotateDir = id;
                // accessory.Log.Debug($"捕捉到内圈旋转方向：{(innerRotateDir == 131073 ? "顺时针" : "逆时针")}");
                rotcnt = rotcnt + 1;
            } else if (@event["Index"] == "00000003") {
                outerRotateDir = id;
                // accessory.Log.Debug($"捕捉到外圈旋转方向：{(outerRotateDir == 131073 ? "顺时针" : "逆时针")}");
                rotcnt = rotcnt + 1;
            }

            accessory.Log.Debug($"rotcnt = {rotcnt}");

            // 第1次炼狱魔法阵
            if (ph == 0 && rotcnt == 2)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = $"魔法阵1绘图";
                dp.TargetPosition = new(100, 0, 100);
                dp.Color = color_lightOrange;
                dp.DestoryAt = 22000;
                // 找到施法点
                var rotateRad = (innerRotateDir == 131073 ? float.Pi / 2 : -1 * float.Pi/2);
                var point = RotatePoint(new(120, 0, 100), new(100, 0, 100), rotateRad);
                dp.Position = point;
                // accessory.Log.Debug($"RotateRad:{rotateRad}");
                // 找到源头类型，外圈是顺时针转，所以源头要往逆时针找
                var outRotateRad = rotateRad + (outerRotateDir == 131073 ? -1 * float.Pi / 4 : float.Pi / 4);
                int closestIndex = FindClosestAngleIndex(magicCircleRotation, outRotateRad);
                // accessory.Log.Debug($"outRotateRad:{outRotateRad}");
                // accessory.Log.Debug($"The closest angle index is: {closestIndex}, which is {magicCircleRotation[closestIndex]}, {magicCircleType[closestIndex]}");

                
                if (magicCircleType[closestIndex] == 1) {
                    // 是半场刀
                    dp.Scale = new(40, 20);
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Rect, dp);
                } else {
                    // 是扇形
                    dp.Scale = new(40);
                    dp.Radian = float.Pi / 3;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Fan, dp);
                }                               
                rotcnt = 0;
            }
            
            else if (ph == 1 && rotcnt == 3) 
            {
                // TODO 找不到内圈魔法阵的方向……
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = $"魔法阵2绘图 必是半场刀";
                dp.Position = new(100, 0, 100);
                dp.Color = color_lightOrange;
                dp.DestoryAt = 22000;
                dp.Scale = new(5, 15);
                accessory.Log.Debug($"innerRotation:{innerRotation}");
                // dp.Rotation = innerRotation;
                dp.Rotation = 0.79f;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Rect, dp);

                dp.Rotation = 0.79f + float.Pi/2;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Rect, dp);

                // dp.Rotation = innerRotation + float.Pi/2;
                // accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Rect, dp);
            }
        
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

        private Vector3 RotatePoint(Vector3 point, Vector3 center, float theta)
        {
            Vector2 v2 = new(point.X - center.X, point.Z - center.Z);
            var xNew = center.X + (point.X - center.X) * Math.Cos(theta) - (point.Z - center.Z) * Math.Sin(theta);
            var zNew = center.Z + (point.X - center.X) * Math.Sin(theta) + (point.Z - center.Z) * Math.Sin(theta);
            return new((float)xNew, (float)point.Y, (float)zNew);
        }

        private static int FindClosestAngleIndex(List<float> angles, float targetAngle)
        {
            int closestIndex = -1;
            float minDifference = float.MaxValue;

            for (int i = 0; i < angles.Count; i++)
            {
                float diff = Math.Abs(angles[i] - targetAngle);

                // 确保角度差值小于等于pi（360度情况下，0和2pi]角度是相同的）
                if (diff > Math.PI)
                {
                    diff = (float)(2 * Math.PI - diff);
                }

                if (diff < minDifference)
                {
                    minDifference = diff;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

    }
}

// 测试用

