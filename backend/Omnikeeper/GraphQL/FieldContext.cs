//using GraphQL;
//using GraphQL.Execution;
//using GraphQL.Language.AST;
//using GraphQL.Types;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Omnikeeper.GraphQL
//{
//	// NOTE: helper class, taken from https://github.com/graphql-dotnet/graphql-dotnet/issues/2164
//	// used to traverse graphql request tree more easily
//	public record FieldContext(
//	ExecutionContext ExecutionContext,
//	FieldType FiledDefinition,
//	Field FieldAst
//)
//	{
//		public string Name => FieldAst.Name;
//		public string AliasOrName => FieldAst.Alias ?? FieldAst.Name;

//		public static FieldContext FromContext(IResolveFieldContext context)
//		{
//			return new FieldContext(
//				FieldAst: context.FieldAst,
//				FiledDefinition: GetFieldDefinition(context.Schema, context.ParentType, context.FieldAst)!,
//				ExecutionContext: new ExecutionContext
//				{
//					Document = context.Document,
//					Schema = context.Schema,
//					Variables = context.Variables
//				}
//			);

//		}


//		public IEnumerable<FieldContext> Fields
//		{
//			get
//			{
//				if (FiledDefinition.ResolvedType is null) yield break;
//				if (ResolveType(FiledDefinition.ResolvedType) is not IObjectGraphType objectGraph) yield break;

//				var fields = GetSubFields(ExecutionContext, objectGraph, FieldAst);
//				if (fields is null) yield break;
//				foreach (var field in fields)
//				{
//					var fieldDefinition = GetFieldDefinition(ExecutionContext.Schema, objectGraph, field.Value);
//					if (fieldDefinition is null) continue;
//					yield return new FieldContext
//					(
//						FieldAst: field.Value,
//						FiledDefinition: fieldDefinition,
//						ExecutionContext: ExecutionContext
//					);
//				}
//			}
//		}

//		public object? GetArgumentObject(string name)
//		{
//			var field = FieldAst.Arguments?.FirstOrDefault(a => a.Name == name);
//			var valueValue = field?.Value;

//			var filedDefinition = FiledDefinition.Arguments?.FirstOrDefault(x => x.Name == name);
//			if (filedDefinition is null) return null;
//			var argVal = ExecutionHelper.CoerceValue(
//				filedDefinition.ResolvedType!, valueValue, ExecutionContext.Variables, filedDefinition.DefaultValue);
//			return argVal.Value;
//		}
//		public IEnumerable<T> GetArgumentList<T>(string name)
//		{
//			var obj = GetArgumentObject(name);
//			if (obj is null) yield break;
//			var objs = (IEnumerable)obj;
//			foreach (var o in objs)
//			{
//				yield return (T)o;
//			}
//		}

//		IGraphType ResolveType(IGraphType type) =>
//			type switch
//			{
//				NonNullGraphType nullType => ResolveType(nullType.ResolvedType!),
//				ListGraphType listType => ResolveType(listType.ResolvedType!),
//				_ => type
//			};

//		private class GetAroundProtectedFunctions : ExecutionStrategy
//		{
//			protected override Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
//			{
//				throw new System.NotImplementedException();
//			}

//			public Dictionary<string, Field> PublicCollectFieldsFrom(ExecutionContext context, IGraphType specificType, SelectionSet selectionSet, Dictionary<string, Field>? fields)
//			{
//				return base.CollectFieldsFrom(context, specificType, selectionSet, fields);
//			}

//			public FieldType? PublicGetFieldDefinition(ISchema schema, IObjectGraphType parentType, Field field)
//			{
//				return base.GetFieldDefinition(schema, parentType, field);
//			}
//		}

//		private static readonly GetAroundProtectedFunctions _GetAroundProtectedFunctions = new();

//		private static Dictionary<string, Field>? GetSubFields(ExecutionContext context, IGraphType graphType, Field field)
//		{
//			if (field == null || !(field!.SelectionSet?.Selections?.Count > 0))
//			{
//				return null;
//			}
//			return _GetAroundProtectedFunctions.PublicCollectFieldsFrom(
//				context, graphType, field!.SelectionSet!, null);
//		}

//		private static FieldType? GetFieldDefinition(ISchema schema, IObjectGraphType parentType, Field field)
//		{
//			return _GetAroundProtectedFunctions.PublicGetFieldDefinition(schema, parentType, field);
//		}
//	}

//	public static class FieldContextStruct
//	{
//		public static T? GetArgument<T>(this FieldContext con, string name) where T : struct
//		{
//			return (T?)con.GetArgumentObject(name);
//		}
//	}

//	public static class FieldContextClass
//	{
//		public static T? GetArgument<T>(this FieldContext con, string name) where T : class
//		{
//			return (T?)con.GetArgumentObject(name);
//		}
//	}
//}
