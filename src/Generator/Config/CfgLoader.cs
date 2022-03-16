namespace CppSharp.Config
{
    using CppSharp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    internal static partial class CfgLoader
    {
        public static Cfg LoadConfig(string path, Driver driver)
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new YamlTypeConverter())
                .WithNodeDeserializer(new NodeDeserializer())
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            var yaml = File.ReadAllText(path);
            var config = deserializer.Deserialize<Cfg>(yaml);

            if (config.SetupMSVC)
                config.ParserOptions.SetupMSVC();

            driver.Options = config.Options;
            driver.ParserOptions = config.ParserOptions;
            return config;
        }

        private class NodeDeserializer : INodeDeserializer
        {
            bool INodeDeserializer.Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
            {
                if (expectedType != typeof(string) || !parser.TryConsume<Scalar>(out var scalar))
                {
                    value = null;
                    return false;
                }

                var template = scalar.Value;

                value = Regex.Replace(template, @"\${(\w+)}", match =>
                {
                    var result = Environment.GetEnvironmentVariable(match.Groups[1].Value);
                    return result;
                });

                return true;
            }
        }

        private class YamlTypeConverter : IYamlTypeConverter
        {
            public bool Accepts(Type type)
            {
                if (type != typeof(List<string>))
                    return false;
                return true;
            }

            public object ReadYaml(IParser parser, Type type)
            {
                if (parser.Current.GetType() != typeof(SequenceStart))
                    throw new InvalidDataException("Invalid YAML.");

                var list = new List<string>();

                parser.MoveNext();

                do
                {
                    if (parser.Current is not Scalar s)
                        throw new InvalidDataException("Invalid YAML.");

                    list.AddRange(s.Value.Split(';'));
                    parser.MoveNext();
                } while (parser.Current.GetType() != typeof(SequenceEnd));

                parser.MoveNext();
                return list;
            }

            public void WriteYaml(IEmitter emitter, object value, Type type)
            {
                throw new NotImplementedException();
            }
        }
    }
}