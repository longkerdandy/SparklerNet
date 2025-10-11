using System.Reflection;
using MQTTnet;
using MQTTnet.Packets;

// 创建一个简单的反射测试程序，检查MQTTnet库中相关类型的信息
class Program
{
    static void Main()
    {
        Console.WriteLine("检查 MqttClientSubscribeResult 类信息：");
        InspectType(typeof(MqttClientSubscribeResult));
        
        Console.WriteLine("\n检查 MqttClientSubscribeResultItem 类信息：");
        InspectType(typeof(MqttClientSubscribeResultItem));
        
        Console.WriteLine("\n检查 MqttTopicFilter 类信息：");
        InspectType(typeof(MqttTopicFilter));
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
    
    static void InspectType(Type type)
    {
        Console.WriteLine($"类型: {type.FullName}");
        Console.WriteLine($"是否密封类: {type.IsSealed}");
        Console.WriteLine($"是否抽象类: {type.IsAbstract}");
        
        // 检查构造函数
        Console.WriteLine("构造函数:");
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 0)
        {
            Console.WriteLine("  无公共构造函数");
        }
        else
        {
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                var paramStrings = parameters.Select(p => $"{p.ParameterType.Name} {p.Name}");
                Console.WriteLine($"  {type.Name}({string.Join(", ", paramStrings)})");
            }
        }
        
        // 检查属性
        Console.WriteLine("属性:");
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            Console.WriteLine($"  {prop.PropertyType.Name} {prop.Name} (可写: {prop.CanWrite})");
        }
    }
}