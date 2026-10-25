using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

public sealed class ConsoleCommandRegistry
{
    private class Command
    {
        public object Target;
        public MethodInfo Method;
        public ConsoleCommandAttribute Attribute;
        public string OwnerName;
        public bool IsDefaultTarget;
    }

    private readonly Dictionary<string, List<Command>> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Clear() => _commands.Clear();

    public List<string> GetCompletions(string prefix)
    {
        List<string> matches = new();
        if (string.IsNullOrEmpty(prefix))
            return matches;

        int tokenStart = prefix.Length;
        while (tokenStart > 0 && !char.IsWhiteSpace(prefix[tokenStart - 1]))
            tokenStart--;
        if (tokenStart > 0)
        {
            string leading = prefix.Substring(0, tokenStart);
            List<string> words;
            try
            {
                words = Split(leading);
            }
            catch (FormatException)
            {
                return matches;
            }
            if (words.Count == 0 || !_commands.TryGetValue(words[0], out List<Command> commands))
                return matches;

            string fragment = prefix.Substring(tokenStart);
            if (words.Count == 1)
            {
                foreach (Command command in commands)
                {
                    if (command.OwnerName != null && command.OwnerName.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
                    {
                        string suggestion = leading + command.OwnerName;
                        if (!matches.Contains(suggestion))
                            matches.Add(suggestion);
                    }
                }
                matches.Sort(StringComparer.OrdinalIgnoreCase);
            }

            commands = ResolveTargets(commands, words, out int argumentStart);
            if (commands.Count != 1)
                return matches;
            ParameterInfo[] parameters = commands[0].Method.GetParameters();
            int argumentIndex = words.Count - argumentStart;
            if (argumentIndex >= parameters.Length || parameters[argumentIndex].GetCustomAttribute<ConsoleStatNameAttribute>() == null)
                return matches;

            foreach (PropertyInfo property in typeof(ActorStats).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.PropertyType == typeof(Stat) && property.Name.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
                    matches.Add(leading + property.Name);
            }
            matches.Sort(StringComparer.OrdinalIgnoreCase);
            return matches;
        }

        bool isQualified = prefix.Contains(".");
        foreach (string name in _commands.Keys)
        {
            if (name.Contains(".") == isQualified && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                matches.Add(name);
        }
        matches.Sort(StringComparer.OrdinalIgnoreCase);
        return matches;
    }

    public void Register(object target, string ownerName = null, bool isDefaultTarget = true)
    {
        if (target == null)
            return;

        foreach (MethodInfo method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            ConsoleCommandAttribute attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
            if (attribute == null || method.ContainsGenericParameters || method.IsSpecialName)
                continue;

            Command command = new Command
            {
                Target = method.IsStatic ? null : target,
                Method = method,
                Attribute = attribute,
                OwnerName = method.IsStatic ? null : ownerName,
                IsDefaultTarget = method.IsStatic || ownerName == null || isDefaultTarget
            };
            RegisterName(method.Name, command);
            RegisterName(method.DeclaringType.Name + "." + method.Name, command);
        }
    }

    private void RegisterName(string name, Command command)
    {
        if (!_commands.TryGetValue(name, out List<Command> commands))
        {
            commands = new List<Command>();
            _commands.Add(name, commands);
        }
        if (!commands.Exists(existing => existing.Method == command.Method && ReferenceEquals(existing.Target, command.Target)))
            commands.Add(command);
    }

    public string Execute(string input, bool isServer)
    {
        List<string> words = Split(input);
        if (words.Count == 0)
            return null;
        if (!_commands.TryGetValue(words[0], out List<Command> commands))
            throw new ArgumentException("Неизвестная команда. Введи Help.");
        commands = ResolveTargets(commands, words, out int argumentStart);
        if (commands.Count == 0)
            throw new ArgumentException("Команда недоступна для этой цели. Введи Players, чтобы посмотреть игроков.");
        if (commands.Count != 1)
            throw new ArgumentException("Неоднозначная команда. Укажи имя класса: ClassName.MethodName. Если объекты одного класса, оставь один источник команды.");

        Command command = commands[0];
        if (!isServer && (command.Attribute.ServerOnly || !command.IsDefaultTarget))
            throw new InvalidOperationException("Эта команда или управление другим игроком доступны только хосту или серверу.");

        ParameterInfo[] parameters = command.Method.GetParameters();
        int count = words.Count - argumentStart;
        if (count > parameters.Length)
            throw new ArgumentException("Формат: " + Signature(command));

        object[] arguments = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            if (i >= count)
            {
                if (!parameter.HasDefaultValue)
                    throw new ArgumentException("Формат: " + Signature(command));
                arguments[i] = parameter.DefaultValue;
                continue;
            }

            try
            {
                arguments[i] = ConvertArgument(words[i + argumentStart], parameter.ParameterType);
            }
            catch (Exception error) when (error is FormatException || error is OverflowException || error is ArgumentException || error is NotSupportedException)
            {
                throw new ArgumentException($"Неверный параметр {parameter.Name} ({parameter.ParameterType.Name}). Формат: {Signature(command)}");
            }
        }

        try
        {
            object result = command.Method.Invoke(command.Target, arguments);
            return result?.ToString();
        }
        catch (TargetInvocationException error)
        {
            throw new InvalidOperationException(error.InnerException?.Message ?? error.Message);
        }
    }

    public string GetHelp(string name = "")
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (!_commands.TryGetValue(name, out List<Command> commands))
                return "Команда не найдена.";
            return string.Join("\n", new HashSet<string>(commands.ConvertAll(Signature)));
        }

