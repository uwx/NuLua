using System.Text;
using Scriban;
using Scriban.Runtime;

string? name = null;
int? version = null;
var templateDirs = new List<string>();
string? outputDir = null;
bool isJit = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--name":
            name = args[++i];
            break;
        case "--version":
            version = int.Parse(args[++i]);
            break;
        case "--template-dir":
            templateDirs.Add(args[++i]);
            break;
        case "--output-dir":
            outputDir = args[++i];
            break;
        case "--is-jit":
            isJit = true;
            break;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            return 1;
    }
}

if (name is null || version is null || templateDirs.Count == 0 || outputDir is null)
{
    Console.Error.WriteLine(
        "Usage: CodeGen --name <Name> --version <NNN> --template-dir <dir> [--template-dir <dir>...] --output-dir <dir> [--is-jit]"
    );
    return 1;
}

Directory.CreateDirectory(outputDir);

var model = new ScriptObject();
model["name"] = name;
model["version"] = version.Value;
model["is_jit"] = isJit;

var templates = templateDirs
    .SelectMany(dir => Directory.GetFiles(dir, "*.scriban-cs", SearchOption.TopDirectoryOnly))
    .ToArray();
if (templates.Length == 0)
{
    Console.Error.WriteLine(
        $"No templates (*.scriban-cs) found in: {string.Join(", ", templateDirs)}."
    );
    return 1;
}

foreach (var templatePath in templates)
{
    var source = File.ReadAllText(templatePath);
    var template = Template.Parse(source, templatePath);
    if (template.HasErrors)
    {
        foreach (var msg in template.Messages)
        {
            Console.Error.WriteLine(msg.ToString());
        }
        return 1;
    }

    var context = new TemplateContext { StrictVariables = true };
    context.PushGlobal(model);
    var rendered = template.Render(context);
    context.PopGlobal();

    var baseName = Path.GetFileNameWithoutExtension(templatePath);
    var outputPath = Path.Combine(outputDir, $"{name}{baseName}.g.cs");

    if (File.Exists(outputPath))
    {
        var existing = File.ReadAllText(outputPath);
        if (existing == rendered)
        {
            continue;
        }
    }

    File.WriteAllText(outputPath, rendered, new UTF8Encoding(false));
}

return 0;
