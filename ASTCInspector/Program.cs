using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("======================================");
        Console.WriteLine("ASTC INSPECTOR");
        Console.WriteLine("======================================");

        string dllPath =
            args.Length > 0
                ? args[0]
                : Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "AssetsTools.NET.Texture.dll");

        Console.WriteLine();
        Console.WriteLine("DLL:");
        Console.WriteLine(dllPath);

        if (!File.Exists(dllPath))
        {
            Console.WriteLine();
            Console.WriteLine("ERROR: DLL tidak ditemukan.");
            return;
        }

        string reportPath =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "astc-report.txt");

        try
        {
            Assembly asm =
                Assembly.LoadFrom(dllPath);

            Type[] types;

            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types
                    .Where(t => t != null)
                    .Cast<Type>()
                    .ToArray();
            }

            using StreamWriter report =
                new StreamWriter(reportPath, false);

            Write(report,
                "======================================");
            Write(report,
                "ASSETS TOOLS.NET TEXTURE ASTC REPORT");
            Write(report,
                "======================================");

            Write(report, "");
            Write(report, "DLL:");
            Write(report, dllPath);

            Write(report, "");
            Write(report, "ASSEMBLY:");
            Write(report, asm.FullName ?? "");

            // ========================================================
            // TEXTURE FORMAT ENUM
            // ========================================================

            Write(report, "");
            Write(report,
                "======================================");
            Write(report,
                "TEXTURE FORMAT ENUM");
            Write(report,
                "======================================");

            Type? textureFormat =
                types.FirstOrDefault(
                    t =>
                        t.FullName ==
                        "AssetsTools.NET.Texture.TextureFormat");

            Dictionary<int, string> enumValues =
                new Dictionary<int, string>();

            if (textureFormat != null &&
                textureFormat.IsEnum)
            {
                foreach (object value
                         in Enum.GetValues(textureFormat))
                {
                    int number =
                        Convert.ToInt32(value);

                    string name =
                        Enum.GetName(
                            textureFormat,
                            value) ?? value.ToString();

                    enumValues[number] = name;

                    Write(
                        report,
                        $"{number} = {name}");
                }
            }
            else
            {
                Write(
                    report,
                    "TextureFormat enum tidak ditemukan.");
            }

            // ========================================================
            // ASTC ENUM
            // ========================================================

            Write(report, "");
            Write(report,
                "======================================");
            Write(report,
                "ASTC FORMATS");
            Write(report,
                "======================================");

            List<KeyValuePair<int, string>> astcFormats =
                enumValues
                    .Where(x =>
                        x.Value.IndexOf(
                            "ASTC",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(x => x.Key)
                    .ToList();

            if (astcFormats.Count == 0)
            {
                Write(
                    report,
                    "Tidak ditemukan enum ASTC.");
            }
            else
            {
                foreach (var item in astcFormats)
                {
                    Write(
                        report,
                        $"{item.Key} = {item.Value}");
                }
            }

            // ========================================================
            // DECODE MANAGED
            // ========================================================

            Write(report, "");
            Write(report,
                "======================================");
            Write(report,
                "DECODEMANAGED");
            Write(report,
                "======================================");

            Type? textureFile =
                types.FirstOrDefault(
                    t =>
                        t.FullName ==
                        "AssetsTools.NET.Texture.TextureFile");

            if (textureFile == null)
            {
                Write(
                    report,
                    "TextureFile tidak ditemukan.");

                Finish(report, reportPath);
                return;
            }

            MethodInfo? decodeManaged =
                textureFile.GetMethod(
                    "DecodeManaged",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static);

            if (decodeManaged == null)
            {
                Write(
                    report,
                    "DecodeManaged tidak ditemukan.");

                Finish(report, reportPath);
                return;
            }

            Write(
                report,
                "Method: " +
                decodeManaged.DeclaringType?.FullName +
                "." +
                decodeManaged.Name);

            Write(
                report,
                "Return: " +
                decodeManaged.ReturnType.FullName);

            Write(
                report,
                "Static: " +
                decodeManaged.IsStatic);

            Write(report, "Parameters:");

            foreach (ParameterInfo parameter
                     in decodeManaged.GetParameters())
            {
                Write(
                    report,
                    $"  {parameter.Position}: " +
                    $"{parameter.ParameterType.FullName} " +
                    $"{parameter.Name}");
            }

            // ========================================================
            // SWITCH + METHOD TOKEN
            // ========================================================

            Write(report, "");
            Write(report,
                "======================================");
            Write(report,
                "DECODEMANAGED SWITCH");
            Write(report,
                "======================================");

            MethodBody? body =
                decodeManaged.GetMethodBody();

            if (body == null)
            {
                Write(
                    report,
                    "DecodeManaged tidak mempunyai IL body.");

                Finish(report, reportPath);
                return;
            }

            byte[] il =
                body.GetILAsByteArray()
                ?? Array.Empty<byte>();

            Write(
                report,
                "IL Length: " +
                il.Length);

            Module module =
                decodeManaged.Module;

            int position = 0;

            while (position < il.Length)
            {
                int offset = position;

                OpCode opcode;

                ushort value =
                    il[position++];

                if (value == 0xFE)
                {
                    if (position >= il.Length)
                        break;

                    ushort second =
                        il[position++];

                    short code =
                        (short)(
                            0xFE00 |
                            second);

                    if (!TwoByteOpCodes.TryGetValue(
                            code,
                            out opcode))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!OneByteOpCodes.TryGetValue(
                            (short)value,
                            out opcode))
                    {
                        continue;
                    }
                }

                object? operand = null;

                try
                {
                    operand =
                        ReadOperand(
                            opcode,
                            il,
                            ref position,
                            module);
                }
                catch
                {
                    break;
                }

                // Hanya tampilkan bagian penting:
                // switch
                // call
                // callvirt
                // method token
                if (opcode == OpCodes.Switch ||
                    opcode == OpCodes.Call ||
                    opcode == OpCodes.Callvirt)
                {
                    Write(
                        report,
                        $"{offset:X4}: {opcode.Name} {operand}");
                }
            }

            // ========================================================
            // SEMUA METHOD YANG MENGANDUNG ASTC
            // ========================================================

            Write(report, "");
            Write(report,
                "======================================");
            Write(report,
                "ASTC METHODS");
            Write(report,
                "======================================");

            bool foundAstcMethod = false;

            foreach (Type type in
                     types.OrderBy(
                         t => t.FullName))
            {
                MethodInfo[] methods;

                try
                {
                    methods =
                        type.GetMethods(
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Static |
                            BindingFlags.Instance |
                            BindingFlags.DeclaredOnly);
                }
                catch
                {
                    continue;
                }

                foreach (MethodInfo method
                         in methods)
                {
                    string fullName =
                        (type.FullName ?? "") +
                        "." +
                        method.Name;

                    if (fullName.IndexOf(
                            "ASTC",
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    foundAstcMethod = true;

                    Write(
                        report,
                        "");

                    Write(
                        report,
                        "[ASTC METHOD] " +
                        fullName);

                    Write(
                        report,
                        "Return: " +
                        method.ReturnType.FullName);

                    Write(
                        report,
                        "Static: " +
                        method.IsStatic);

                    Write(
                        report,
                        "Parameters:");

                    foreach (ParameterInfo parameter
                             in method.GetParameters())
                    {
                        Write(
                            report,
                            $"  {parameter.Position}: " +
                            $"{parameter.ParameterType.FullName} " +
                            $"{parameter.Name}");
                    }
                }
            }

            if (!foundAstcMethod)
            {
                Write(
                    report,
                    "Tidak ada method yang namanya mengandung ASTC.");
            }

            // ========================================================
            // DECODER METHODS
            // ========================================================

            Write(report, "");
            Write(report,
                "======================================");
            Write(report,
                "POTENTIAL DECODER METHODS");
            Write(report,
                "======================================");

            foreach (Type type in
                     types.OrderBy(
                         t => t.FullName))
            {
                MethodInfo[] methods;

                try
                {
                    methods =
                        type.GetMethods(
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Static |
                            BindingFlags.Instance |
                            BindingFlags.DeclaredOnly);
                }
                catch
                {
                    continue;
                }

                foreach (MethodInfo method
                         in methods)
                {
                    string name =
                        method.Name;

                    if (
                        name.IndexOf(
                            "Decode",
                            StringComparison.OrdinalIgnoreCase) >= 0
                        ||
                        name.IndexOf(
                            "Read",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string fullName =
                            (type.FullName ?? "") +
                            "." +
                            name;

                        if (
                            fullName.IndexOf(
                                "Decoder",
                                StringComparison.OrdinalIgnoreCase) >= 0
                            ||
                            fullName.IndexOf(
                                "ASTC",
                                StringComparison.OrdinalIgnoreCase) >= 0
                            ||
                            fullName.IndexOf(
                                "Texture",
                                StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Write(
                                report,
                                fullName);
                        }
                    }
                }
            }

            // ========================================================
            // SELESAI
            // ========================================================

            Write(report, "");
            Write(report,
                "======================================");
            Write(report,
                "INSPEKSI SELESAI");
            Write(report,
                "======================================");

            report.Flush();

            Console.WriteLine();
            Console.WriteLine(
                "REPORT BERHASIL DIBUAT:");

            Console.WriteLine(
                reportPath);

            Console.WriteLine();
            Console.WriteLine(
                "Silakan ambil artifact:");
            Console.WriteLine(
                "astc-report.txt");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("ERROR:");
            Console.WriteLine(ex);
        }
    }

    // ================================================================
    // WRITE
    // ================================================================

    static void Write(
        StreamWriter report,
        string text)
    {
        Console.WriteLine(text);
        report.WriteLine(text);
    }

    // ================================================================
    // FINISH
    // ================================================================

    static void Finish(
        StreamWriter report,
        string path)
    {
        report.Flush();

        Console.WriteLine();
        Console.WriteLine(
            "REPORT:");
        Console.WriteLine(path);
    }

    // ================================================================
    // OPERAND READER
    // ================================================================

    static object? ReadOperand(
        OpCode opcode,
        byte[] il,
        ref int position,
        Module module)
    {
        switch (opcode.OperandType)
        {
            case OperandType.InlineNone:
                return null;

            case OperandType.ShortInlineI:
                return
                    (sbyte)il[position++];

            case OperandType.InlineI:
                {
                    int value =
                        BitConverter.ToInt32(
                            il,
                            position);

                    position += 4;

                    return value;
                }

            case OperandType.InlineI8:
                {
                    long value =
                        BitConverter.ToInt64(
                            il,
                            position);

                    position += 8;

                    return value;
                }

            case OperandType.ShortInlineR:
                {
                    float value =
                        BitConverter.ToSingle(
                            il,
                            position);

                    position += 4;

                    return value;
                }

            case OperandType.InlineR:
                {
                    double value =
                        BitConverter.ToDouble(
                            il,
                            position);

                    position += 8;

                    return value;
                }

            case OperandType.ShortInlineBrTarget:
                {
                    sbyte delta =
                        (sbyte)il[position++];

                    return
                        "IL_" +
                        (position + delta)
                        .ToString("X4");
                }

            case OperandType.InlineBrTarget:
                {
                    int delta =
                        BitConverter.ToInt32(
                            il,
                            position);

                    position += 4;

                    return
                        "IL_" +
                        (position + delta)
                        .ToString("X4");
                }

            case OperandType.ShortInlineVar:
                return
                    "var_" +
                    il[position++];

            case OperandType.InlineVar:
                {
                    ushort index =
                        BitConverter.ToUInt16(
                            il,
                            position);

                    position += 2;

                    return
                        "var_" +
                        index;
                }

            case OperandType.InlineString:
                {
                    int token =
                        BitConverter.ToInt32(
                            il,
                            position);

                    position += 4;

                    try
                    {
                        return
                            "\"" +
                            module.ResolveString(token) +
                            "\"";
                    }
                    catch
                    {
                        return
                            "stringToken=0x" +
                            token.ToString("X8");
                    }
                }

            case OperandType.InlineMethod:
                {
                    int token =
                        BitConverter.ToInt32(
                            il,
                            position);

                    position += 4;

                    try
                    {
                        MethodBase? method =
                            module.ResolveMethod(
                                token);

                        if (method != null)
                        {
                            return
                                method.DeclaringType?.FullName +
                                "." +
                                method.Name +
                                " [0x" +
                                token.ToString("X8") +
                                "]";
                        }
                    }
                    catch
                    {
                    }

                    return
                        "methodToken=0x" +
                        token.ToString("X8");
                }

            case OperandType.InlineField:
                {
                    int token =
                        BitConverter.ToInt32(
                            il,
                            position);

                    position += 4;

                    try
                    {
                        FieldInfo? field =
                            module.ResolveField(
                                token);

                        if (field != null)
                        {
                            return
                                field.DeclaringType?.FullName +
                                "." +
                                field.Name +
                                " [0x" +
                                token.ToString("X8") +
                                "]";
                        }
                    }
                    catch
                    {
                    }

                    return
                        "fieldToken=0x" +
                        token.ToString("X8");
                }

            case OperandType.InlineType:
                {
                    int token =
                        BitConverter.ToInt32(
                            il,
                            position);

                    position += 4;

                    try
                    {
                        Type? type =
                            module.ResolveType(
                                token);

                        if (type != null)
                            return
                                type.FullName +
                                " [0x" +
                                token.ToString("X8") +
                                "]";
                    }
                    catch
                    {
                    }

                    return
                        "typeToken=0x" +
                        token.ToString("X8");
                }

            case OperandType.InlineTok:
                {
                    int token =
                        BitConverter.ToInt32(
                            il,
                            position);

                    position += 4;

                    try
                    {
                        MemberInfo? member =
                            module.ResolveMember(
                                token);

                        if (member != null)
                            return
                                member.DeclaringType?.FullName +
                                "." +
                                member.Name +
                                " [0x" +
                                token.ToString("X8") +
                                "]";
                    }
                    catch
                    {
                    }

                    return
                        "token=0x" +
                        token.ToString("X8");
                }

            case OperandType.InlineSwitch:
                {
                    int count =
                        BitConverter.ToInt32(
                            il,
                            position);

                    position += 4;

                    int basePosition =
                        position +
                        count * 4;

                    List<string> targets =
                        new List<string>();

                    for (int i = 0;
                         i < count;
                         i++)
                    {
                        int delta =
                            BitConverter.ToInt32(
                                il,
                                position);

                        position += 4;

                        targets.Add(
                            "IL_" +
                            (basePosition + delta)
                            .ToString("X4"));
                    }

                    return
                        "[" +
                        string.Join(
                            ", ",
                            targets) +
                        "]";
                }

            default:
                return
                    "[unsupported operand]";
        }
    }

    // ================================================================
    // OPCODE TABLES
    // ================================================================

    static readonly Dictionary<short, OpCode>
        OneByteOpCodes =
            BuildOneByteOpCodes();

    static readonly Dictionary<short, OpCode>
        TwoByteOpCodes =
            BuildTwoByteOpCodes();

    static Dictionary<short, OpCode>
        BuildOneByteOpCodes()
    {
        Dictionary<short, OpCode> result =
            new Dictionary<short, OpCode>();

        foreach (FieldInfo field in
                 typeof(OpCodes).GetFields(
                     BindingFlags.Public |
                     BindingFlags.Static))
        {
            if (field.GetValue(null)
                is OpCode opcode)
            {
                short value =
                    opcode.Value;

                if ((value & 0xFF00) == 0)
                    result[value] = opcode;
            }
        }

        return result;
    }

    static Dictionary<short, OpCode>
        BuildTwoByteOpCodes()
    {
        Dictionary<short, OpCode> result =
            new Dictionary<short, OpCode>();

        foreach (FieldInfo field in
                 typeof(OpCodes).GetFields(
                     BindingFlags.Public |
                     BindingFlags.Static))
        {
            if (field.GetValue(null)
                is OpCode opcode)
            {
                short value =
                    opcode.Value;

                if ((value & 0xFF00) == unchecked((short)0xFE00))
                    result[value] = opcode;
            }
        }

        return result;
    }
}
