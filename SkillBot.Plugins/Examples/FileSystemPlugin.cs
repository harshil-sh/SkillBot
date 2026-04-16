using System.ComponentModel;
using Microsoft.SemanticKernel;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.Examples;

[Plugin(Name = "FileSystem", Description = "File operations")]
public class FileSystemPlugin
{
    [KernelFunction("read_file")]
    [Description("Read contents of a file")]
    public string ReadFile([Description("File path")] string path)
    {
        return File.ReadAllText(path);
    }
}