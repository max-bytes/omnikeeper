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

    public class SpanJSONGraphQLSerializer : IGraphQLTextSerializer
    {
        private readonly IErrorInfoProvider errorInfoProvider;

        public SpanJSONGraphQLSerializer(IErrorInfoProvider errorInfoProvider)
        {
            this.errorInfoProvider = errorInfoProvider;
        }

        public bool IsNativelyAsync => true;

        public T? Deserialize<T>(string? value)
        {
            var t = JsonSerializer.Generic.Utf16.Deserialize<T, Resolver<char>>(value);
            return t;
        }

        public ValueTask<T?> ReadAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public T? ReadNode<T>(object? value)
        {
            throw new NotImplementedException();
        }

        public string Serialize<T>(T? value)
        {
            if (value is not ExecutionResult er)
                throw new Exception("Serialization is only supported for type ExecutionResult");
            // HACK: this is a really weird way to "inject" the errorInfoProvider into the formatter, but with SpanJSON, there seems to be no other way
            ExecutionResultFormatter.Default.ErrorInfoProvider = errorInfoProvider;
            var str = JsonSerializer.Generic.Utf16.Serialize<ExecutionResult, Resolver<char>>(er);
            return str;
        }

        public async Task WriteAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new Exception("Encountered null value to write, not supported");

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

        public void Serialize<C>(ref JsonWriter<C> writer, ExecutionResult value) where C : struct
        {
            writer.WriteBeginObject();
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
                if (separated) writer.WriteValueSeparator();
                WriteErrors(ref writer, value.Errors!);
                separated = true;
            }

            //if (shouldWriteExtensions)
            //{
            //    if (separated)
            //    {
            //        writer.WriteValueSeparator();
            //    }
            //    WriteExtensions(ref writer, value);
            //}

            writer.WriteEndObject();
        }

        private void WriteData<C>(ref JsonWriter<C> writer, ExecutionResult result) where C : struct
        {
            if (result.Executed)
            {
                writer.WriteName("data");
                if (result.Data is ExecutionNode executionNode)
                {
                    WriteExecutionNode(ref writer, executionNode);
                }
                else
                {
                    ComplexClassFormatter<object, C, IncludeNullsCamelCaseResolver<C>>.Default.Serialize(ref writer, result.Data!);
                }
            }
        }

        private void WriteExecutionNode<C>(ref JsonWriter<C> writer, ExecutionNode node) where C : struct
        {
            if (node is ValueExecutionNode valueExecutionNode)
            {
                var v = valueExecutionNode.ToValue();
                if (v is string s)
                    writer.WriteString(s);
                else if (v is null)
                    writer.WriteNull();
                else if (v is bool b)
                    writer.WriteBoolean(b);
                else if (v is int i)
                    writer.WriteInt32(i);
                else if (v is long l)
                    writer.WriteInt64(l);
                else if (v is float f)
                    writer.WriteSingle(f);
                else if (v is double d)
                    writer.WriteDouble(d);
                else if (v is DateTime dt)
                    writer.WriteDateTime(dt);
                else if (v is DateTime dto)
                    writer.WriteDateTimeOffset(dto);
                else
                    throw new Exception("Unknown value type detected!");
            }
            else if (node is ObjectExecutionNode objectExecutionNode)
            {
                if (objectExecutionNode.SubFields == null)
                {
                    writer.WriteNull();
                }
                else
                {
                    var separated = false;
                    writer.WriteBeginObject();
                    foreach (var childNode in objectExecutionNode.SubFields)
                    {
                        if (separated) writer.WriteValueSeparator();
                        writer.WriteName(childNode.Name);
                        WriteExecutionNode(ref writer, childNode);
                        separated = true;
                    }
                    writer.WriteEndObject();
                }
            }
            else if (node is ArrayExecutionNode arrayExecutionNode)
            {
                var items = arrayExecutionNode.Items;
                if (items == null)
                {
                    writer.WriteNull();
                }
                else
                {
                    var separated = false;
                    writer.WriteBeginArray();
                    foreach (var childNode in items)
                    {
                        if (separated) writer.WriteValueSeparator();
                        WriteExecutionNode(ref writer, childNode);
                        separated = true;
                    }
                    writer.WriteEndArray();
                }
            }
            else if (node == null || node is NullExecutionNode)
            {
                writer.WriteNull();
            }
            else
            {
                ComplexClassFormatter<object, C, IncludeNullsCamelCaseResolver<C>>.Default.Serialize(ref writer, node.ToValue()!);
            }
        }

        private void WriteErrors<C>(ref JsonWriter<C> writer, ExecutionErrors errors) where C : struct
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            writer.WriteName("errors");

            writer.WriteBeginArray();
            var separated = false;
            foreach (var error in errors)
            {
                var info = ErrorInfoProvider!.GetInfo(error);

                if (separated) writer.WriteValueSeparator();

                writer.WriteBeginObject();

                writer.WriteName("message");
                writer.WriteString(info.Message);

                if (error.Locations != null)
                {
                    writer.WriteValueSeparator();
                    writer.WriteName("locations");
                    writer.WriteBeginArray();
                    var separatedInner = false;
                    foreach (var location in error.Locations)
                    {
                        if (separatedInner) writer.WriteValueSeparator();
                        writer.WriteBeginObject();
                        writer.WriteName("line");
                        writer.WriteInt32(location.Line);
                        writer.WriteValueSeparator();
                        writer.WriteName("column");
                        writer.WriteInt32(location.Column);
                        writer.WriteEndObject();
                        separatedInner = true;
                    }
                    writer.WriteEndArray();
                }

                if (error.Path != null && error.Path.Any())
                {
                    writer.WriteValueSeparator();
                    writer.WriteName("path");
                    if (error.Path == null)
                        writer.WriteNull();
                    else
                        ListFormatter<IList<object>, object, C, IncludeNullsCamelCaseResolver<C>>.Default.Serialize(ref writer, error.Path.ToList());
                }

                if (info.Extensions?.Count > 0)
                {
                    //writer.WriteName("extensions");
                    //writer.WriteString(info.Extensions);
                    //serializer.Serialize(writer, info.Extensions);
                }

                writer.WriteEndObject();

                separated = true;
            }

            writer.WriteEndArray();
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
            Serialize<char>(ref writer, value);
        }

        public void Serialize(ref JsonWriter<byte> writer, ExecutionResult value)
        {
            Serialize<byte>(ref writer, value);
        }
    }
}
