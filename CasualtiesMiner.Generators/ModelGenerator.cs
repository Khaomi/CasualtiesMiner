using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CasualtiesMiner.Generators;

#pragma warning disable RS1035 // explicit Environment.NewLine usage; deliberate for readability

[Generator]
public sealed class ModelGenerator : IIncrementalGenerator
{
    private static readonly string[] Models =
    {
        "ItemInfo",
        "BlockInfo",
        "LiquidType",
        "TileInfo",
        "LiquidInfo",
        "LiquidItemInfo",
        "LiquidStack",
        "Recipe",
        "RecipeItem",
        "RecipeResult",
        "CraftingQuality",
        "BatteryInfo",
        "Color",
        "Recognition",
        "SleepQuality",
        "Language",
        "BuildingEntity",
        "ItemDrop"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        //if (!System.Diagnostics.Debugger.IsAttached)
        //{
        //    System.Diagnostics.Debugger.Launch();
        //}
#endif
        context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) =>
        {
            var unityAssembly = FindAssembly(compilation, "Assembly-CSharp");
            if (unityAssembly is null)
            {
                return;
            }

            var allTypes = GetAllTypes(unityAssembly.GlobalNamespace).ToList();

            var modelClasses = allTypes
                .Where(t => t.TypeKind == TypeKind.Class && !t.IsAbstract)
                .Where(t => MatchesModelName(t.Name))
                .ToList();

            var enumTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var modelClass in modelClasses)
            {
                foreach (var field in GetFields(modelClass))
                {
                    CollectEnumTypes(field.Type, enumTypes);
                }
            }

            foreach (var type in allTypes)
            {
                if (type.TypeKind != TypeKind.Enum)
                {
                    continue;
                }

                if (MatchesModelName(type.Name))
                {
                    enumTypes.Add(type);
                }
            }

            foreach (var enumType in enumTypes.OrderBy(t => t.ToDisplayString()))
            {
                spc.AddSource(ToSourceFileName(enumType), SourceText.From(GenerateEnum(enumType), Encoding.UTF8));
            }

