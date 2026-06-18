using System.Text;
using ConsoleAppFramework;
using Scriban;
using Scriban.Runtime;

ConsoleApp.Run(args, Run);

static int Run(
    string name,
    int version,
    string commonTemplateDir,
    string outputDir,
    string? dialectTemplateDir = null,
    bool isJit = false
)
{
    Directory.CreateDirectory(outputDir);

    var model = new ScriptObject();
    model["name"] = name;
    model["version"] = version;
    model["is_jit"] = isJit;

    var templateDirs = new List<string> { commonTemplateDir };
    if (dialectTemplateDir is not null)
    {
        templateDirs.Add(dialectTemplateDir);
    }

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
}