        List<string> lines = new();
        foreach (KeyValuePair<string, List<Command>> pair in _commands)
        {
            if (pair.Key.Contains("."))
                continue;
            foreach (Command command in pair.Value)
            {
                string signature = Signature(command);
                if (!lines.Contains(signature))
                    lines.Add(signature);
            }
        }
        lines.Sort(StringComparer.OrdinalIgnoreCase);
        return string.Join("\n", lines);
    }

    private string Signature(Command command)
    {
        StringBuilder text = new StringBuilder(command.Method.DeclaringType.Name + "." + command.Method.Name);
        if (command.OwnerName != null)
            text.Append(" [player]");
        foreach (ParameterInfo parameter in command.Method.GetParameters())
        {
            text.Append(" <").Append(parameter.Name).Append(": ").Append(parameter.ParameterType.Name);
            if (parameter.HasDefaultValue)
                text.Append(" = ").Append(Convert.ToString(parameter.DefaultValue, CultureInfo.InvariantCulture));
            text.Append('>');
        }
        if (command.Attribute.ServerOnly)
            text.Append(" [host]");
        return text.ToString();
    }

    private List<Command> ResolveTargets(List<Command> commands, List<string> words, out int argumentStart)
    {
        argumentStart = 1;
        if (words.Count > 1 && commands.Exists(command => command.OwnerName != null
            && string.Equals(command.OwnerName, words[1], StringComparison.OrdinalIgnoreCase)))
        {
            argumentStart = 2;
            return commands.FindAll(command => string.Equals(command.OwnerName, words[1], StringComparison.OrdinalIgnoreCase));
        }
        return commands.FindAll(command => command.IsDefaultTarget);
    }

    private object ConvertArgument(string text, Type type)
    {
        if (type.IsEnum)
        {
            object value = Enum.Parse(type, text, true);
            if (!Enum.IsDefined(type, value))
                throw new ArgumentException("Unknown enum value.");
            return value;
        }
        if (type == typeof(string))
            return text;
        if (!type.IsPrimitive && type != typeof(decimal))
            throw new NotSupportedException("Unsupported command argument.");

        object result;
        if (type == typeof(float))
            result = float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        else if (type == typeof(double))
            result = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        else if (type == typeof(decimal))
            result = decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        else
            result = Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
        if (result is float number && (float.IsNaN(number) || float.IsInfinity(number)))
            throw new FormatException("Expected a finite number.");
        if (result is double doubleNumber && (double.IsNaN(doubleNumber) || double.IsInfinity(doubleNumber)))
            throw new FormatException("Expected a finite number.");
        return result;
    }

    private List<string> Split(string input)
    {
        List<string> words = new();
        StringBuilder word = new();
        bool isQuoted = false;
        bool isStarted = false;
        input = input ?? "";
        for (int i = 0; i < input.Length; i++)
        {
            char character = input[i];
            if (character == '\\' && i + 1 < input.Length && (input[i + 1] == '"' || input[i + 1] == '\\'))
            {
                word.Append(input[++i]);
                isStarted = true;
            }
            else if (character == '"')
            {
                isQuoted = !isQuoted;
                isStarted = true;
            }
            else if (char.IsWhiteSpace(character) && !isQuoted)
            {
                if (isStarted)
                {
                    words.Add(word.ToString());
                    word.Clear();
                    isStarted = false;
                }
            }
            else
            {
                word.Append(character);
                isStarted = true;
            }
        }
        if (isQuoted)
            throw new FormatException("Не закрыта кавычка.");
        if (isStarted)
            words.Add(word.ToString());
        return words;
    }
}
