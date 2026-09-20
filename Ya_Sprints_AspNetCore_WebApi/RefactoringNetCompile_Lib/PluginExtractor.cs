//dotnet add package Microsoft.CodeAnalysis.Workspaces.MSBuild
//dotnet add package Microsoft.CodeAnalysis.CSharp
// dotnet run -- "C:\MyApp\MyApp.sln" "MyPlugin"
 
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild; 
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;


namespace RefactoringNetCompile_Lib;

/// <summary>
/// PluginExtractor для рефакторинга очень сложных монолитов
/// Вынос и группировка связанных блоков
/// Отображение в UI в виде графов  
/// </summary>
class PluginExtractor
{
    static async Task Main(string[] args)
    {
        
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: PluginExtractor <path-to-sln-or-csproj> <new-library-name>");
            return;
        }

        var projectPath = args[0];
        var newLibraryName = args[1];

        MSBuildLocator.RegisterDefaults();
        // 1. Загружаем проект
        var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);

        // 2. Анализируем и строим граф
        var analyzer = new ProjectAnalyzer(project);
        var graph = await analyzer.BuildDependencyGraphAsync();

        // 3. Находим компоненты связности (группы)
        var groups = DependencyGraph.FindConnectedComponents(graph);

        // 4. Выводим группы и даём выбрать
        Console.WriteLine($"Found {groups.Count} groups:");
        for (int i = 0; i < groups.Count; i++)
        {
            var names = string.Join(", ", groups[i].Take(3));
            Console.WriteLine($"{i + 1}: [{groups[i].Count} types] {names}...");
        }

        Console.Write("Enter group number(s) to extract (e.g. 1,3,5): ");
        var input = Console.ReadLine();
        var selectedIndices = input.Split(',').Select(s => int.Parse(s.Trim()) - 1).ToList();

        // 5. Копируем выбранные группы
        var copier = new CodeCopier(project, newLibraryName);
        foreach (var idx in selectedIndices)
        {
            if (idx < groups.Count)
            {
                await copier.CopyGroupAsync(groups[idx]);
            }
        }

        Console.WriteLine("Done.");
    }
}

// ---------- Модели и анализ ----------
public class DependencyGraph
{
    public Dictionary<string, HashSet<string>> Edges { get; } = new();

    public void AddEdge(string from, string to)
    {
        if (!Edges.ContainsKey(from)) Edges[from] = new HashSet<string>();
        Edges[from].Add(to);
    }

    public static List<HashSet<string>> FindConnectedComponents(DependencyGraph graph)
    {
        var visited = new HashSet<string>();
        var components = new List<HashSet<string>>();

        foreach (var node in graph.Edges.Keys)
        {
            if (!visited.Contains(node))
            {
                var component = new HashSet<string>();
                var stack = new Stack<string>();
                stack.Push(node);
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (!visited.Add(current)) continue;
                    component.Add(current);
                    if (graph.Edges.TryGetValue(current, out var neighbors))
                    {
                        foreach (var n in neighbors)
                            if (!visited.Contains(n)) stack.Push(n);
                    }
                }
                components.Add(component);
            }
        }
        return components;
    }
}

public class ProjectAnalyzer
{
    private readonly Project _project;

    public ProjectAnalyzer(Project project) => _project = project;

    public async Task<DependencyGraph> BuildDependencyGraphAsync()
    {
        var graph = new DependencyGraph();
        var documents = _project.Documents;

        foreach (var doc in documents)
        {
            var syntaxTree = await doc.GetSyntaxTreeAsync();
            var semanticModel = await doc.GetSemanticModelAsync();
            var root = await syntaxTree.GetRootAsync();

            // Обрабатываем C# файлы
            if (doc.FilePath.EndsWith(".cs"))
            {
                var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>();
                foreach (var typeDecl in types)
                {
                    var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl);
                    if (typeSymbol == null) continue;
                    var fullName = typeSymbol.ToString();

                    // Ищем ссылки на другие типы
                    var references = typeDecl.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Select(id => semanticModel.GetSymbolInfo(id).Symbol)
                        .OfType<INamedTypeSymbol>()
                        .Where(s => !s.ContainingNamespace?.ToString().StartsWith("System") == true);

                    foreach (var refSymbol in references)
                    {
                        graph.AddEdge(fullName, refSymbol.ToString());
                    }
                }
            }
            // Для AXAML файлов ищем DataContext и x:DataType
            else if (doc.FilePath.EndsWith(".axaml") || doc.FilePath.EndsWith(".xaml"))
            {
                var text = await File.ReadAllTextAsync(doc.FilePath);
                // Ищем атрибуты DataContext или x:DataType
                var matches = Regex.Matches(text,
                    @"(?:DataContext|x:DataType)\s*=\s*""\s*({?[^""}]+)""?");
                foreach (Match m in matches)
                {
                    var typeName = m.Groups[1].Value.Trim();
                    // Упрощённо: если имя содержит точку, считаем полным
                    if (typeName.Contains('.') && !typeName.Contains('{'))
                    {
                        // Добавляем связь от файла (или от view) к этому типу
                        // Свяжем с именем класса, указанным в x:Class
                        var classMatch = Regex.Match(text, @"x:Class\s*=\s*""([^""]+)""");
                        if (classMatch.Success)
                        {
                            var viewClass = classMatch.Groups[1].Value;
                            graph.AddEdge(viewClass, typeName);
                        }
                    }
                }
            }
        }
        return graph;
    }
}

