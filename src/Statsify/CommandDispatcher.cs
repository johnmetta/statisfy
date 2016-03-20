using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Options;

namespace Statsify
{
    public interface ICommandDispatcher
    {
        string Executable { get; }

        IReadOnlyCollection<ICommandDescriptor> Commands { get; } 
    }

    public interface ICommandDescriptor
    {
        string Name { get; }

        string Description { get; }

        OptionSet OptionSet { get; }

        IReadOnlyCollection<string> Aliases { get; }

        IReadOnlyCollection<IParameterDescriptor> Parameters { get; } 
    }

    public interface IParameterDescriptor
    {
        string Name { get; }

        string Description { get; }

        int Position { get; }

        bool Optional { get; }
    }

    class ParameterDescriptor : IParameterDescriptor
    {
        public string Name { get; private set; }
        
        public string Description { get; private set; }
        
        public int Position { get; private set; }
        
        public bool Optional { get; private set; }
        
        public Action<object, string> Setter { get; private set; }  

        public ParameterDescriptor(string name, string description, int position, bool optional, Action<object, string> setter)
        {
            Setter = setter;
            Name = name;
            Description = description;
            Position = position;
            Optional = optional;
        }
    }

    class CommandDescriptor : ICommandDescriptor
    {
        public string Name { get; private set; }
        
        public string Description { get; private set; }
        
        public OptionSet OptionSet { get; private set; }
        
        public IReadOnlyCollection<string> Aliases { get; private set; }

        public IReadOnlyCollection<ParameterDescriptor> Parameters { get; private set; }

        public Func<ICommand> Provider { get; private set; } 

        public Func<ICommand, Arguments, int> Invoker { get; private set; }

        public CommandDescriptor(string name, string description, OptionSet optionSet, IEnumerable<string> aliases, IReadOnlyCollection<ParameterDescriptor> parameters, Func<ICommand> provider, Func<ICommand, Arguments, int> invoker)
        {
            Provider = provider;
            Parameters = parameters;
            OptionSet = optionSet;
            Name = name;
            Description = description;
            Aliases = new ReadOnlyCollection<string>(new List<string>(aliases ?? new string[] {}));
            Invoker = invoker;
        }

        IReadOnlyCollection<IParameterDescriptor> ICommandDescriptor.Parameters 
        { 
            get { return Parameters; }
        }

    }

    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly Action<Options, OptionSet> defaultOptions;
        private readonly string helpCommandName;
        private readonly IList<CommandDescriptor> commandDescriptors = new List<CommandDescriptor>();

        public CommandDispatcher(Action<Options, OptionSet> defaultOptions = null, string helpCommandName = "help")
        {
            this.defaultOptions = defaultOptions;
            this.helpCommandName = helpCommandName;
        }

        public void Register<TCommand, TOptions>(Func<ICommandDispatcher, TCommand> commandProvider, string name, string description, Func<TOptions, OptionSet> optionSetProvider = null, params string[] aliases)
            where TCommand : Command<TOptions>
            where TOptions : Options, new()
        {
            var options = new TOptions();
            
            var optionSet = new OptionSet();
            if(optionSetProvider != null)
                foreach(var option in optionSetProvider(options))
                    optionSet.Add(option);

            if(defaultOptions != null)
                defaultOptions(options, optionSet);
            
            if(commandDescriptors.Any(cd => string.Equals(cd.Name, name, StringComparison.InvariantCultureIgnoreCase)))
                throw new InvalidOperationException();

            Func<ICommand> provider = () => commandProvider(this);
                
            Func<ICommand, Arguments, int> invoker = 
                (c, args) => {
                    var command = (TCommand)c;
                    var result = command.Execute(options, args);

                    return result;
                };

            var optional = false;
            var ordinal = 0;
            var parameters =
                typeof(TCommand).
                    GetProperties(BindingFlags.Public | BindingFlags.Instance).
                    Where(pi => pi.GetCustomAttribute<ParameterAttribute>() != null).
                    OrderBy(pi => pi.GetCustomAttribute<ParameterAttribute>().Position).
                    Select(pi => 
                    {
                        var a = pi.GetCustomAttribute<ParameterAttribute>();

                        Action<object, string> setter = (c, v) => {
                            var typeConverter = TypeDescriptor.GetConverter(pi.PropertyType);
                            var value = typeConverter.ConvertFromString(v);

                            pi.SetValue(c, value);
                        };

                        //
                        // As soon as we see the very first optional parameter, make all subsequent parameters optional as well.
                        optional |= a.Optional;
                        var parameter = new ParameterDescriptor(a.Name, a.Description, ordinal++, optional, setter);

                        return parameter;
                    }).
                    OrderBy(p => p.Position).
                    ToList();


            var commandDescriptor = new CommandDescriptor(name, description, optionSet, null, parameters, provider, invoker);
            commandDescriptors.Add(commandDescriptor);
        }

