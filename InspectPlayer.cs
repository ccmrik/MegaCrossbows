using System;
using System.Linq;
using System.Reflection;

class Program
{
    static void Main()
    {
        var assembly = Assembly.LoadFrom(@"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll");
        
        var playerType = assembly.GetType("Player");
        
        Console.WriteLine("=== PLAYER METHODS (Attack/Input/Update related) ===");
        var methods = playerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.Contains("Attack") || m.Name.Contains("attack") || 
                       m.Name.Contains("Update") || m.Name.Contains("Input") || 
                       m.Name.Contains("Fire") || m.Name.Contains("Reload") ||
                       m.Name.Contains("Draw") || m.Name.Contains("Button"))
            .OrderBy(m => m.Name);
        
        foreach (var method in methods)
        {
            Console.WriteLine($"{method.Name} - Returns: {method.ReturnType.Name}");
        }
        
        Console.WriteLine("\n=== CHARACTER BASE CLASS METHODS ===");
        var characterType = assembly.GetType("Character");
        var charMethods = characterType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.Contains("Attack") || m.Name.Contains("attack"))
            .OrderBy(m => m.Name);
        
        foreach (var method in charMethods)
        {
            Console.WriteLine($"{method.Name} - Returns: {method.ReturnType.Name}");
        }
    }
}