// ---------- Копирование и преобразование ----------
public class CodeCopier
{
    private readonly Project _sourceProject;
    private readonly string _newLibraryName;
    private readonly string _outputDir;

    public CodeCopier(Project sourceProject, string newLibraryName)
    {
        _sourceProject = sourceProject;
        _newLibraryName = newLibraryName;
        _outputDir = Path.Combine(Path.GetDirectoryName(sourceProject.FilePath), "ExtractedPlugins", newLibraryName);
    }

    public async Task CopyGroupAsync(HashSet<string> group)
    {
        // Находим все документы, содержащие типы из группы
        var documents = _sourceProject.Documents
            .Where(d => d.FilePath.EndsWith(".cs") || d.FilePath.EndsWith(".axaml"))
            .ToList();

        // Сопоставим файлы с типами
        var fileToTypes = new Dictionary<string, List<INamedTypeSymbol>>();
        foreach (var doc in documents)
        {
            var tree = await doc.GetSyntaxTreeAsync();
            var model = await doc.GetSemanticModelAsync();
            var root = await tree.GetRootAsync();
            var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Select(t => model.GetDeclaredSymbol(t) as INamedTypeSymbol)
                .Where(t => t != null && group.Contains(t.ToString()))
                .ToList();
            if (types.Any())
            {
                fileToTypes[doc.FilePath] = types;
            }
        }

        // Определим категории файлов
        var enumFiles = new List<string>();
        var viewFiles = new List<string>();
        var viewModelFiles = new List<string>();
        var dbContextFiles = new List<string>();
        var entityFiles = new List<string>();

        foreach (var kv in fileToTypes)
        {
            var file = kv.Key;
            var types = kv.Value;
            var anyEnum = types.Any(t => t.TypeKind == TypeKind.Enum);
            var anyClass = types.Any(t => t.TypeKind == TypeKind.Class);
            var isView = file.EndsWith(".axaml") || types.Any(t => t.BaseType?.Name == "UserControl" || t.BaseType?.Name == "Window");
            var isViewModel = types.Any(t => t.BaseType?.Name.Contains("ViewModel") == true || t.Name.EndsWith("ViewModel"));
            var isDbContext = types.Any(t => t.BaseType?.Name == "DbContext");
            var isEntity = types.Any(t => t.Name.EndsWith("Entity") || t.Name.EndsWith("Model") && !isViewModel);

            if (anyEnum && types.Count == 1) enumFiles.Add(file);
            else if (isView) viewFiles.Add(file);
            else if (isViewModel) viewModelFiles.Add(file);
            else if (isDbContext) dbContextFiles.Add(file);
            else if (isEntity) entityFiles.Add(file);
            else // остальные (могут быть перечислены выше)
            {
                // Попробуем отнести к сущностям или моделям
                if (types.Any(t => t.Name.EndsWith("Model") && !t.Name.EndsWith("ViewModel")))
                    entityFiles.Add(file);
                else
                    viewModelFiles.Add(file); // fallback
            }
        }

        // Создаём структуру папок
        var baseDir = _outputDir;
        Directory.CreateDirectory(baseDir);
        Directory.CreateDirectory(Path.Combine(baseDir, "Views"));
        Directory.CreateDirectory(Path.Combine(baseDir, "ViewModels"));
        Directory.CreateDirectory(Path.Combine(baseDir, "Enums"));
        Directory.CreateDirectory(Path.Combine(baseDir, "Data"));
        Directory.CreateDirectory(Path.Combine(baseDir, "Entities"));

        // Копируем с преобразованием неймспейсов
        var rewriter = new NamespaceRewriter(_sourceProject.Name, _newLibraryName);

        foreach (var file in viewFiles)
            await CopyFileWithNamespaceChange(file, Path.Combine(baseDir, "Views"), rewriter);
        foreach (var file in viewModelFiles)
            await CopyFileWithNamespaceChange(file, Path.Combine(baseDir, "ViewModels"), rewriter);
        foreach (var file in enumFiles)
            await SplitEnumFile(file, Path.Combine(baseDir, "Enums"), rewriter);
        foreach (var file in dbContextFiles)
            await CopyFileWithNamespaceChange(file, Path.Combine(baseDir, "Data"), rewriter);
        foreach (var file in entityFiles)
            await CopyFileWithNamespaceChange(file, Path.Combine(baseDir, "Entities"), rewriter);

        // Для файлов, которые не попали в категории (например, сервисы) – можно скопировать в корень
        var allCopied = viewFiles.Concat(viewModelFiles).Concat(enumFiles).Concat(dbContextFiles).Concat(entityFiles).ToHashSet();
        foreach (var file in fileToTypes.Keys.Except(allCopied))
        {
            await CopyFileWithNamespaceChange(file, baseDir, rewriter);
        }

        // Создаём файл .csproj для библиотеки
        CreateProjectFile();
    }

