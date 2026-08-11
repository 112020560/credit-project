using Scriban;
using Scriban.Runtime;

namespace CreditSystem.Infrastructure.Documents;

public class ScribanTemplateEngine
{
    private readonly string _templatesPath;

    public ScribanTemplateEngine()
    {
        _templatesPath = Path.Combine(
            AppContext.BaseDirectory,
            "Documents", "Templates");
    }

    public async Task<string> RenderAsync<TData>(string templateFileName, TData data)
    {
        var filePath = Path.Combine(_templatesPath, templateFileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Document template not found: {templateFileName}", filePath);

        var templateText = await File.ReadAllTextAsync(filePath);
        var template = Template.Parse(templateText);

        var scriptObject = new ScriptObject();
        scriptObject.Import(data, renamer: member => member.Name.ToLower()
            .Replace("_", ""));

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return await template.RenderAsync(context);
    }
}
