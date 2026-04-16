using System.ComponentModel;
using Microsoft.SemanticKernel;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.Examples;

/// <summary>
/// Example plugin providing basic calculator operations.
/// </summary>
[Plugin(Name = "Calculator", Description = "Performs basic arithmetic operations")]
public class CalculatorPlugin
{
    [KernelFunction("add")]
    [Description("Add two numbers together")]
    public double Add(
        [Description("The first number")] double a,
        [Description("The second number")] double b)
    {
        return a + b;
    }

    [KernelFunction("subtract")]
    [Description("Subtract one number from another")]
    public double Subtract(
        [Description("The number to subtract from")] double a,
        [Description("The number to subtract")] double b)
    {
        return a - b;
    }

    [KernelFunction("multiply")]
    [Description("Multiply two numbers")]
    public double Multiply(
        [Description("The first number")] double a,
        [Description("The second number")] double b)
    {
        return a * b;
    }

    [KernelFunction("divide")]
    [Description("Divide one number by another")]
    public double Divide(
        [Description("The dividend")] double a,
        [Description("The divisor")] double b)
    {
        if (b == 0)
            throw new DivideByZeroException("Cannot divide by zero");
        
        return a / b;
    }

    [KernelFunction("power")]
    [Description("Raise a number to a power")]
    public double Power(
        [Description("The base number")] double baseNumber,
        [Description("The exponent")] double exponent)
    {
        return Math.Pow(baseNumber, exponent);
    }
}