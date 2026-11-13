using System;
using System.Reflection;
using NUnit.Framework;

public static class TutorialTestRunner
{
    public static int Main()
    {
        int count = 0;
        foreach (MethodInfo method in typeof(TutorialTests).GetMethods())
        {
            if (!Attribute.IsDefined(method, typeof(TestAttribute))) continue;
            try { method.Invoke(new TutorialTests(), null); Console.WriteLine("PASS: " + method.Name); count++; }
            catch (Exception exception) { Console.WriteLine("FAIL: " + method.Name + "\n" + (exception.InnerException ?? exception)); return 1; }
        }
        Console.WriteLine("Passed " + count + " tutorial tests.");
        return 0;
    }
}
