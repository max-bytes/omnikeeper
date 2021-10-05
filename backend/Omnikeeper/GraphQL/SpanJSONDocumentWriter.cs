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
        public Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            try
            {
                return JsonSerializer.Generic.Utf8.SerializeAsync<ExecutionResult, Resolver<byte>>((value as ExecutionResult)!, stream, cancellationToken).AsTask();
            } catch (Exception e)
            {
                return Task.CompletedTask;
            }
        }
    }

    public sealed class ExecutionResultFormatter : ICustomJsonFormatter<ExecutionResult>
    {
        public static readonly ExecutionResultFormatter Default = new ExecutionResultFormatter();

        public object? Arguments { get; set; }

        private ErrorInfoProvider errorInfoProvider = new ErrorInfoProvider(); // TODO: make configurable?

        public void Serialize(ref JsonWriter<byte> writer, ExecutionResult value)
        {
            writer.WriteBeginObject();
            var shouldWriteData = (value.Errors == null || value.Errors.Count == 0) && value.Data != null;
            var shouldWriteErrors = value.Errors != null && value.Errors.Count > 0;
            var shouldWriteExtensions = value.Data != null && value.Extensions != null && value.Extensions.Count > 0;
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

        private void WriteData(ref JsonWriter<byte> writer, ExecutionResult result)
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
                ComplexClassFormatter<object, byte, IncludeNullsCamelCaseResolver<byte>>.Default.Serialize(ref writer, node.ToValue()!);
            }
        }

        private void WriteErrors(ref JsonWriter<byte> writer, ExecutionErrors errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            writer.WriteName("errors");

            writer.WriteBeginArray();

            foreach (var error in errors)
            {
                var info = errorInfoProvider.GetInfo(error);

                writer.WriteBeginObject();

                writer.WriteName("message");

                writer.WriteUtf8String(info.Message);

                if (error.Locations != null)
                {
                    writer.WriteName("locations");
                    writer.WriteBeginArray();
                    foreach (var location in error.Locations)
                    {
                        writer.WriteBeginObject();
                        writer.WriteName("line");
                        writer.WriteUtf8Int32(location.Line);
                        writer.WriteName("column");
                        writer.WriteUtf8Int32(location.Column);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }

                if (error.Path != null && error.Path.Any())
                {
                    writer.WriteName("path");
                    if (error.Path == null)
                        writer.WriteUtf8Null();
                    else
                        ListFormatter<IList<object>, object, byte, IncludeNullsCamelCaseResolver<byte>>.Default.Serialize(ref writer, error.Path.ToList());
                    //serializer.Serialize(writer, error.Path);
                }

                if (info.Extensions?.Count > 0)
                {
                    // TODO
                    //writer.WriteName("extensions");
                    //writer.WriteUtf8String(info.Extensions);
                    //serializer.Serialize(writer, info.Extensions);
                }

                writer.WriteEndObject();
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
            throw new NotImplementedException();
        }
    }
}
