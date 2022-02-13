using GraphQL;
using GraphQL.Execution;
using SpanJson;
using SpanJson.Formatters;
using SpanJson.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL
{
    public class Resolver<TSymbol> : ResolverBase<TSymbol, Resolver<TSymbol>> where TSymbol : struct
    {
        public Resolver() : base(new SpanJsonOptions
        {
            NullOption = NullOptions.IncludeNulls,
            NamingConvention = NamingConventions.CamelCase,
            EnumOption = EnumOptions.String
        })
        {
            RegisterGlobalCustomFormatter<ExecutionResult, ExecutionResultFormatter>();
        }
    }

    public class SpanJSONDocumentWriter : IDocumentWriter
    {
        private readonly IErrorInfoProvider errorInfoProvider;

        public SpanJSONDocumentWriter(IErrorInfoProvider errorInfoProvider)
        {
            this.errorInfoProvider = errorInfoProvider;
        }
        public async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            // HACK: this is a really weird way to "inject" the errorInfoProvider into the formatter, but with SpanJSON, there seems to be no other way
            ExecutionResultFormatter.Default.ErrorInfoProvider = errorInfoProvider;
            await JsonSerializer.Generic.Utf8.SerializeAsync<ExecutionResult, Resolver<byte>>((value as ExecutionResult)!, stream, cancellationToken);
        }
    }

    public sealed class ExecutionResultFormatter : ICustomJsonFormatter<ExecutionResult>
    {
        public static readonly ExecutionResultFormatter Default = new ExecutionResultFormatter();

        public object? Arguments { get; set; }

        public IErrorInfoProvider? ErrorInfoProvider;// = new ErrorInfoProvider(); // TODO: make configurable?

        public void Serialize(ref JsonWriter<byte> writer, ExecutionResult value)
        {
            writer.WriteUtf8BeginObject();
            var shouldWriteData = (value.Errors == null || value.Errors.Count == 0) && value.Data != null;
            var shouldWriteErrors = value.Errors != null && value.Errors.Count > 0;
            //var shouldWriteExtensions = value.Data != null && value.Extensions != null && value.Extensions.Count > 0;
            var separated = false;
            if (shouldWriteData)
            {
                WriteData(ref writer, value);
                separated = true;
            }

            if (shouldWriteErrors)
            {
                if (separated) writer.WriteUtf8ValueSeparator();
                WriteErrors(ref writer, value.Errors!);
                separated = true;
            }

            //if (shouldWriteExtensions)
            //{
            //    if (separated)
            //    {
            //        writer.WriteUtf8ValueSeparator();
            //    }
            //    WriteExtensions(ref writer, value);
            //}

            writer.WriteUtf8EndObject();
        }

        private void WriteData(ref JsonWriter<byte> writer, ExecutionResult result)
        {
            if (result.Executed)
            {
                writer.WriteUtf8Name("data");
                if (result.Data is ExecutionNode executionNode)
                {
                    WriteExecutionNode(ref writer, executionNode);
                }
                else
                {
                    ComplexClassFormatter<object, byte, IncludeNullsCamelCaseResolver<byte>>.Default.Serialize(ref writer, result.Data!);
                }
            }
        }

        private void WriteExecutionNode(ref JsonWriter<byte> writer, ExecutionNode node)
        {
            if (node is ValueExecutionNode valueExecutionNode)
            {
                var v = valueExecutionNode.ToValue();
                if (v is string s)
                    writer.WriteUtf8String(s);
                else if (v is null)
                    writer.WriteUtf8Null();
                else if (v is bool b)
                    writer.WriteUtf8Boolean(b);
                else if (v is int i)
                    writer.WriteUtf8Int32(i);
                else if (v is long l)
                    writer.WriteUtf8Int64(l);
                else if (v is float f)
                    writer.WriteUtf8Single(f);
                else if (v is double d)
                    writer.WriteUtf8Double(d);
                else if (v is DateTime dt)
                    writer.WriteUtf8DateTime(dt);
                else if (v is DateTime dto)
                    writer.WriteUtf8DateTimeOffset(dto);
                else
                    throw new Exception("Unknown value type detected!");
            }
            else if (node is ObjectExecutionNode objectExecutionNode)
            {
                if (objectExecutionNode.SubFields == null)
                {
                    writer.WriteUtf8Null();
                }
                else
                {
                    var separated = false;
                    writer.WriteUtf8BeginObject();
                    foreach (var childNode in objectExecutionNode.SubFields)
                    {
                        if (separated) writer.WriteUtf8ValueSeparator();
                        writer.WriteUtf8Name(childNode.Name);
                        WriteExecutionNode(ref writer, childNode);
                        separated = true;
                    }
                    writer.WriteUtf8EndObject();
                }
            }
            else if (node is ArrayExecutionNode arrayExecutionNode)
            {
                var items = arrayExecutionNode.Items;
                if (items == null)
                {
                    writer.WriteUtf8Null();
                }
                else
                {
                    var separated = false;
                    writer.WriteUtf8BeginArray();
                    foreach (var childNode in items)
                    {
                        if (separated) writer.WriteUtf8ValueSeparator();
                        WriteExecutionNode(ref writer, childNode);
                        separated = true;
                    }
                    writer.WriteUtf8EndArray();
                }
            }
            else if (node == null || node is NullExecutionNode)
            {
                writer.WriteUtf8Null();
            }
            else
            {
                ComplexClassFormatter<object, byte, IncludeNullsCamelCaseResolver<byte>>.Default.Serialize(ref writer, node.ToValue()!);
            }
        }

        private void WriteErrors(ref JsonWriter<byte> writer, ExecutionErrors errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            writer.WriteUtf8Name("errors");

            writer.WriteUtf8BeginArray();
            var separated = false;
            foreach (var error in errors)
            {
                var info = ErrorInfoProvider!.GetInfo(error);

                if (separated) writer.WriteUtf8ValueSeparator();

                writer.WriteUtf8BeginObject();

                writer.WriteUtf8Name("message");
                writer.WriteUtf8String(info.Message);

                if (error.Locations != null)
                {
                    writer.WriteUtf8ValueSeparator();
                    writer.WriteUtf8Name("locations");
                    writer.WriteUtf8BeginArray();
                    var separatedInner = false;
                    foreach (var location in error.Locations)
                    {
                        if (separatedInner) writer.WriteUtf8ValueSeparator();
                        writer.WriteUtf8BeginObject();
                        writer.WriteUtf8Name("line");
                        writer.WriteUtf8Int32(location.Line);
                        writer.WriteUtf8ValueSeparator();
                        writer.WriteUtf8Name("column");
                        writer.WriteUtf8Int32(location.Column);
                        writer.WriteUtf8EndObject();
                        separatedInner = true;
                    }
                    writer.WriteUtf8EndArray();
                }

                if (error.Path != null && error.Path.Any())
                {
                    writer.WriteUtf8ValueSeparator();
                    writer.WriteUtf8Name("path");
                    if (error.Path == null)
                        writer.WriteUtf8Null();
                    else
                        ListFormatter<IList<object>, object, byte, IncludeNullsCamelCaseResolver<byte>>.Default.Serialize(ref writer, error.Path.ToList());
                }

                if (info.Extensions?.Count > 0)
                {
                    //writer.WriteUtf8Name("extensions");
                    //writer.WriteUtf8String(info.Extensions);
                    //serializer.Serialize(writer, info.Extensions);
                }

                writer.WriteUtf8EndObject();

                separated = true;
            }

            writer.WriteUtf8EndArray();
        }

        public ExecutionResult Deserialize(ref JsonReader<byte> reader)
        {
            throw new NotImplementedException();
        }

        public ExecutionResult Deserialize(ref JsonReader<char> reader)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref JsonWriter<char> writer, ExecutionResult value)
        {
            throw new NotImplementedException();
        }
    }
}