    private async Task CopyFileWithNamespaceChange(string sourceFile, string destFolder, NamespaceRewriter rewriter)
    {
        var destFile = Path.Combine(destFolder, Path.GetFileName(sourceFile));
        var content = await File.ReadAllTextAsync(sourceFile);
        if (sourceFile.EndsWith(".cs"))
        {
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = await tree.GetRootAsync();
            var newRoot = rewriter.Visit(root);
            content = newRoot.ToFullString();
        }
        else if (sourceFile.EndsWith(".axaml") || sourceFile.EndsWith(".xaml"))
        {
            // Заменяем xmlns:local и clr-namespace
            content = Regex.Replace(content, @"clr-namespace:[^;""]+", $"clr-namespace:{_newLibraryName}");
            content = Regex.Replace(content, @"xmlns:local=""[^""]*""", $"xmlns:local=\"clr-namespace:{_newLibraryName}\"");
            // x:Class тоже меняем
            content = Regex.Replace(content, @"x:Class=""[^""]+""", $"x:Class=\"{_newLibraryName}.{Path.GetFileNameWithoutExtension(sourceFile)}\"");
        }
        Directory.CreateDirectory(destFolder);
        await File.WriteAllTextAsync(destFile, content);
    }

    private async Task SplitEnumFile(string sourceFile, string destFolder, NamespaceRewriter rewriter)
    {
        var content = await File.ReadAllTextAsync(sourceFile);
        var tree = CSharpSyntaxTree.ParseText(content);
        var root = await tree.GetRootAsync();
        var enumDecls = root.DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();

        // Получаем using directives и namespace
        var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
        var namespaceDecl = root.DescendantNodes()
       .OfType<BaseNamespaceDeclarationSyntax>()
       .FirstOrDefault();

        foreach (var enumDecl in enumDecls)
        {
            var enumName = enumDecl.Identifier.Text;
            var newContent = string.Join("\n", usings) + "\n\n";
            if (namespaceDecl != null)
            {
                var ns = namespaceDecl.Name.ToString();
                ns = ns.Replace(_sourceProject.Name, _newLibraryName);
                // Создаём новое namespace с фигурными скобками (безопасно для любого типа)
                newContent += $"namespace {ns} {{ {enumDecl.ToFullString()} }}";
            }
            else
            {
                newContent += enumDecl.ToFullString();
            }
            var destFile = Path.Combine(destFolder, enumName + ".cs");
            await File.WriteAllTextAsync(destFile, newContent);
        }
    }

    private void CreateProjectFile()
    {
        var csproj = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Avalonia"" Version=""11.0.0"" />
    <!-- Добавьте другие зависимости по необходимости -->
  </ItemGroup>
</Project>";
        File.WriteAllText(Path.Combine(_outputDir, $"{_newLibraryName}.csproj"), csproj);
    }
}

// ---------- Переименование неймспейсов в C# ----------
public class NamespaceRewriter : CSharpSyntaxRewriter
{
    private readonly string _oldNs;
    private readonly string _newNs;

    public NamespaceRewriter(string oldNs, string newNs)
    {
        _oldNs = oldNs;
        _newNs = newNs;
    }

    public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        var newName = node.Name.ToString().Replace(_oldNs, _newNs);
        return node.WithName(SyntaxFactory.ParseName(newName));
    }

    public override SyntaxNode VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        var newName = node.Name.ToString().Replace(_oldNs, _newNs);
        return node.WithName(SyntaxFactory.ParseName(newName));
    }

    public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
    {
        if (node.Name != null)
        {
            var newName = node.Name.ToString().Replace(_oldNs, _newNs);
            if (newName != node.Name.ToString())
                return node.WithName(SyntaxFactory.ParseName(newName));
        }
        return base.VisitUsingDirective(node);
    }
}