        public int Execute(params string[] args)
        {
            if(args.Length == 0)
            {
                return string.IsNullOrWhiteSpace(helpCommandName) ?
                    -1 :
                    Execute(helpCommandName);
            } // if

            var name = args[0].ToLowerInvariant();

            var commandDescriptor = commandDescriptors.SingleOrDefault(cd => string.Equals(cd.Name, name, StringComparison.InvariantCultureIgnoreCase));
            if(commandDescriptor == null) return -1;

            Arguments arguments;
            
            try
            {
                arguments = new Arguments(commandDescriptor.OptionSet.Parse(args.Skip(1).ToArray()));
            } // try
            catch(OptionException e)
            {
                Console.WriteLine("{0} {1}: {2}", Executable, name, e.Message);
                Execute("help", name);

                return -1;
            } // catch

            var command = commandDescriptor.Provider();
            
            // 
            // Make sure all the required arguments are supplied
            var requiredParameters = commandDescriptor.Parameters.Count(p => !p.Optional);
            if(arguments.Length < requiredParameters)
            {
                Console.WriteLine("{0} {1}: Missing required parameter(s)", Executable, name);
                Execute("help", name);

                return -1;
            } // if

            var offset = 0;
            for(var i = 0; i < arguments.Length; ++i)
            {
                var parameter = commandDescriptor.Parameters.FirstOrDefault(p => p.Position == i);
                if(parameter == null) continue;

                parameter.Setter(command, arguments.GetArgument(i));
                offset++;
            } // for
            
            arguments.WithOffset(offset);
            var result = commandDescriptor.Invoker(command, arguments);

            return result;
        }

        public string Executable
        {
            get { return Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName).ToLowerInvariant(); }
        }

        public IReadOnlyCollection<ICommandDescriptor> Commands
        {
            get { return commandDescriptors.ToList().AsReadOnly(); }
        }
    }

    public class Options
    {
        public bool Verbose { get; set; }
    }

    public class Arguments
    {
        private readonly string[] arguments;
        private int offset;

        public int Length
        {
            get { return arguments.Length - offset; }
        }

        public Arguments(IEnumerable<string> arguments)
        {
            this.arguments = (arguments ?? new string[] { }).ToArray();
        }

        public string GetArgument(int index, string @default = "")
        {
            return index + offset > arguments.Length ? @default : arguments[index + offset];
        }

        public void WithOffset(int offset)
        {
            if(offset > arguments.Length)
                throw new ArgumentOutOfRangeException("offset");

            this.offset = offset;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ParameterAttribute : Attribute
    {
        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// The relative position of the parameter.
        /// </summary>
        public int Position { get; set; }

        public bool Optional { get; set; }
    }

    public interface ICommand
    {
    }

    public abstract class Command<TOptions> : ICommand
        where TOptions : Options
    {
        public virtual int Execute(TOptions options, Arguments args)
        {
            return 0;
        }
    }
}
