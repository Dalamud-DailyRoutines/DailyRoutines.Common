using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using OmenTools.Dalamud;

namespace DailyRoutines.Common.Module.Compilers;

public sealed class ModuleCompiler
{
    private static readonly CSharpCompilationOptions CompilationOptions = new
    (
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
        concurrentBuild: true,
        allowUnsafe: true
    );

    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default;

    private readonly ConcurrentDictionary<string, Lazy<byte[]>>    compiledAssemblyCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<Assembly?>> loadedAssemblyCache   = new(StringComparer.Ordinal);
    private readonly ConcurrentBag<ModuleLoadContext>              loadedContexts        = [];

    private readonly Lazy<ImmutableArray<MetadataReference>> referenceListLazy = new(BuildReferenceList, LazyThreadSafetyMode.ExecutionAndPublication);

    private ImmutableArray<MetadataReference> ReferenceList => referenceListLazy.Value;

    public void UnloadAll()
    {
        compiledAssemblyCache.Clear();
        loadedAssemblyCache.Clear();

        while (loadedContexts.TryTake(out var context))
            TryUnloadContext(context);
    }

    public Assembly? Load(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var assemblyBytes = Compile(code);
        return assemblyBytes.Length > 0 ? Load(assemblyBytes) : null;
    }

    public Assembly? Load(byte[] assembly)
    {
        if (assembly.Length == 0)
            return null;

        var assemblyHash = ComputeSHA256Hash(assembly);
        var cacheEntry = loadedAssemblyCache.GetOrAdd
        (
            assemblyHash,
            _ => new(() => LoadCore(assembly), LazyThreadSafetyMode.ExecutionAndPublication)
        );

        try
        {
            var loadedAssembly = cacheEntry.Value;
            if (loadedAssembly != null)
                return loadedAssembly;
        }
        catch
        {
            // ignored
        }

        loadedAssemblyCache.TryRemove(assemblyHash, out _);
        return null;
    }

    public static IReadOnlyList<Type> GetDerivedTypes<T>(Assembly? assembly) where T : class
    {
        if (assembly == null)
            return [];

        var        baseType = typeof(T);
        List<Type> result   = [];

        try
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract                &&
                    baseType.IsAssignableFrom(type) &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                    result.Add(type);
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var type in ex.Types)
            {
                if (type == null                     ||
                    type.IsAbstract                  ||
                    !baseType.IsAssignableFrom(type) ||
                    type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                result.Add(type);
            }

            DLog.Error("[模块编译器] 解析模块类型时发生部分加载失败。", ex);
        }

        return result;
    }

    private byte[] Compile(string sourceCode)
    {
        var codeHash = ComputeSHA256Hash(sourceCode);
        var cacheEntry = compiledAssemblyCache.GetOrAdd
        (
            codeHash,
            _ => new(() => CompileCore(sourceCode), LazyThreadSafetyMode.ExecutionAndPublication)
        );

        try
        {
            return cacheEntry.Value;
        }
        catch
        {
            compiledAssemblyCache.TryRemove(codeHash, out _);
            throw;
        }
    }

    private byte[] CompileCore(string sourceCode)
    {
        using var stream = new MemoryStream();

        var emitResult = CreateCompilation(sourceCode).Emit(stream);
        if (!emitResult.Success)
            ThrowCompilationException(emitResult);

        return stream.ToArray();
    }

    private CSharpCompilation CreateCompilation(string sourceCode)
    {
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(SourceText.From(sourceCode, Encoding.UTF8), ParseOptions);

        return CSharpCompilation.Create
        (
            $"DailyRoutinesModule-{Guid.NewGuid():N}",
            [syntaxTree],
            ReferenceList,
            CompilationOptions
        );
    }

    private static void ThrowCompilationException(EmitResult emitResult)
    {
        var diagnostics = emitResult.Diagnostics
                                    .Where
                                    (static diagnostic => diagnostic.IsWarningAsError ||
                                                          diagnostic.Severity == DiagnosticSeverity.Error
                                    )
                                    .ToArray();

        foreach (var diagnostic in diagnostics)
            DLog.Error($"[模块编译器] 模块编译失败: {diagnostic}");

        throw new InvalidOperationException($"模块编译失败，共 {diagnostics.Length} 个错误。");
    }

    private Assembly? LoadCore(byte[] assembly)
    {
        var parentLoadContext = AssemblyLoadContext.GetLoadContext(typeof(ModuleCompiler).Assembly);

        if (parentLoadContext == null)
        {
            DLog.Error("[模块编译器] 无法获取父程序集加载上下文。");
            return null;
        }

        using var stream      = new MemoryStream(assembly, false);
        var       loadContext = new ModuleLoadContext(parentLoadContext, $"DailyRoutinesModule-{Guid.NewGuid():N}");

        try
        {
            var loadedAssembly = loadContext.LoadFromStream(stream);
            loadedContexts.Add(loadContext);
            return loadedAssembly;
        }
        catch (Exception ex)
        {
            TryUnloadContext(loadContext);
            DLog.Error("[模块编译器] 加载动态程序集时发生异常。", ex);
            return null;
        }
    }

    private static ImmutableArray<MetadataReference> BuildReferenceList()
    {
        var builder       = ImmutableArray.CreateBuilder<MetadataReference>();
        var seenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddReferences(builder, seenFileNames, Path.GetDirectoryName(typeof(object).Assembly.Location), SearchOption.TopDirectoryOnly);
        AddReferences(builder, seenFileNames, Path.GetDirectoryName(typeof(Form).Assembly.Location),   SearchOption.TopDirectoryOnly);
        AddReferences(builder, seenFileNames, DService.Instance().PI.AssemblyLocation.DirectoryName,   SearchOption.AllDirectories);
        AddReferences
        (
            builder,
            seenFileNames,
            Path.GetDirectoryName(DService.Instance().PI.GetType().Assembly.Location),
            SearchOption.AllDirectories
        );

        return builder.MoveToImmutable();
    }

    private static void AddReferences
    (
        ImmutableArray<MetadataReference>.Builder builder,
        HashSet<string>                           seenFileNames,
        string?                                   directory,
        SearchOption                              searchOption
    )
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return;

        foreach (var path in EnumerateAssemblyFiles(directory, searchOption))
        {
            var fileName = Path.GetFileName(path);
            if (!seenFileNames.Add(fileName) || !IsValidAssembly(path))
                continue;

            builder.Add(MetadataReference.CreateFromFile(path));
        }
    }

    private static IEnumerable<string> EnumerateAssemblyFiles(string directory, SearchOption searchOption)
    {
        foreach (var path in Directory.EnumerateFiles(directory, "*.dll", searchOption))
            yield return path;

        foreach (var path in Directory.EnumerateFiles(directory, "*.exe", searchOption))
            yield return path;
    }

    private static bool IsValidAssembly(string path)
    {
        try
        {
            _ = AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
        catch (FileLoadException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string ComputeSHA256Hash(string data) =>
        ComputeSHA256Hash(Encoding.UTF8.GetBytes(data));

    private static string ComputeSHA256Hash(byte[] data) =>
        ComputeSHA256Hash(data.AsSpan());

    private static string ComputeSHA256Hash(ReadOnlySpan<byte> data) =>
        Convert.ToHexString(SHA256.HashData(data));

    private static void TryUnloadContext(ModuleLoadContext context)
    {
        try
        {
            context.Unload();
        }
        catch (Exception ex)
        {
            DLog.Error("[模块编译器] 卸载动态程序集上下文时发生异常。", ex);
        }
    }
}
