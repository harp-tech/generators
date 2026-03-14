using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Harp.Generators.Tests
{
    internal class YamlStream : YamlNode
    {
        internal YamlStream(IParser parser)
        {
            Start = parser.Consume<StreamStart>();
            Document = new YamlDocumentNode(parser);
            End = parser.Consume<StreamEnd>();
        }

        public StreamStart Start { get; }

        public YamlDocumentNode Document { get; }

        public StreamEnd End { get; }

        internal override void Save(IEmitter emitter)
        {
            emitter.Emit(Start);
            Document?.Save(emitter);
            emitter.Emit(End);
        }
    }

    internal class YamlDocumentNode : YamlNode
    {
        internal YamlDocumentNode(IParser parser)
        {
            Start = parser.Consume<DocumentStart>();
            Contents = Parse(parser);
            End = parser.Consume<DocumentEnd>();
        }

        public DocumentStart Start { get; }

        public YamlNode Contents { get; }

        public DocumentEnd End { get; }

        internal override void Save(IEmitter emitter)
        {
            emitter.Emit(Start);
            Contents?.Save(emitter);
            emitter.Emit(End);
        }
    }

    internal class YamlSequenceNode : YamlNode
    {
        internal YamlSequenceNode(IParser parser)
        {
            Start = parser.Consume<SequenceStart>();
            SequenceEnd? end;
            while (!parser.TryConsume<SequenceEnd>(out end))
            {
                Children.Add(Parse(parser));
            }
            End = end;
        }

        public SequenceStart Start { get; }

        public SequenceEnd End { get; }

        public List<YamlNode> Children { get; } = [];

        internal override void Save(IEmitter emitter)
        {
            emitter.Emit(Start);
            foreach (var child in Children)
                child.Save(emitter);
            emitter.Emit(End);
        }
    }

    internal class YamlMappingNode : YamlNode
    {
        internal YamlMappingNode(IParser parser)
        {
            Start = parser.Consume<MappingStart>();
            MappingEnd? end;
            while (!parser.TryConsume<MappingEnd>(out end))
            {
                var key = new YamlScalarNode(parser);
                var value = Parse(parser);
                Children[key] = value;
            }
            End = end;
        }

        public MappingStart Start { get; }

        public MappingEnd End { get; }

        public Dictionary<YamlScalarNode, YamlNode> Children { get; } = new(YamlScalarNodeEqualityComparer.Default);

        internal override void Save(IEmitter emitter)
        {
            emitter.Emit(Start);
            foreach (var (key, value) in Children)
            {
                key.Save(emitter);
                value.Save(emitter);
            }
            emitter.Emit(End);
        }

        class YamlScalarNodeEqualityComparer : IEqualityComparer<YamlScalarNode>
        {
            public static readonly YamlScalarNodeEqualityComparer Default = new();

            public bool Equals(YamlScalarNode? x, YamlScalarNode? y)
            {
                return x?.Event.Value == y?.Event.Value;
            }

            public int GetHashCode([DisallowNull] YamlScalarNode obj)
            {
                return obj.Event.Value.GetHashCode();
            }
        }
    }

    internal class YamlScalarNode : YamlNode
    {
        internal YamlScalarNode(IParser parser)
        {
            Event = parser.Consume<Scalar>();
        }

        public Scalar Event { get; }

        internal override void Save(IEmitter emitter)
        {
            emitter.Emit(Event);
        }
    }

    internal abstract class YamlNode
    {
        internal static YamlNode Parse(IParser parser)
        {
            if (parser.Accept<Scalar>(out _))
            {
                return new YamlScalarNode(parser);
            }

            if (parser.Accept<SequenceStart>(out var start))
            {
                return new YamlSequenceNode(parser);
            }

            if (parser.Accept<MappingStart>(out var _))
            {
                return new YamlMappingNode(parser);
            }

            throw new ArgumentException("The current event is of an unsupported type.", nameof(parser));
        }

        internal abstract void Save(IEmitter emitter);
    }
}