            foreach (var modelClass in modelClasses)
            {
                var fields = GetFields(modelClass).ToList();
                var code = GeneratePartial(modelClass, fields);
                spc.AddSource(ToSourceFileName(modelClass), SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    private static bool MatchesModelName(string typeName)
    {
        foreach (var name in Models)
        {
            if (typeName.Equals(name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IAssemblySymbol FindAssembly(Compilation compilation, string assemblyName)
    {
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
                && assembly.Name == assemblyName)
            {
                return assembly;
            }
        }

        return null;
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
    {
        foreach (var member in root.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol ns:
                    foreach (var type in GetAllTypes(ns))
                    {
                        yield return type;
                    }

                    break;

                case INamedTypeSymbol type:
                    foreach (var nested in GetNestedTypes(type))
                    {
                        yield return nested;
                    }

                    break;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
    {
        yield return type;

        foreach (var nested in type.GetTypeMembers())
        {
            foreach (var inner in GetNestedTypes(nested))
            {
                yield return inner;
            }
        }
    }

    private static IEnumerable<IFieldSymbol> GetFields(INamedTypeSymbol type)
    {
        bool ShouldKeepType(ITypeSymbol type)
        {
            var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
            if (name.StartsWith("global::UnityEngine"))
            {
                return name switch
                {
                    "global::UnityEngine.Color" => true,
                    _ => false
                };
            }

            return true;
        }
        
        return type.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic && !f.IsImplicitlyDeclared)
            .Where(f => ShouldKeepType(f.Type))
            .Where(f => f.DeclaredAccessibility == Accessibility.Public || IsStringArray(f.Type));
    }

    private static bool IsStringArray(ITypeSymbol type)
    {
        return type is IArrayTypeSymbol arrayType
            && arrayType.ElementType.SpecialType == SpecialType.System_String;
    }

    private static void CollectEnumTypes(ITypeSymbol type, HashSet<INamedTypeSymbol> sink)
    {
        switch (type)
        {
            case INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType:
                sink.Add(enumType);
                break;

            case INamedTypeSymbol named when named.TypeArguments.Length > 0:
                foreach (var typeArgument in named.TypeArguments)
                {
                    CollectEnumTypes(typeArgument, sink);
                }

                break;

            case IArrayTypeSymbol arrayType:
                CollectEnumTypes(arrayType.ElementType, sink);
                break;
        }
    }

    private static string GenerateEnum(INamedTypeSymbol enumType)
    {
        const string ns = "CasualtiesMiner.Shared.Models;";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine();
        sb.Append("namespace ").AppendLine(ns);
        sb.AppendLine();

        sb.Append("public enum ").AppendLine(enumType.Name);
        sb.AppendLine("{");

        var members = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsStatic && f.HasConstantValue && f.Name != "value__")
            .ToList();

        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            sb.Append("    ").Append(member.Name);

            if (member.ConstantValue != null)
            {
                sb.Append(" = ").Append(FormatConstant(member.ConstantValue));
            }

            sb.AppendLine(i == members.Count - 1 ? string.Empty : ",");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GeneratePartial(INamedTypeSymbol type, IReadOnlyList<IFieldSymbol> fields)
    {
        const string ns = "CasualtiesMiner.Shared.Models;";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.Append("namespace ").AppendLine(ns);
        sb.AppendLine();
        sb.Append("public partial class ").AppendLine(type.Name);
        sb.AppendLine("{");

        foreach (var field in fields)
        {
            if (TryGetDelegateSignature(field.Type, out var delegateSignature))
            {
                sb.Append("    // Unity delegate: ").AppendLine(delegateSignature);
            }

            sb.Append("    public ")
                .Append(MapFieldType(field.Type))
                .Append(' ')
                .Append(field.Name)
                .AppendLine(";");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string ToSourceFileName(INamedTypeSymbol type)
    {
        var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace(".", "_");

        return $"{fullName}.g.cs";
    }

    private static bool TryGetDelegateSignature(ITypeSymbol type, out string signature)
    {
        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Delegate } delegateType)
        {
            signature = string.Empty;
            return false;
        }

        var invoke = delegateType.DelegateInvokeMethod;
        if (invoke is null)
        {
            signature = delegateType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return true;
        }

        var returnType = FormatTypeName(invoke.ReturnType);
        var parameters = string.Join(", ",
            invoke.Parameters.Select(p => $"{FormatTypeName(p.Type)} {p.Name}"));

        var delegateName = delegateType.Name;
        if (delegateType.ContainingType != null)
        {
            delegateName = $"{delegateType.ContainingType.Name}.{delegateName}";
        }

        signature = $"public delegate {returnType} {delegateName}({parameters});";
        return true;
    }

    private static string MapFieldType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { TypeKind: TypeKind.Delegate })
        {
            // Dumper stores decompiled method source lines for delegate assignments.
            return "string[]?";
        }

        return FormatTypeName(type);
    }

    private static string FormatTypeName(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
        {
            return enumType.Name;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } genericType)
        {
            var typeName = genericType.Name;
            var tickIndex = typeName.IndexOf('`');
            if (tickIndex >= 0)
            {
                typeName = typeName.Substring(0, tickIndex);
            }

            var typeArguments = string.Join(", ",
                genericType.TypeArguments.Select(FormatTypeName));

            return $"{typeName}<{typeArguments}>";
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return $"{FormatTypeName(arrayType.ElementType)}[]";
        }

        return type.SpecialType switch
        {
            SpecialType.System_String => "string",
            SpecialType.System_Boolean => "bool",
            SpecialType.System_Int32 => "int",
            SpecialType.System_Single => "float",
            SpecialType.System_Double => "double",
            SpecialType.System_Byte => "byte",
            SpecialType.System_Int16 => "short",
            SpecialType.System_Int64 => "long",
            _ => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) switch
            {
                "UnityEngine.Color" => "Color",
                _ => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            }
        };
    }

    private static string FormatConstant(object value)
    {
        return value switch
        {
            bool boolean => boolean ? "true" : "false",
            string text => $"\"{text}\"",
            char character => $"'{character}'",
            float number => number.ToString(CultureInfo.InvariantCulture) + "f",
            double number => number.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0"
        };
    }
}
