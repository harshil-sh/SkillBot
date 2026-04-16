using System.ComponentModel;
using Microsoft.SemanticKernel;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.Examples;

/// <summary>
/// Example plugin for time and date operations.
/// </summary>
[Plugin(Name = "Time", Description = "Get current time and date information")]
public class TimePlugin
{
    [KernelFunction("get_current_time")]
    [Description("Get the current date and time")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    [KernelFunction("get_current_date")]
    [Description("Get the current date")]
    public string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    [KernelFunction("get_day_of_week")]
    [Description("Get the day of the week for today")]
    public string GetDayOfWeek()
    {
        return DateTime.Now.DayOfWeek.ToString();
    }

    [KernelFunction("add_days")]
    [Description("Add days to the current date and return the result")]
    public string AddDays(
        [Description("Number of days to add (can be negative)")] int days)
    {
        return DateTime.Now.AddDays(days).ToString("yyyy-MM-dd");
    }
}