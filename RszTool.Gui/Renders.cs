using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;


namespace RszTool.Gui
{
    public static class Renders
    {
        public static void RenderRszInstance(RszInstance instance, string? prefix = null)
        {
            if (ImGui.TreeNode(prefix != null ? $"{prefix}{instance.Name}" : instance.Name))
            {
                var fields = instance.Fields;
                if (instance.RSZUserData == null)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        ImGui.Text(field.name);
                        if (field.array)
                        {
                            var array = (List<object>)instance.Values[i];
                            ImGui.Indent();
                            for (int j = 0; j < array.Count; j++)
                            {
                                if (field.IsReference)
                                {
                                    RenderRszInstance((RszInstance)array[j], $"{j}: ");
                                }
                                else
                                {
                                    ImGui.Text($"{j}: ");
                                    ImGui.SameLine();
                                    var item = array[j];
                                    RenderRszField(field, ref item, out bool changed);
                                    if (changed) array[j] = item;
                                }
                            }
                            ImGui.Unindent();
                        }
                        else if (field.IsReference)
                        {
                            RenderRszInstance((RszInstance)instance.Values[i]);
                        }
                        else
                        {
                            ImGui.SameLine();
                            RenderRszField(field, ref instance.Values[i], out _);
                        }
                    }
                }
                ImGui.TreePop();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns>changed</returns>
        public static void RenderRszField(RszField field, ref object value, out bool changed)
        {
            changed = false;
            var label = field.original_type;
            ImGui.SetNextItemWidth(-400);
            switch (field.type)
            {
                case RszFieldType.S32:
                {
                    int intValue = (int)value;
                    ImGui.InputInt(label, ref intValue);
                    if (changed) value = intValue;
                    break;
                }
                case RszFieldType.U32:
                    unsafe
                    {
                        var typeValue = (uint)value;
                        changed = ImGui.InputScalar(label, ImGuiDataType.U32, (IntPtr)(&typeValue));
                        if (changed) { value = typeValue; }
                    }
                    break;
                case RszFieldType.S64:
                    unsafe
                    {
                        var typeValue = (long)value;
                        changed = ImGui.InputScalar(label, ImGuiDataType.S64, (IntPtr)(&typeValue));
                        if (changed) { value = typeValue; }
                    }
                    break;
                case RszFieldType.U64:
                    unsafe
                    {
                        var typeValue = (ulong)value;
                        changed = ImGui.InputScalar(label, ImGuiDataType.U64, (IntPtr)(&typeValue));
                        if (changed) { value = typeValue; }
                    }
                    break;
                case RszFieldType.F32:
                {
                    float floatValue = (float)value;
                    ImGui.InputFloat(label, ref floatValue);
                    if (changed) value = floatValue;
                    break;
                }
                case RszFieldType.F64:
                {
                    double doubleValue = (double)value;
                    ImGui.InputDouble(label, ref doubleValue);
                    if (changed) value = doubleValue;
                    break;
                }
                case RszFieldType.Bool:
                {
                    bool boolValue = (bool)value;
                    changed = ImGui.Checkbox(label, ref boolValue);
                    if (changed) value = boolValue;
                    break;
                }
                case RszFieldType.S8:
                    unsafe
                    {
                        var typeValue = (sbyte)value;
                        changed = ImGui.InputScalar(label, ImGuiDataType.S8, (IntPtr)(&typeValue));
                        if (changed) { value = typeValue; }
                    }
                    break;
                case RszFieldType.U8:
                    unsafe
                    {
                        var typeValue = (byte)value;
                        changed = ImGui.InputScalar(label, ImGuiDataType.U8, (IntPtr)(&typeValue));
                        if (changed) { value = typeValue; }
                    }
                    break;
                case RszFieldType.S16:
                    unsafe
                    {
                        var typeValue = (short)value;
                        changed = ImGui.InputScalar(label, ImGuiDataType.S16, (IntPtr)(&typeValue));
                        if (changed) { value = typeValue; }
                    }
                    break;
                case RszFieldType.U16:
                    unsafe
                    {
                        var typeValue = (ushort)value;
                        changed = ImGui.InputScalar(label, ImGuiDataType.U16, (IntPtr)(&typeValue));
                        if (changed) { value = typeValue; }
                    }
                    break;
                case RszFieldType.Data:
                    break;
                case RszFieldType.Mat4:
                    break;
                case RszFieldType.Vec2:
                case RszFieldType.Float2:
                {
                    Vector2 vector2 = (Vector2)value;
                    changed = ImGui.InputFloat2(label, ref vector2);
                    if (changed) { value = vector2; }
                    break;
                }
                case RszFieldType.Vec3:
                case RszFieldType.Float3:
                {
                    Vector3 vector3 = (Vector3)value;
                    changed = ImGui.InputFloat3(label, ref vector3);
                    if (changed) { value = vector3; }
                    break;
                }
                case RszFieldType.Vec4:
                case RszFieldType.Float4:
                {
                    Vector4 vector4 = (Vector4)value;
                    changed = ImGui.InputFloat4(label, ref vector4);
                    if (changed) { value = vector4; }
                    break;
                }
                case RszFieldType.OBB:
                    break;
                case RszFieldType.Guid:
                case RszFieldType.GameObjectRef:
                {
                    Guid guid = (Guid)value;
                    string guidStr = guid.ToString();
                    changed = ImGui.InputText(label, ref guidStr, 32);
                    if (changed) { value = Guid.Parse(guidStr); }
                    break;
                }
                case RszFieldType.Color:
                {
                    break;
                }
                case RszFieldType.Range:
                {
                    Vector2 vector2 = (Vector2)(via.Range)value;
                    changed = ImGui.InputFloat2(label, ref vector2);
                    if (changed) { value = (via.Range)vector2; }
                    break;
                }
                case RszFieldType.Quaternion:
                {
                    Quaternion vector4 = (Quaternion)value;
                    changed = ImGui.InputFloat4(label, ref Unsafe.As<Quaternion, Vector4>(ref vector4));
                    if (changed) { value = vector4; }
                    break;
                }
                case RszFieldType.Sphere:
                    break;
            }
        }
    }
}
