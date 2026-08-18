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
        Console.WriteLine("ASSETS TOOLS TEXTURE IL INSPECTOR");
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
        Console.WriteLine();

        if (!File.Exists(dllPath))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("DLL tidak ditemukan.");
            Console.WriteLine();

            Console.WriteLine(
                "Current Directory: " +
                Directory.GetCurrentDirectory());

            return;
        }

        try
        {
            Assembly asm =
                Assembly.LoadFrom(dllPath);

            Console.WriteLine("Assembly:");
            Console.WriteLine(asm.FullName);

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine("MENCARI METHOD PENTING");
            Console.WriteLine("======================================");

            Type[] types;

            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types =
                    ex.Types
                      .Where(t => t != null)
                      .Cast<Type>()
                      .ToArray();

                Console.WriteLine();
                Console.WriteLine("WARNING: beberapa type gagal dimuat.");

                foreach (Exception loaderError in
                         ex.LoaderExceptions ?? Array.Empty<Exception>())
                {
                    Console.WriteLine(
                        loaderError?.Message);
                }
            }

            // ========================================================
            // 1. TAMPILKAN SEMUA METHOD YANG BERKAITAN DENGAN
            //    DECODE / ASTC / TEXTURE
            // ========================================================

            foreach (Type type in types
                     .OrderBy(t => t.FullName))
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

                foreach (MethodInfo method in methods)
                {
                    string text =
                        (type.FullName ?? "") +
                        " " +
                        method.Name;

                    if (text.IndexOf(
                            "Decode",
                            StringComparison.OrdinalIgnoreCase) >= 0
                        ||
                        text.IndexOf(
                            "ASTC",
                            StringComparison.OrdinalIgnoreCase) >= 0
                        ||
                        text.IndexOf(
                            "Texture",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            "[METHOD] " +
                            type.FullName +
                            "." +
                            method.Name);

                        PrintSignature(method);
                    }
                }
            }

            // ========================================================
            // 2. CARI DecodeManaged SECARA KHUSUS
            // ========================================================

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine("DECODEMANAGED IL");
            Console.WriteLine("======================================");

            Type textureFileType =
                types.FirstOrDefault(
                    t =>
                        t.FullName ==
                        "AssetsTools.NET.Texture.TextureFile");

            if (textureFileType == null)
            {
                Console.WriteLine(
                    "TextureFile tidak ditemukan.");

                return;
            }

            MethodInfo decodeManaged =
                textureFileType.GetMethod(
                    "DecodeManaged",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance);

            if (decodeManaged == null)
            {
                Console.WriteLine(
                    "DecodeManaged tidak ditemukan.");

                return;
            }

            Console.WriteLine();
            Console.WriteLine(
                "FOUND:");

            PrintSignature(decodeManaged);

            DumpIL(
                decodeManaged);

            // ========================================================
            // 3. CARI SEMUA METHOD YANG NAMANYA ASTC
            // ========================================================

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine("ASTC METHODS IL");
            Console.WriteLine("======================================");

            foreach (Type type in types
                     .OrderBy(t => t.FullName))
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

                foreach (MethodInfo method in methods)
                {
                    if (method.Name.IndexOf(
                            "ASTC",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            "--------------------------------------");

                        PrintSignature(method);

                        DumpIL(method);
                    }
                }
            }

            // ========================================================
            // 4. CARI METHOD YANG MENGANDUNG "READ"
            //    DAN BERHUBUNGAN DENGAN TEXTURE DECODER
            // ========================================================

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine("DECODER METHODS");
            Console.WriteLine("======================================");

            foreach (Type type in types
                     .OrderBy(t => t.FullName))
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

                foreach (MethodInfo method in methods)
                {
                    string name =
                        method.Name;

                    if (
                        name.IndexOf(
                            "Read",
                            StringComparison.OrdinalIgnoreCase) >= 0
                        ||
                        name.IndexOf(
                            "Decode",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string full =
                            (type.FullName ?? "") +
                            "." +
                            name;

                        if (
                            full.IndexOf(
                                "Texture",
                                StringComparison.OrdinalIgnoreCase) >= 0
                            ||
                            full.IndexOf(
                                "ASTC",
                                StringComparison.OrdinalIgnoreCase) >= 0
                            ||
                            full.IndexOf(
                                "Decoder",
                                StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine(
                                "[DECODER] " +
                                full);

                            PrintSignature(method);
                        }
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine("INSPEKSI SELESAI");
            Console.WriteLine("======================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("ERROR:");
            Console.WriteLine(ex);
        }
    }

    // ================================================================
    // METHOD SIGNATURE
    // ================================================================

    static void PrintSignature(
        MethodInfo method)
    {
        Console.WriteLine(
            "Return: " +
            method.ReturnType.FullName);

        Console.WriteLine(
            "Static: " +
            method.IsStatic);

        Console.WriteLine(
            "Visibility: " +
            GetVisibility(method));

        Console.WriteLine(
            "Parameters:");

        foreach (ParameterInfo p
                 in method.GetParameters())
        {
            Console.WriteLine(
                "  " +
                p.Position +
                ": " +
                p.ParameterType.FullName +
                " " +
                p.Name);
        }
    }

    // ================================================================
    // VISIBILITY
    // ================================================================

    static string GetVisibility(
        MethodInfo method)
    {
        if (method.IsPublic)
            return "public";

        if (method.IsPrivate)
            return "private";

        if (method.IsFamily)
            return "protected";

        if (method.IsAssembly)
            return "internal";

        return "other";
    }

    // ================================================================
    // IL DUMPER
    // ================================================================

    static void DumpIL(
        MethodInfo method)
    {
        Console.WriteLine();
        Console.WriteLine(
            "----- IL START -----");

        MethodBody? body;

        try
        {
            body =
                method.GetMethodBody();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "GetMethodBody gagal: " +
                ex.Message);

            return;
        }

        if (body == null)
        {
            Console.WriteLine(
                "Method tidak memiliki IL body.");

            Console.WriteLine(
                "Kemungkinan native / abstract / external.");

            Console.WriteLine(
                "----- IL END -----");

            return;
        }

        byte[] il =
            body.GetILAsByteArray()
            ?? Array.Empty<byte>();

        Console.WriteLine(
            "IL Length: " +
            il.Length);

        Console.WriteLine();

        Module module =
            method.Module;

        int position = 0;

        while (position < il.Length)
        {
            int offset =
                position;

            ushort value =
                il[position++];

            OpCode opcode;

            if (value == 0xFE)
            {
                if (position >= il.Length)
                    break;

                ushort second =
                    il[position++];

                short twoByte =
                    (short)(
                        0xFE00 |
                        second);

                if (!TwoByteOpCodes.TryGetValue(
                        twoByte,
                        out opcode))
                {
                    Console.WriteLine(
                        offset.ToString("X4") +
                        ": UNKNOWN");

                    continue;
                }
            }
            else
            {
                if (!OneByteOpCodes.TryGetValue(
                        (short)value,
                        out opcode))
                {
                    Console.WriteLine(
                        offset.ToString("X4") +
                        ": UNKNOWN");

                    continue;
                }
            }

            object? operand =
                null;

            try
            {
                operand =
                    ReadOperand(
                        opcode,
                        il,
                        ref position,
                        module);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    offset.ToString("X4") +
                    ": " +
                    opcode.Name +
                    " [operand error: " +
                    ex.Message +
                    "]");

                break;
            }

            Console.WriteLine(
                offset.ToString("X4") +
                ": " +
                opcode.Name +
                (
                    operand != null
                        ? " " + operand
                        : ""
                ));
        }

        Console.WriteLine(
            "----- IL END -----");
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
                int intValue =
                    BitConverter.ToInt32(
                        il,
                        position);

                position += 4;

                return intValue;

            case OperandType.InlineI8:
                long longValue =
                    BitConverter.ToInt64(
                        il,
                        position);

                position += 8;

                return longValue;

            case OperandType.ShortInlineR:
                float floatValue =
                    BitConverter.ToSingle(
                        il,
                        position);

                position += 4;

                return floatValue;

            case OperandType.InlineR:
                double doubleValue =
                    BitConverter.ToDouble(
                        il,
                        position);

                position += 8;

                return doubleValue;

            case OperandType.ShortInlineBrTarget:
                {
                    sbyte delta =
                        (sbyte)il[position++];

                    int target =
                        position + delta;

                    return
                        "IL_" +
                        target.ToString("X4");
                }

            case OperandType.InlineBrTarget:
                {
                    int delta =
                        BitConverter.ToInt32(
                            il,
                            position);

                    position += 4;

                    int target =
                        position + delta;

                    return
                        "IL_" +
                        target.ToString("X4");
                }

            case OperandType.ShortInlineVar:
                return
                    "var_" +
                    il[position++];

            case OperandType.InlineVar:
                ushort varIndex =
                    BitConverter.ToUInt16(
                        il,
                        position);

                position += 2;

                return
                    "var_" +
                    varIndex;

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
                        MethodBase? target =
                            module.ResolveMethod(
                                token);

                        if (target != null)
                            return
                                target.DeclaringType?.FullName +
                                "." +
                                target.Name;
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
                            return
                                field.DeclaringType?.FullName +
                                "." +
                                field.Name;
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
                                type.FullName;
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
                                member.Name;
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

                        int target =
                            basePosition +
                            delta;

                        targets.Add(
                            "IL_" +
                            target.ToString("X4"));
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

        FieldInfo[] fields =
            typeof(OpCodes).GetFields(
                BindingFlags.Public |
                BindingFlags.Static);

        foreach (FieldInfo field in fields)
        {
            if (field.GetValue(null)
                is OpCode opcode)
            {
                short value =
                    (short)opcode.Value;

                if ((value & 0xFF00) == 0)
                    result[value] =
                        opcode;
            }
        }

        return result;
    }

    static Dictionary<short, OpCode>
        BuildTwoByteOpCodes()
    {
        Dictionary<short, OpCode> result =
            new Dictionary<short, OpCode>();

        FieldInfo[] fields =
            typeof(OpCodes).GetFields(
                BindingFlags.Public |
                BindingFlags.Static);

        foreach (FieldInfo field in fields)
        {
            if (field.GetValue(null)
                is OpCode opcode)
            {
                short value =
                    (short)opcode.Value;

                if ((value & 0xFF00) == 0xFE00)
                    result[value] =
                        opcode;
            }
        }

        return result;
    }
}
